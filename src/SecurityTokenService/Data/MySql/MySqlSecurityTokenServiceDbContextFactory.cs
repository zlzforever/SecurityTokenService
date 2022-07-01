using System;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityTokenService.Data.MySql
{
    public class
        MySqlSecurityTokenServiceDbContextFactory : IDesignTimeDbContextFactory<MySqlSecurityTokenServiceDbContext>
    {
        public MySqlSecurityTokenServiceDbContext CreateDbContext(string[] args)
        {
            var service = Program.CreateHostBuilder(Array.Empty<string>()).Build().Services;
            return (MySqlSecurityTokenServiceDbContext)service.CreateScope()
                .ServiceProvider.GetRequiredService(typeof(MySqlSecurityTokenServiceDbContext));
        }
    }
}
