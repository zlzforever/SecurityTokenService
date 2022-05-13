using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecurityTokenService.Data;
using SecurityTokenService.Data.MySql;
using SecurityTokenService.Data.PostgreSql;

namespace SecurityTokenService.Extensions
{
    public static class IdentityExtensions
    {
        public static void LoadIdentityData(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            DbContext securityTokenServiceDbContext;
            if (configuration["Database"] == "MySql")
            {
                securityTokenServiceDbContext =
                    scope.ServiceProvider.GetRequiredService<MySqlSecurityTokenServiceDbContext>();
            }
            else
            {
                securityTokenServiceDbContext =
                    scope.ServiceProvider.GetRequiredService<PostgreSqlSecurityTokenServiceDbContext>();
            }

            if (string.Equals(configuration["Identity:SelfHost"], "true", StringComparison.InvariantCultureIgnoreCase))
            {
                securityTokenServiceDbContext.Database.Migrate();
            }

            var seedData = scope.ServiceProvider.GetRequiredService<SeedData>();
            seedData.Load();
            securityTokenServiceDbContext.Dispose();
        }
    }
}