using System;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityTokenService.Data
{
    public class SecurityTokenServiceDbContextFactory : IDesignTimeDbContextFactory<SecurityTokenServiceDbContext>
    {
        public SecurityTokenServiceDbContext CreateDbContext(string[] args)
        {
            var service = Program.CreateHostBuilder(Array.Empty<string>()).Build().Services;
            return (SecurityTokenServiceDbContext)service.CreateScope()
                .ServiceProvider.GetRequiredService(typeof(SecurityTokenServiceDbContext));
        }
    }
}