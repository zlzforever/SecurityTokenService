using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Npgsql;

namespace SecurityTokenService.Identity;

public class PasswordSecurityInfoStore : IPasswordSecurityInfoStore
{
    private readonly IConfiguration _configuration;

    public PasswordSecurityInfoStore(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task UpdateAsync(string userId, int passwordLength, bool passwordContainsDigit,
        bool passwordContainsLowercase,
        bool passwordContainsUppercase, bool passwordContainsNonAlphanumeric)
    {
        await using var conn = GetConnection();

        var connectString = _configuration["ConnectionStrings:Identity"];
        var tablePrefix = _configuration["Identity:TablePrefix"];

        int effected = 0;
        
        if (_configuration["Database"] == "MySql")
        {
            await conn.ExecuteAsync(
                @$"UPDATE {tablePrefix}user SET password_length = @passwordLength, password_contains_digit = @passwordContainsDigit, 
            password_contains_lowercase = @passwordContainsLowercase, password_contains_uppercase = @passwordContainsUppercase, password_contains_non_alphanumeric = @passwordContainsNonAlphanumeric WHERE id = @userId",
                new
                {
                    userId,
                    passwordLength,
                    passwordContainsDigit,
                    passwordContainsLowercase,
                    passwordContainsUppercase,
                    passwordContainsNonAlphanumeric
                });
        }
        else
        {
            await conn.ExecuteAsync(
                @$"UPDATE {tablePrefix}user SET password_length = @passwordLength, password_contains_digit = @passwordContainsDigit, 
            password_contains_lowercase = @passwordContainsLowercase, password_contains_uppercase = @passwordContainsUppercase, password_contains_non_alphanumeric = @passwordContainsNonAlphanumeric WHERE id = @userId",
                new
                {
                    userId,
                    passwordLength,
                    passwordContainsDigit,
                    passwordContainsLowercase,
                    passwordContainsUppercase,
                    passwordContainsNonAlphanumeric
                });
        }
    }

    private DbConnection GetConnection()
    {
        var connectionString = _configuration["ConnectionStrings:Identity"];
        var database = _configuration["Database"].ToLower();

        DbConnection conn = database switch
        {
            "mysql" => new MySqlConnection(connectionString),
            _ => new NpgsqlConnection(connectionString)
        };
        return conn;
    }
}