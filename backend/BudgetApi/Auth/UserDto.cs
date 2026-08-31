using BudgetApi.Data.Entities;

namespace BudgetApi.Auth;

public record UserDto(int Id, string Username, string DisplayName, bool IsActive, DateTime CreatedAt);

public static class UserDtos
{
    public static UserDto ToDto(User user)
        => new(user.Id, user.Username, user.DisplayName, user.IsActive, user.CreatedAt);
}
