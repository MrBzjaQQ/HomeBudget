using Budget.Application.Users;

namespace Budget.Application.Auth;

public class LoginRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class LoginResponse(string token, UserDto user, string serverDate)
{
    public string Token { get; } = token;
    public UserDto User { get; } = user;
    public string ServerDate { get; } = serverDate;
}

public record MeResponse(UserDto User, string ServerDate);
