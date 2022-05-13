using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using SecurityTokenService.IdentityServer;

namespace SecurityTokenService.Data.PostgreSql
{
    public class PostgreSqlPersistedGrantDbContext : IdentityServer4.EntityFramework.DbContexts.PersistedGrantDbContext<
        PostgreSqlPersistedGrantDbContext>
    {
        public PostgreSqlPersistedGrantDbContext(DbContextOptions<PostgreSqlPersistedGrantDbContext> options,
            OperationalStoreOptions storeOptions) : base(options, storeOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var extensionOptions = this.GetService<IOptionsMonitor<IdentityServerExtensionOptions>>().CurrentValue;
            modelBuilder.ConfigureDefault(extensionOptions.TablePrefix);
        }
    }
}