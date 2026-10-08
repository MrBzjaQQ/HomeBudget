using Budget.Application.Auth;
using Budget.Application.Budget;
using Budget.Application.Categories;
using Budget.Application.Export;
using Budget.Application.Goals;
using Budget.Application.Savings;
using Budget.Application.Transactions;
using Budget.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Budget.Application;

/// <summary>Регистрация сервисов бизнес-логики слоя Application.</summary>
public static class DependencyInjection
{
    /// <summary>Регистрирует прикладные сервисы с областью жизни на один запрос.</summary>
    /// <param name="services">Коллекция зависимостей приложения.</param>
    /// <returns>Та же коллекция для продолжения настройки контейнера.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<ISavingsService, SavingsService>();
        services.AddScoped<IGoalsService, GoalsService>();
        services.AddScoped<IExportService, ExportService>();

        return services;
    }
}
