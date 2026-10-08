using Budget.Application.Export;
using Budget.Infrastructure.Export;
using ClosedXML.Excel;
using Xunit;

namespace BudgetApi.Tests;

/// <summary>Проверяет структуру и содержимое месячных и годовых Excel-отчётов.</summary>
public class ExportTests
{
    private static readonly int Year = 2026;

    private static ExportData SampleData() => new()
    {
        WhoLabel = "вся семья",
        Rows = new List<ExportTransactionRow>
        {
            new(new DateOnly(Year, 1, 5), true, 10000m, "Зарплата", "Иван", "оклад"),
            new(new DateOnly(Year, 1, 10), false, 2500m, "Продукты / Супермаркет", "Иван", "продукты"),
            new(new DateOnly(Year, 1, 20), false, 500m, "Транспорт / Такси", "Мария", null),
            new(new DateOnly(Year, 12, 31), false, 100m, "Прочее", "Мария", "конец года"),
        },
    };

    [Fact]
    /// <summary>Проверяет наличие двенадцати месячных листов и листа сводки в годовом файле.</summary>
    public void BuildYearFile_WithAllMonths_CreatesMonthlySheetsAndSummary()
    {
        var bytes = new ExcelExportService().BuildYearFile(SampleData(), Year);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));

        Assert.Equal(13, workbook.Worksheets.Count);
        var names = workbook.Worksheets.Select(w => w.Name).ToList();
        var expected = new[] { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
        Assert.Equal(expected, names.Take(12));
        Assert.Equal("Сводка", names[12]);
    }

    [Fact]
    /// <summary>Проверяет заголовки и итоги пустого месяца в годовом файле.</summary>
    public void BuildYearFile_WithEmptyMonth_CreatesHeaderAndZeroTotals()
    {
        var bytes = new ExcelExportService().BuildYearFile(SampleData(), Year);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet("Февраль");

        Assert.Contains("февраль", sheet.Cell(1, 1).GetString());
        Assert.Contains(Year.ToString(), sheet.Cell(1, 1).GetString());

        Assert.True(ContainsText(sheet, "Итого за период"));
        Assert.True(ContainsText(sheet, "Доходы:"));
        Assert.True(ContainsText(sheet, "Расходы:"));
        Assert.True(ContainsText(sheet, "Остаток:"));
        Assert.False(ContainsText(sheet, "Расходы по категориям"));
    }

    [Fact]
    /// <summary>Проверяет строки и итоги месячного листа годового файла.</summary>
    public void BuildYearFile_WithMonthlyTransactions_IncludesRowsAndTotals()
    {
        var bytes = new ExcelExportService().BuildYearFile(SampleData(), Year);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet("Январь");

        Assert.Equal("Движения за январь 2026", sheet.Cell(1, 1).GetString());
        Assert.Equal("№", sheet.Cell(4, 1).GetString());
        Assert.Equal("Доход, ₽", sheet.Cell(4, 7).GetString());
        Assert.Equal("Расход, ₽", sheet.Cell(4, 8).GetString());

        // 3 data rows in January, sorted by date.
        Assert.Equal("1", sheet.Cell(5, 1).GetString());
        Assert.Equal("Доход", sheet.Cell(5, 3).GetString());
        Assert.Equal(10000.0, sheet.Cell(5, 7).GetDouble());
        Assert.Equal("Расход", sheet.Cell(6, 3).GetString());
        Assert.Equal(2500.0, sheet.Cell(6, 8).GetDouble());
        Assert.Equal("Мария", sheet.Cell(7, 6).GetString());
        Assert.Equal("Транспорт / Такси", sheet.Cell(7, 4).GetString());

        // Totals block: income 10000, expense 3000, net 7000.
        var incomeRow = FindRow(sheet, "Доходы:");
        Assert.Equal(10000.0, sheet.Cell(incomeRow, 2).GetDouble());
        var expenseRow = FindRow(sheet, "Расходы:");
        Assert.Equal(3000.0, sheet.Cell(expenseRow, 2).GetDouble());
        var netRow = FindRow(sheet, "Остаток:");
        Assert.Equal(7000.0, sheet.Cell(netRow, 2).GetDouble());
    }

    [Fact]
    /// <summary>Проверяет месячные строки и накопительные итоги листа годовой сводки.</summary>
    public void BuildYearFile_WithTransactions_IncludesMonthlyAndCumulativeSummary()
    {
        var bytes = new ExcelExportService().BuildYearFile(SampleData(), Year);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet("Сводка");

        Assert.Contains("Годовая сводка", sheet.Cell(1, 1).GetString());
        Assert.Equal("Месяц", sheet.Cell(3, 1).GetString());
        Assert.Equal("Остаток накопительно, ₽", sheet.Cell(3, 5).GetString());

        // January: 10000 - 3000 = 7000 cumulative.
        Assert.Equal("Январь", sheet.Cell(4, 1).GetString());
        Assert.Equal(10000.0, sheet.Cell(4, 2).GetDouble());
        Assert.Equal(3000.0, sheet.Cell(4, 3).GetDouble());
        Assert.Equal(7000.0, sheet.Cell(4, 4).GetDouble());
        Assert.Equal(7000.0, sheet.Cell(4, 5).GetDouble());

        // February empty: zeros, cumulative stays 7000.
        Assert.Equal(0.0, sheet.Cell(5, 2).GetDouble());
        Assert.Equal(7000.0, sheet.Cell(5, 5).GetDouble());

        // December: -100, cumulative 6900.
        Assert.Equal(-100.0, sheet.Cell(15, 4).GetDouble());
        Assert.Equal(6900.0, sheet.Cell(15, 5).GetDouble());

        // Total row.
        Assert.Contains("Итого за 2026", sheet.Cell(16, 1).GetString());
        Assert.Equal(10000.0, sheet.Cell(16, 2).GetDouble());
        Assert.Equal(3100.0, sheet.Cell(16, 3).GetDouble());
        Assert.Equal(6900.0, sheet.Cell(16, 5).GetDouble());

        Assert.Contains("в пределах", sheet.Cell(18, 1).GetString());
    }

    [Fact]
    /// <summary>Проверяет единственный лист и детализацию расходов по категориям в месячном файле.</summary>
    public void BuildMonthFile_WithExpenses_CreatesSheetAndCategoryBreakdown()
    {
        var bytes = new ExcelExportService().BuildMonthFile(SampleData(), Year, 1);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));

        Assert.Single(workbook.Worksheets);
        var sheet = workbook.Worksheets.First();
        Assert.Equal("Январь 2026", sheet.Name);

        Assert.True(ContainsText(sheet, "Расходы по категориям"));

        // Breakdown: Продукты 2500 (83.3%), Транспорт 500 (16.7%).
        var headerRow = FindRow(sheet, "Категория", 4);
        Assert.True(headerRow > 0);
        Assert.Equal("Продукты / Супермаркет", sheet.Cell(headerRow + 1, 1).GetString());
        Assert.Equal(2500.0, sheet.Cell(headerRow + 1, 2).GetDouble());
        Assert.Equal(500.0, sheet.Cell(headerRow + 2, 2).GetDouble());

        var totalRow = FindRow(sheet, "Итого", headerRow + 1);
        Assert.Equal(3000.0, sheet.Cell(totalRow, 2).GetDouble());
    }

    [Fact]
    /// <summary>Проверяет отображение выбранного пользователя в месячном файле.</summary>
    public void BuildMonthFile_WithSpecificUser_ShowsUserLabel()
    {
        var data = new ExportData { WhoLabel = "Иван", Rows = SampleData().Rows.Where(r => r.UserName == "Иван").ToList() };
        var bytes = new ExcelExportService().BuildMonthFile(data, Year, 1);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheets.First();

        Assert.Contains("Иван", sheet.Cell(2, 6).GetString());
        Assert.Equal(12500.0, sheet.Cell(5, 7).GetDouble() + sheet.Cell(6, 8).GetDouble());
    }

    [Fact]
    /// <summary>Проверяет закрепление строк заголовка в месячном файле.</summary>
    public void BuildMonthFile_WithTransactions_FreezesPanesAfterHeader()
    {
        var bytes = new ExcelExportService().BuildMonthFile(SampleData(), Year, 1);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheets.First();

        Assert.Equal(4, sheet.SheetView.SplitRow);
    }

    [Fact]
    /// <summary>Проверяет сохранение структуры листа месячного файла без операций.</summary>
    public void BuildMonthFile_WithoutTransactions_PreservesWorksheetLayout()
    {
        var data = new ExportData { WhoLabel = "вся семья", Rows = new List<ExportTransactionRow>() };
        var bytes = new ExcelExportService().BuildMonthFile(data, Year, 6);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheets.First();

        Assert.Equal("Июнь 2026", sheet.Name);
        Assert.True(ContainsText(sheet, "Итого за период"));
        Assert.True(ContainsText(sheet, "Расходы по категориям"));
    }

    private static bool ContainsText(IXLWorksheet sheet, string text)
    {
        var range = sheet.RangeUsed();
        if (range is null)
        {
            return false;
        }

        foreach (var cell in range.Cells())
        {
            if (cell.GetString().Contains(text))
            {
                return true;
            }
        }

        return false;
    }

    private static int FindRow(IXLWorksheet sheet, string value, int startRow = 1)
    {
        for (var row = startRow; row <= 200; row++)
        {
            if (sheet.Cell(row, 1).GetString().Trim() == value)
            {
                return row;
            }
        }

        return -1;
    }
}
