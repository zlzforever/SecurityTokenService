using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using SecurityTokenService.Identity;

namespace SecurityTokenService.Data.MySql;

public class MySqlPersistedGrantDbContext : IdentityServer4.EntityFramework.DbContexts.PersistedGrantDbContext<
    MySqlPersistedGrantDbContext>
{
    public MySqlPersistedGrantDbContext(DbContextOptions<MySqlPersistedGrantDbContext> options,
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