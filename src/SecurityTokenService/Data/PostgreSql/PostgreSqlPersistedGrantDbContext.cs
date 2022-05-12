using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using SecurityTokenService.Extensions;
using SecurityTokenService.Identity;

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
            var identityExtensionOptions = this.GetService<IOptionsMonitor<IdentityExtensionOptions>>().CurrentValue;
            modelBuilder.ConfigureDefault(identityExtensionOptions);
        }
    }
}