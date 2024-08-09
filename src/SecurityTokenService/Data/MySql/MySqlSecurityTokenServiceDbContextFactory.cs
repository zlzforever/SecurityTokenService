using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityTokenService.Data.MySql;

public class MySqlSecurityTokenServiceDbContextFactory
    : IDesignTimeDbContextFactory<MySqlSecurityTokenServiceDbContext>
{
    public MySqlSecurityTokenServiceDbContext CreateDbContext(string[] args)
    {
        var service = Program.CreateApp([]).Services;
        return (MySqlSecurityTokenServiceDbContext)service.CreateScope()
            .ServiceProvider.GetRequiredService(typeof(MySqlSecurityTokenServiceDbContext));
    }
}
