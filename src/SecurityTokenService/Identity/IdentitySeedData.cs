using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecurityTokenService.Data;
using SecurityTokenService.Data.MySql;
using SecurityTokenService.Data.PostgreSql;
using SecurityTokenService.Extensions;
using SecurityTokenService.Stores;

namespace SecurityTokenService.Identity
{
    public static class IdentitySeedData
    {
        public static void Load(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            DbContext securityTokenServiceDbContext;
            if (configuration.GetDatabaseType() == "MySql")
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

            var phoneCodeStore = scope.ServiceProvider.GetService<IPhoneCodeStore>();
            phoneCodeStore?.InitializeAsync().Wait();

            var seedData = scope.ServiceProvider.GetRequiredService<SeedData>();
            seedData.Load();
            securityTokenServiceDbContext.Dispose();
        }
    }
}
