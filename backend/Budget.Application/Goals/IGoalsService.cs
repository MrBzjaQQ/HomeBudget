namespace Budget.Application.Goals;

/// <summary>Выполняет операции чтения и управления финансовыми целями.</summary>
public interface IGoalsService
{
    /// <summary>Возвращает цели с рассчитанным прогрессом накоплений.</summary>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Список финансовых целей.</returns>
    Task<List<GoalDto>> ListAsync(CancellationToken ct);

    /// <summary>Создаёт финансовую цель после проверки её полей и счёта.</summary>
    /// <param name="request">Название, сумма, счёт и необязательный срок достижения.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Созданная цель с вычисленным прогрессом.</returns>
    Task<GoalDto> CreateAsync(CreateGoalRequest request, CancellationToken ct);

    /// <summary>Заменяет значения существующей финансовой цели.</summary>
    /// <param name="id">Идентификатор изменяемой цели.</param>
    /// <param name="request">Новые данные цели.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённая цель с вычисленным прогрессом.</returns>
    Task<GoalDto> UpdateAsync(int id, UpdateGoalRequest request, CancellationToken ct);

    /// <summary>Удаляет финансовую цель.</summary>
    /// <param name="id">Идентификатор удаляемой цели.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    Task DeleteAsync(int id, CancellationToken ct);
}
