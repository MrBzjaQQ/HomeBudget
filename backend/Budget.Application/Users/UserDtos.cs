using Budget.Domain.Entities;

namespace Budget.Application.Users;

/// <summary>Преобразования доменных пользователей в безопасные DTO.</summary>
public static class UserDtos
{
    /// <summary>Создаёт DTO пользователя, исключая хеш пароля.</summary>
    /// <param name="user">Доменная учётная запись.</param>
    /// <returns>Публичное представление пользователя.</returns>
    public static UserDto ToDto(User user)
        => new(user.Id, user.Username, user.DisplayName, user.IsActive, user.CreatedAt);
}
