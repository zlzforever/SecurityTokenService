using System;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityTokenService.Data.PostgreSql
{
    public class PostgreSqlSecurityTokenServiceDbContextFactory : IDesignTimeDbContextFactory<PostgreSqlSecurityTokenServiceDbContext>
    {
        public PostgreSqlSecurityTokenServiceDbContext CreateDbContext(string[] args)
        {
            var service = Program.CreateHostBuilder(Array.Empty<string>()).Build().Services;
            return (PostgreSqlSecurityTokenServiceDbContext)service.CreateScope()
                .ServiceProvider.GetRequiredService(typeof(PostgreSqlSecurityTokenServiceDbContext));
        }
    }
}