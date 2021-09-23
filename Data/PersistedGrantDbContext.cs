using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using SecurityTokenService.Extensions;
using SecurityTokenService.Identity;

namespace SecurityTokenService.Data
{
    public class PersistedGrantDbContext : IdentityServer4.EntityFramework.DbContexts.PersistedGrantDbContext<
        PersistedGrantDbContext>
    {
        public PersistedGrantDbContext(DbContextOptions<PersistedGrantDbContext> options,
            OperationalStoreOptions storeOptions) : base(options, storeOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.SetDefaultStringLength();

            var identityExtensionOptions = this.GetService<IOptions<IdentityExtensionOptions>>().Value;
            var tablePrefix = identityExtensionOptions.TablePrefix;

            if (!string.IsNullOrWhiteSpace(tablePrefix))
            {
                modelBuilder.SetTablePrefix(tablePrefix);
            }

            modelBuilder.SetSnakeCaseNaming();
        }
    }
}