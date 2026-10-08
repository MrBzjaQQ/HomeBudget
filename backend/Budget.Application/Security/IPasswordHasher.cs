namespace Budget.Application.Security;

/// <summary>Определяет операции безопасного хеширования паролей и проверки совпадения.</summary>
public interface IPasswordHasher
{
    /// <summary>Создаёт необратимое представление пароля для хранения.</summary>
    /// <param name="password">Пароль в открытом виде.</param>
    /// <returns>Строка хеша, пригодная для последующей проверки.</returns>
    string Hash(string password);

    /// <summary>Проверяет пароль относительно ранее сохранённого хеша.</summary>
    /// <param name="password">Проверяемый пароль в открытом виде.</param>
    /// <param name="hash">Хеш, сохранённый для учётной записи.</param>
    /// <returns><see langword="true"/>, если пароль соответствует хешу; иначе <see langword="false"/>.</returns>
    bool Verify(string password, string hash);
}
