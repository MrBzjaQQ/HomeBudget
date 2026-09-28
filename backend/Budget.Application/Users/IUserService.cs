namespace Budget.Application.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct);

    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct);
}
