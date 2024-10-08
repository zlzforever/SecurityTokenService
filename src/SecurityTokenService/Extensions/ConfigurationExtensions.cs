using Microsoft.Extensions.Configuration;

namespace SecurityTokenService.Extensions;

public static class ConfigurationExtensions
{
    public static string GetDatabaseType(this IConfiguration configuration)
    {
        return configuration["DATABASE_TYPE"] ?? configuration["DatabaseType"] ?? configuration["Database"];
    }
}
