using Budget.Domain.Entities;

namespace Budget.Application.Users;

public record UserDto(int Id, string Username, string DisplayName, bool IsActive, DateTime CreatedAt);

public static class UserDtos
{
    public static UserDto ToDto(User user)
        => new(user.Id, user.Username, user.DisplayName, user.IsActive, user.CreatedAt);
}

public record CreateUserRequest
{
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Password { get; set; }
}

public record UpdateUserRequest
{
    public string? DisplayName { get; set; }
    public string? Password { get; set; }
    public bool? IsActive { get; set; }
}
