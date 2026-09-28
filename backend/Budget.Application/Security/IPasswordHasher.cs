namespace Budget.Application.Security;

/// <summary>Хеширование и проверка паролей.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
