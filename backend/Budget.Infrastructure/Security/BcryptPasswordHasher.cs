using Budget.Application.Security;

namespace Budget.Infrastructure.Security;

/// <summary>Хеширует пароли и проверяет их с помощью алгоритма BCrypt.</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    /// <summary>Создаёт BCrypt-хеш открытого пароля.</summary>
    /// <param name="password">Пароль, который не должен сохраняться в открытом виде.</param>
    /// <returns>Строковое представление BCrypt-хеша с параметрами алгоритма.</returns>
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    /// <summary>Сравнивает открытый пароль с ранее созданным BCrypt-хешем.</summary>
    /// <param name="password">Проверяемый открытый пароль.</param>
    /// <param name="hash">Сохранённое представление BCrypt-хеша.</param>
    /// <returns><see langword="true"/>, если пароль соответствует хешу.</returns>
    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
