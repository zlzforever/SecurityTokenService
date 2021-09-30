using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SecurityTokenService.Data;
using SecurityTokenService.Identity;

namespace SecurityTokenService.Extensions
{
    public static class IdentityExtensions
    {
        public static void LoadUserQuerySql(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            using var securityTokenServiceDbContext =
                scope.ServiceProvider.GetRequiredService<SecurityTokenServiceDbContext>();
            securityTokenServiceDbContext.Database.Migrate();

            var storeObjectIdentifier = StoreObjectIdentifier.SqlQuery(securityTokenServiceDbContext.Users.EntityType);
            var name = securityTokenServiceDbContext.Users.EntityType.GetProperty("UserName")
                .GetColumnName(in storeObjectIdentifier);
            var email = securityTokenServiceDbContext.Users.EntityType.GetProperty("Email")
                .GetColumnName(in storeObjectIdentifier);
            var phone = securityTokenServiceDbContext.Users.EntityType.GetProperty("PhoneNumber")
                .GetColumnName(in storeObjectIdentifier);
            var identityExtensionOptions = app.ApplicationServices
                .GetRequiredService<IOptionsMonitor<IdentityExtensionOptions>>().CurrentValue;
            var sql = string.IsNullOrEmpty(identityExtensionOptions.SoftDeleteColumn)
                ? $"SELECT * FROM {securityTokenServiceDbContext.Users.EntityType.GetTableName()} WHERE {name} = {{0}} OR {email} = {{0}} OR {phone} = {{0}} LIMIT 1"
                : $"SELECT * FROM {securityTokenServiceDbContext.Users.EntityType.GetTableName()} WHERE ({name} = {{0}} OR {email} = {{0}} OR {phone} = {{0}}) AND {identityExtensionOptions.SoftDeleteColumn} == false LIMIT 1";
            Constants.LoginUserQuerySql = sql;
        }

        public static void LoadIdentityData(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();

            using var persistedGrantDbContext = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
            persistedGrantDbContext.Database.Migrate();

            using var securityTokenServiceDbContext =
                scope.ServiceProvider.GetRequiredService<SecurityTokenServiceDbContext>();

            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            if (string.Equals(configuration["Identity:SelfHost"], "true", StringComparison.InvariantCultureIgnoreCase))
            {
                securityTokenServiceDbContext.Database.Migrate();
            }

            var seedData = scope.ServiceProvider.GetRequiredService<SeedData>();
            seedData.Load();
        }
    }
}