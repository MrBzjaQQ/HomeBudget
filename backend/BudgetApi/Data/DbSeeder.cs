using BudgetApi.Data;
using BudgetApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(BudgetDbContext db)
    {
        if (!await db.Users.AnyAsync())
        {
            db.Users.Add(new User
            {
                Username = "admin",
                DisplayName = "Админ",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        }

        if (!await db.Categories.AnyAsync())
        {
            var (incomeGroups, expenseGroups) = BuildSeedCategories();
            db.Categories.AddRange(incomeGroups);
            db.Categories.AddRange(expenseGroups);
        }

        if (!await db.SavingsContainers.AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.SavingsContainers.AddRange(
                new SavingsContainer { Name = "Вклад", Color = "#0f766e", SortOrder = 1, CreatedAt = now },
                new SavingsContainer { Name = "Наличка", Color = "#f59e0b", SortOrder = 2, CreatedAt = now },
                new SavingsContainer { Name = "Накопительный счёт", Color = "#3b82f6", SortOrder = 3, CreatedAt = now });
        }

        await db.SaveChangesAsync();
    }

    private static (List<Category> Income, List<Category> Expense) BuildSeedCategories()
    {
        var income = new List<Category>();
        var expense = new List<Category>();
        var now = DateTime.UtcNow;

        void AddGroup(List<Category> target, string name, string color, int order, params (string Name, int Order)[] subs)
        {
            var kind = target == income ? TransactionKind.Income : TransactionKind.Expense;
            var group = new Category { Name = name, Kind = kind, Color = color, SortOrder = order, CreatedAt = now };
            foreach (var sub in subs)
            {
                group.Children.Add(new Category
                {
                    Name = sub.Name,
                    Kind = kind,
                    SortOrder = sub.Order,
                    CreatedAt = now,
                });
            }

            target.Add(group);
        }

        AddGroup(income, "Зарплата", "#2E7D32", 1);
        AddGroup(income, "Подработка", "#66BB6A", 2);
        AddGroup(income, "Кэшбэк/возвраты", "#26A69A", 3);
        AddGroup(income, "Подарки", "#43A047", 4);
        AddGroup(income, "Прочее", "#A5D6A7", 5);

        AddGroup(expense, "Продукты", "#E5484D", 1,
            ("Супермаркет", 1), ("Фермерский", 2), ("Рынок", 3));
        AddGroup(expense, "ЖКХ и связи", "#FF8B00", 2,
            ("Ипотека", 1), ("Коммуналка", 2), ("Интернет/телефон", 3));
        AddGroup(expense, "Транспорт", "#0091FF", 3,
            ("Бензин", 1), ("Такси", 2), ("Проезд", 3));
        AddGroup(expense, "Здоровье", "#D6409F", 4,
            ("Аптека", 1), ("Врачи", 2));
        AddGroup(expense, "Детям", "#8E4EC6", 5,
            ("Детское", 1), ("Детский сад", 2), ("Обучение", 3));
        AddGroup(expense, "Дом", "#A06A3C", 6,
            ("Ремонт", 1), ("Мебель", 2), ("Хозтовары", 3));
        AddGroup(expense, "Рестораны и кафе", "#FF6369", 7);
        AddGroup(expense, "Одежда", "#00B3A4", 8);
        AddGroup(expense, "Подарки", "#FFD43B", 9);
        AddGroup(expense, "Развлечения", "#7048E8", 10);
        AddGroup(expense, "Прочее", "#868E96", 11);

        return (income, expense);
    }
}
