namespace Budget.Application.Users;

/// <summary>Публичное представление учётной записи без секретных данных.</summary>
/// <param name="Id">Идентификатор пользователя.</param>
/// <param name="Username">Имя для входа.</param>
/// <param name="DisplayName">Имя для отображения в приложении.</param>
/// <param name="IsActive">Признак доступности входа для пользователя.</param>
/// <param name="CreatedAt">Дата и время создания учётной записи.</param>
public sealed record UserDto(int Id, string Username, string DisplayName, bool IsActive, DateTime CreatedAt);
