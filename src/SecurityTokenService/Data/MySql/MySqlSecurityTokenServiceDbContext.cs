using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using SecurityTokenService.Data.PostgreSql;
using SecurityTokenService.Identity;

namespace SecurityTokenService.Data.MySql;

public class MySqlSecurityTokenServiceDbContext : IdentityDbContext
{
    public MySqlSecurityTokenServiceDbContext(DbContextOptions<MySqlSecurityTokenServiceDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var identityExtensionOptions = this.GetService<IOptionsMonitor<IdentityExtensionOptions>>().CurrentValue;
        builder.ConfigureIdentity(identityExtensionOptions);
        builder.ConfigureDefault(identityExtensionOptions);
    }
}