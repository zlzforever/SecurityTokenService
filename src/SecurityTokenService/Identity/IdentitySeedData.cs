using System;
using System.Data.Common;
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Npgsql;
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
            var connectionString = configuration["ConnectionStrings:Identity"];
            DbContext securityTokenServiceDbContext;
            DbConnection conn;
            if (configuration.GetDatabaseType() == "MySql")
            {
                conn = new MySqlConnection(connectionString);
                conn.Execute($"""
                              create table if not exists system_data_protection_keys
                              (
                                  id int auto_increment primary key,
                                  friendly_name varchar(64) not null,
                                  xml varchar(2000) not null
                              );
                              """
                );

                securityTokenServiceDbContext =
                    scope.ServiceProvider.GetRequiredService<MySqlSecurityTokenServiceDbContext>();
            }
            else
            {
                conn = new NpgsqlConnection(connectionString);
                conn.Execute($"""
                              create table if not exists system_data_protection_keys
                              (
                                  id serial primary key,
                                  friendly_name varchar(64) not null,
                                  xml varchar(2000) not null
                              );
                              """
                );

                securityTokenServiceDbContext =
                    scope.ServiceProvider.GetRequiredService<PostgreSqlSecurityTokenServiceDbContext>();
            }

            conn.Dispose();

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
