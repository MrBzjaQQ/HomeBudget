namespace Budget.Infrastructure.Data;

/// <summary>Содержит ключи и имена провайдеров конфигурации базы данных.</summary>
public static class DatabaseConfiguration
{
    /// <summary>Ключ конфигурации, задающий используемый провайдер базы данных.</summary>
    public const string ProviderKey = "Database:Provider";

    /// <summary>Ключ конфигурации с именем InMemory-базы для тестов.</summary>
    public const string InMemoryNameKey = "Database:InMemoryName";

    /// <summary>Ключ строки подключения, общий для runtime и design-time настройки.</summary>
    public const string DefaultConnectionStringKey = "ConnectionStrings:Default";

    /// <summary>Имя провайдера PostgreSQL, используемое по умолчанию.</summary>
    public const string NpgsqlProvider = "Npgsql";

    /// <summary>Имя провайдера базы данных в памяти для тестирования.</summary>
    public const string InMemoryProvider = "InMemory";
}
