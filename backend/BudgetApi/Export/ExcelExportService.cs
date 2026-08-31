using System.Globalization;
using ClosedXML.Excel;

namespace BudgetApi.Export;

public sealed record ExportTransactionRow(
    DateOnly Date,
    bool IsIncome,
    decimal Amount,
    string CategoryPath,
    string UserName,
    string? Note);

public sealed class ExportData
{
    public required IReadOnlyList<ExportTransactionRow> Rows { get; init; }
    public required string WhoLabel { get; init; }
}

public sealed class ExcelExportService
{
    private static readonly CultureInfo Ru = new("ru-RU");
    private const string MoneyFormat = "#,##0.00";
    private const string DateFormat = "dd.mm.yyyy";

    public byte[] BuildMonthFile(ExportData data, int year, int month)
    {
        using var workbook = new XLWorkbook();
        var sheetName = Capitalize(MonthName(month)) + " " + year;
        AddSheet(workbook, sheetName, data, year, month, withCategoryBreakdown: true);
        return Save(workbook);
    }

    public byte[] BuildYearFile(ExportData data, int year)
    {
        using var workbook = new XLWorkbook();

        for (var month = 1; month <= 12; month++)
        {
            var first = new DateOnly(year, month, 1);
            var last = first.AddMonths(1).AddDays(-1);
            var monthRows = data.Rows
                .Where(r => r.Date >= first && r.Date <= last)
                .ToList();
            AddSheet(workbook, Capitalize(MonthName(month)), new ExportData { Rows = monthRows, WhoLabel = data.WhoLabel }, year, month, withCategoryBreakdown: false);
        }

        AddSummarySheet(workbook, data, year);
        return Save(workbook);
    }

