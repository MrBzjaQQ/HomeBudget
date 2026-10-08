namespace Budget.Application.Users;

/// <summary>Выполняет операции управления учётными записями пользователей.</summary>
public interface IUserService
{
    /// <summary>Возвращает пользователей в порядке идентификатора.</summary>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Список публичных представлений пользователей.</returns>
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct);

    /// <summary>Создаёт активную учётную запись после проверки полей запроса.</summary>
    /// <param name="request">Логин, отображаемое имя и пароль нового пользователя.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Публичное представление созданного пользователя.</returns>
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct);

    /// <summary>Изменяет предоставленные поля существующей учётной записи.</summary>
    /// <param name="id">Идентификатор изменяемого пользователя.</param>
    /// <param name="request">Поля учётной записи, подлежащие изменению.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновлённое публичное представление пользователя.</returns>
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct);
}
