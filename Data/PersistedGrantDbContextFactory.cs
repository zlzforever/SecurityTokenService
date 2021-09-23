using System;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityTokenService.Data
{
    public class PersistedGrantDbContextFactory : IDesignTimeDbContextFactory<PersistedGrantDbContext>
    {
        public PersistedGrantDbContext CreateDbContext(string[] args)
        {
            var service = Program.CreateHostBuilder(Array.Empty<string>()).Build().Services;
            return (PersistedGrantDbContext)service.CreateScope()
                .ServiceProvider.GetRequiredService(typeof(PersistedGrantDbContext));
        }
    }
}