    private static byte[] Save(IXLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void AddSheet(IXLWorkbook workbook, string sheetName, ExportData data, int year, int month, bool withCategoryBreakdown)
    {
        var sheet = workbook.Worksheets.Add(sheetName);
        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var rows = data.Rows
            .Where(r => r.Date >= first && r.Date <= last)
            .OrderBy(r => r.Date)
            .ThenBy(r => r.CategoryPath)
            .ToList();

        // Column widths: No 5, Date 12, Kind 10, Category 26, Note 40, Who 15, amounts 14.
        sheet.Column(1).Width = 5;
        sheet.Column(2).Width = 12;
        sheet.Column(3).Width = 10;
        sheet.Column(4).Width = 26;
        sheet.Column(5).Width = 40;
        sheet.Column(6).Width = 15;
        sheet.Column(7).Width = 14;
        sheet.Column(8).Width = 14;

        // Row 1: title.
        var title = $"Движения за {MonthName(month)} {year}";
        sheet.Range("A1:H1").Merge().SetValue(title).Style.Font.Bold = true;
        sheet.Range("A1:H1").Style.Font.FontSize = 14;

        // Row 2: parameters.
        var period = $"Период: {first:dd.MM.yyyy}–{last:dd.MM.yyyy}";
        sheet.Range("A2:E2").Merge().SetValue(period);
        sheet.Range("F2:H2").Merge().SetValue($"Кто: {data.WhoLabel}");

        // Row 4: header.
        var headers = new[] { "№", "Дата", "Тип", "Категория", "Комментарий", "Кто", "Доход, ₽", "Расход, ₽" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(4, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EAF0");
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#B8BCC8");
        }

        // Data rows.
        var income = 0m;
        var expense = 0m;
        var row = 5;
        var number = 0;
        foreach (var r in rows)
        {
            number++;
            if (r.IsIncome)
            {
                income += r.Amount;
            }
            else
            {
                expense += r.Amount;
            }

            SetCell(sheet.Cell(row, 1), number.ToString(CultureInfo.InvariantCulture));
            SetCell(sheet.Cell(row, 2), r.Date, DateFormat);
            SetCell(sheet.Cell(row, 3), r.IsIncome ? "Доход" : "Расход");
            SetCell(sheet.Cell(row, 4), r.CategoryPath);
            SetCell(sheet.Cell(row, 5), r.Note ?? string.Empty);
            SetCell(sheet.Cell(row, 6), r.UserName);
            if (r.IsIncome)
            {
                SetCell(sheet.Cell(row, 7), r.Amount, MoneyFormat);
            }
            else
            {
                SetCell(sheet.Cell(row, 8), r.Amount, MoneyFormat);
            }

            row++;
        }

        sheet.SheetView.FreezeRows(4);

        // Totals block.
        row += 1; // blank line
        sheet.Cell(row, 1).Value = "Итого за период";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        row++;
        sheet.Cell(row, 1).Value = "Доходы:";
        SetCell(sheet.Cell(row, 2), income, MoneyFormat);
        row++;
        sheet.Cell(row, 1).Value = "Расходы:";
        SetCell(sheet.Cell(row, 2), expense, MoneyFormat);
        row++;
        sheet.Cell(row, 1).Value = "Остаток:";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        SetCell(sheet.Cell(row, 2), income - expense, MoneyFormat);
        sheet.Cell(row, 2).Style.Font.Bold = true;

        if (!withCategoryBreakdown)
        {
            return;
        }

        // Expense breakdown by category (monthly file only).
        row += 2;
        sheet.Cell(row, 1).Value = "Расходы по категориям";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        row++;
        var breakdownHeaders = new[] { "Категория", "Сумма, ₽", "Доля, %" };
        for (var i = 0; i < breakdownHeaders.Length; i++)
        {
            var cell = sheet.Cell(row, i + 1);
            cell.Value = breakdownHeaders[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EAF0");
        }
        row++;

        var byCategory = rows
            .Where(r => !r.IsIncome)
            .GroupBy(r => r.CategoryPath)
            .Select(g => new { Path = g.Key, Total = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Path)
            .ToList();

        foreach (var item in byCategory)
        {
            SetCell(sheet.Cell(row, 1), item.Path);
            SetCell(sheet.Cell(row, 2), item.Total, MoneyFormat);
            var share = expense > 0 ? item.Total / expense : 0m;
            var shareCell = sheet.Cell(row, 3);
            shareCell.SetValue((double)share);
            shareCell.Style.NumberFormat.Format = "0.0%";
            row++;
        }

        SetCell(sheet.Cell(row, 1), "Итого");
        sheet.Cell(row, 1).Style.Font.Bold = true;
        SetCell(sheet.Cell(row, 2), expense, MoneyFormat);
        sheet.Cell(row, 2).Style.Font.Bold = true;
        var totalShareCell = sheet.Cell(row, 3);
        totalShareCell.SetValue(expense > 0 ? 1d : 0d);
        totalShareCell.Style.NumberFormat.Format = "0.0%";
        totalShareCell.Style.Font.Bold = true;
    }

    private static void AddSummarySheet(IXLWorkbook workbook, ExportData data, int year)
    {
        var sheet = workbook.Worksheets.Add("Сводка");
        sheet.Column(1).Width = 14;
        sheet.Column(2).Width = 16;
        sheet.Column(3).Width = 16;
        sheet.Column(4).Width = 22;
        sheet.Column(5).Width = 26;

        sheet.Range("A1:E1").Merge().SetValue($"Годовая сводка {year}").Style.Font.Bold = true;
        sheet.Range("A1:E1").Style.Font.FontSize = 14;

        var headers = new[] { "Месяц", "Доходы, ₽", "Расходы, ₽", "Остаток за месяц, ₽", "Остаток накопительно, ₽" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EAF0");
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#B8BCC8");
        }

        var cumulative = 0m;
        var totalIncome = 0m;
        var totalExpense = 0m;
        var row = 4;
        for (var month = 1; month <= 12; month++)
        {
            var first = new DateOnly(year, month, 1);
            var last = first.AddMonths(1).AddDays(-1);
            var monthRows = data.Rows.Where(r => r.Date >= first && r.Date <= last).ToList();
            var income = monthRows.Where(r => r.IsIncome).Sum(r => r.Amount);
            var expense = monthRows.Where(r => !r.IsIncome).Sum(r => r.Amount);
            var net = income - expense;
            cumulative += net;
            totalIncome += income;
            totalExpense += expense;

            SetCell(sheet.Cell(row, 1), Capitalize(MonthName(month)));
            SetCell(sheet.Cell(row, 2), income, MoneyFormat);
            SetCell(sheet.Cell(row, 3), expense, MoneyFormat);
            SetCell(sheet.Cell(row, 4), net, MoneyFormat);
            SetCell(sheet.Cell(row, 5), cumulative, MoneyFormat);
            row++;
        }

        sheet.Cell(row, 1).Value = $"Итого за {year}";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        SetCell(sheet.Cell(row, 2), totalIncome, MoneyFormat);
        SetCell(sheet.Cell(row, 3), totalExpense, MoneyFormat);
        SetCell(sheet.Cell(row, 4), totalIncome - totalExpense, MoneyFormat);
        SetCell(sheet.Cell(row, 5), cumulative, MoneyFormat);
        for (var i = 1; i <= 5; i++)
        {
            sheet.Cell(row, i).Style.Font.Bold = true;
        }

        row += 2;
        sheet.Cell(row, 1).Value = $"накопительно — в пределах {year} года";
    }

    private static void SetCell(IXLCell cell, string value)
    {
        cell.Value = value;
    }

    private static void SetCell(IXLCell cell, DateOnly value, string format)
    {
        cell.Value = value.ToDateTime(new TimeOnly(0));
        cell.Style.NumberFormat.Format = format;
    }

    private static void SetCell(IXLCell cell, decimal value, string format)
    {
        cell.Value = value;
        cell.Style.NumberFormat.Format = format;
    }

    private static string MonthName(int month) => Ru.DateTimeFormat.GetMonthName(month);

    private static string Capitalize(string s)
        => s.Length == 0 ? s : char.ToUpper(s[0], Ru) + s[1..];
}
