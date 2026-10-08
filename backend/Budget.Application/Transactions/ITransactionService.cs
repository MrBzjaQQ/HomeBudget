using Budget.Domain.Entities;

namespace Budget.Application.Transactions;

/// <summary>Обеспечивает чтение, агрегацию и изменение финансовых операций.</summary>
public interface ITransactionService
{
    /// <summary>Возвращает страницу операций с фильтрацией, сортировкой и итогами.</summary>
    /// <param name="from">Нижняя включительная граница даты.</param>
    /// <param name="to">Верхняя включительная граница даты.</param>
    /// <param name="userId">Идентификатор участника для фильтрации.</param>
    /// <param name="categoryIds">Идентификаторы категорий для фильтрации.</param>
    /// <param name="kind">Направление операций для фильтрации.</param>
    /// <param name="q">Текст поиска в комментариях.</param>
    /// <param name="page">Номер страницы, начиная с единицы.</param>
    /// <param name="pageSize">Количество элементов страницы.</param>
    /// <param name="sort">Поле сортировки; поддерживаются дата и сумма.</param>
    /// <param name="dir">Направление сортировки.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Страница операций и итоговые суммы выбранного набора.</returns>
    Task<TransactionListResponse> ListAsync(
        DateOnly? from,
        DateOnly? to,
        int? userId,
        List<int>? categoryIds,
        TransactionKind? kind,
        string? q,
        int page,
        int pageSize,
        string? sort,
        string? dir,
        CancellationToken ct);

    /// <summary>Возвращает операцию по её идентификатору.</summary>
    /// <param name="id">Идентификатор операции.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Представление найденной операции.</returns>
    Task<TransactionDto> GetAsync(long id, CancellationToken ct);

    /// <summary>Строит сводку операций по категориям или календарным месяцам.</summary>
    /// <param name="from">Нижняя включительная граница периода.</param>
    /// <param name="to">Верхняя включительная граница периода.</param>
    /// <param name="groupBy">Поле группировки: category или month.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Сводные строки выбранного вида группировки.</returns>
    Task<SummaryResponse> SummaryAsync(DateOnly? from, DateOnly? to, string? groupBy, CancellationToken ct);

    /// <summary>Создаёт операцию после проверки данных и владельца.</summary>
    /// <param name="request">Поля создаваемой операции.</param>
    /// <param name="currentUserId">Идентификатор текущего пользователя для проверки доступа.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Представление созданной операции.</returns>
    Task<TransactionDto> CreateAsync(CreateTransactionRequest request, int currentUserId, CancellationToken ct);

    /// <summary>Обновляет существующую операцию проверенными значениями.</summary>
    /// <param name="id">Идентификатор изменяемой операции.</param>
    /// <param name="request">Новые данные операции.</param>
    /// <param name="currentUserId">Идентификатор текущего пользователя для проверки доступа.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённое представление операции.</returns>
    Task<TransactionDto> UpdateAsync(long id, CreateTransactionRequest request, int currentUserId, CancellationToken ct);

    /// <summary>Удаляет операцию по идентификатору.</summary>
    /// <param name="id">Идентификатор удаляемой операции.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    Task DeleteAsync(long id, CancellationToken ct);
}
