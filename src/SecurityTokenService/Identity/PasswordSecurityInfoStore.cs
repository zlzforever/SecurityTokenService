using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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

        if (_configuration["Database"] == "MySql")
        {
            await conn.ExecuteAsync("UPDATE ", new { });
        }
        else
        {
            await conn.ExecuteAsync("UPDATE ", new { });
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
