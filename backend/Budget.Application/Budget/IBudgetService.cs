namespace Budget.Application.Budget;

/// <summary>Управляет плановыми расходами и сводкой бюджета по месяцам.</summary>
public interface IBudgetService
{
    /// <summary>Возвращает плановые позиции и сравнение плана с фактом за месяц.</summary>
    /// <param name="month">Месяц в формате yyyy-MM; null или некорректное значение отклоняется.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Позиции плана, разбивка по категориям и общие суммы.</returns>
    Task<BudgetMonthResponse> GetMonthAsync(string? month, CancellationToken ct);

    /// <summary>Добавляет плановую позицию бюджета.</summary>
    /// <param name="request">Месяц, категория, сумма и комментарий плана.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Представление созданной позиции.</returns>
    Task<BudgetItemDto> CreateAsync(CreateBudgetItemRequest request, CancellationToken ct);

    /// <summary>Изменяет существующую плановую позицию.</summary>
    /// <param name="id">Идентификатор позиции бюджета.</param>
    /// <param name="request">Новые значения позиции.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённое представление позиции.</returns>
    Task<BudgetItemDto> UpdateAsync(long id, CreateBudgetItemRequest request, CancellationToken ct);

    /// <summary>Удаляет плановую позицию бюджета.</summary>
    /// <param name="id">Идентификатор позиции, которую нужно удалить.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    Task DeleteAsync(long id, CancellationToken ct);
}
