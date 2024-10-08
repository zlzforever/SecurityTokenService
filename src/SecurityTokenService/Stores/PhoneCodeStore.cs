using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Npgsql;
using SecurityTokenService.Extensions;

namespace SecurityTokenService.Stores;

public class PhoneCodeStore : IPhoneCodeStore
{
    private readonly IConfiguration _configuration;

    public PhoneCodeStore(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        var tablePrefix = _configuration["Identity:TablePrefix"];
        tablePrefix = string.IsNullOrWhiteSpace(tablePrefix) ? string.Empty : tablePrefix.Trim();
        // TODO: 不同数据库不同脚本
        var sql = $@"
create table if not exists {tablePrefix}sms_code
(
    phone_number                     varchar(16) not null primary key,
    code                   varchar(8),
    modification_time      integer
)";
        await using var conn = GetConnection();
        await conn.ExecuteAsync(sql, commandTimeout: 30);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <param name="ttl">秒</param>
    /// <returns></returns>
    public async Task<string> GetAsync(string phoneNumber, int ttl = 300)
    {
        var sql = GetSelectSql();
        await using var conn = GetConnection();
        var code = await conn.QueryFirstOrDefaultAsync<string>(sql, new { phoneNumber, ttl }, commandTimeout: 30);
        return code;
    }

    public async Task UpdateAsync(string phoneNumber, string code)
    {
        await using var conn = GetConnection();

        await conn.ExecuteAsync(GetUpdateSql(), new { phoneNumber, code }, commandTimeout: 30);
    }

    private DbConnection GetConnection()
    {
        var connectionString = _configuration["ConnectionStrings:Identity"];

        if ("mysql".Equals(_configuration.GetDatabaseType(), StringComparison.OrdinalIgnoreCase))
        {
            return new MySqlConnection(connectionString);
        }

        return new NpgsqlConnection(connectionString);
    }

    private string GetUpdateSql()
    {
        var tablePrefix = _configuration["Identity:TablePrefix"];

        if ("mysql".Equals(_configuration.GetDatabaseType(), StringComparison.OrdinalIgnoreCase))
        {
            return
                $@"INSERT INTO {tablePrefix}sms_code (phone_number, code, modification_time) VALUES (@phoneNumber, @code, UNIX_TIMESTAMP()) 
ON DUPLICATE KEY UPDATE code = @code, modification_time = UNIX_TIMESTAMP()";
        }

        return
            $@"INSERT INTO {tablePrefix}sms_code (phone_number, code, modification_time) VALUES (@phoneNumber, @code, floor(extract(epoch from now()))) 
on conflict (phone_number) do update set code = @code, modification_time = floor(extract(epoch from now()))";
    }

    private string GetSelectSql()
    {
        var tablePrefix = _configuration["Identity:TablePrefix"];
        if ("mysql".Equals(_configuration.GetDatabaseType(), StringComparison.OrdinalIgnoreCase))
        {
            return
                $"SELECT code FROM {tablePrefix}sms_code WHERE phone_number = @phoneNumber AND modification_time >= UNIX_TIMESTAMP() - @ttl;";
        }

        return
            $"SELECT code FROM {tablePrefix}sms_code WHERE phone_number = @phoneNumber AND modification_time >= floor(extract(epoch from now())) - @ttl;";
    }
}
