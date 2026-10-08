namespace Budget.Application.Savings;

/// <summary>Управляет счетами и записями накоплений, а также формирует сводку.</summary>
public interface ISavingsService
{
    /// <summary>Возвращает счета накоплений в порядке отображения.</summary>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Список счетов накоплений.</returns>
    Task<IReadOnlyList<SavingsContainerDto>> ListContainersAsync(CancellationToken ct);

    /// <summary>Создаёт счёт накоплений.</summary>
    /// <param name="request">Название, цвет и порядок отображения нового счёта.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Представление созданного счёта.</returns>
    Task<SavingsContainerDto> CreateContainerAsync(CreateSavingsContainerRequest request, CancellationToken ct);

    /// <summary>Изменяет предоставленные свойства счёта накоплений.</summary>
    /// <param name="id">Идентификатор изменяемого счёта.</param>
    /// <param name="request">Новые значения свойств счёта.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённое представление счёта.</returns>
    Task<SavingsContainerDto> UpdateContainerAsync(int id, UpdateSavingsContainerRequest request, CancellationToken ct);

    /// <summary>Удаляет счёт, если он не содержит записей или целей.</summary>
    /// <param name="id">Идентификатор удаляемого счёта.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    Task DeleteContainerAsync(int id, CancellationToken ct);

    /// <summary>Возвращает записи накоплений за месяц и их сумму.</summary>
    /// <param name="month">Месяц в формате yyyy-MM.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Записи и общая сумма накоплений выбранного месяца.</returns>
    Task<SavingsMonthResponse> GetMonthAsync(string? month, CancellationToken ct);

    /// <summary>Создаёт ежемесячную запись накоплений участника.</summary>
    /// <param name="request">Месяц, пользователь, счёт, сумма и комментарий.</param>
    /// <param name="currentUserId">Идентификатор пользователя, выполняющего запрос.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Представление созданной записи.</returns>
    Task<SavingsEntryDto> CreateEntryAsync(CreateSavingsEntryRequest request, int currentUserId, CancellationToken ct);

    /// <summary>Обновляет существующую ежемесячную запись накоплений.</summary>
    /// <param name="id">Идентификатор записи.</param>
    /// <param name="request">Новые значения записи.</param>
    /// <param name="currentUserId">Идентификатор пользователя, выполняющего запрос.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённое представление записи.</returns>
    Task<SavingsEntryDto> UpdateEntryAsync(long id, UpdateSavingsEntryRequest request, int currentUserId, CancellationToken ct);

    /// <summary>Удаляет запись накоплений по идентификатору.</summary>
    /// <param name="id">Идентификатор удаляемой записи.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    Task DeleteEntryAsync(long id, CancellationToken ct);

    /// <summary>Возвращает помесячную динамику накоплений и суммы по участникам.</summary>
    /// <param name="to">Последний месяц сводки в формате yyyy-MM; по умолчанию используется текущий месяц.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Сводка за окно месяцев, заканчивающееся указанным месяцем.</returns>
    Task<SavingsSummary> SummaryAsync(string? to, CancellationToken ct);
}
