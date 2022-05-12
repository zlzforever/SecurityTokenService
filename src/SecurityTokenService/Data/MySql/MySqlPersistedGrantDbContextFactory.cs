using System;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityTokenService.Data.MySql
{
    public class MySqlPersistedGrantDbContextFactory : IDesignTimeDbContextFactory<MySqlPersistedGrantDbContext>
    {
        public MySqlPersistedGrantDbContext CreateDbContext(string[] args)
        {
            var service = Program.CreateHostBuilder(Array.Empty<string>()).Build().Services;
            return (MySqlPersistedGrantDbContext)service.CreateScope()
                .ServiceProvider.GetRequiredService(typeof(MySqlPersistedGrantDbContext));
        }
    }
}