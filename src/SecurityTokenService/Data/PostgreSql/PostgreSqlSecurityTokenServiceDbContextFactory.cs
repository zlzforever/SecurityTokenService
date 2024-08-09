using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityTokenService.Data.PostgreSql;

public class PostgreSqlSecurityTokenServiceDbContextFactory
    : IDesignTimeDbContextFactory<PostgreSqlSecurityTokenServiceDbContext>
{
    public PostgreSqlSecurityTokenServiceDbContext CreateDbContext(string[] args)
    {
        var service = Program.CreateApp([]).Services;
        return (PostgreSqlSecurityTokenServiceDbContext)service.CreateScope()
            .ServiceProvider.GetRequiredService(typeof(PostgreSqlSecurityTokenServiceDbContext));
    }
}
