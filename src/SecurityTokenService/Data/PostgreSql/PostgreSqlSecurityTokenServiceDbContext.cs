using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using SecurityTokenService.Identity;

namespace SecurityTokenService.Data.PostgreSql
{
    public class PostgreSqlSecurityTokenServiceDbContext : IdentityDbContext
    {
        public PostgreSqlSecurityTokenServiceDbContext(
            DbContextOptions<PostgreSqlSecurityTokenServiceDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var identityExtensionOptions = this.GetService<IOptionsMonitor<IdentityExtensionOptions>>().CurrentValue;
            builder.ConfigureIdentity(identityExtensionOptions);
            builder.ConfigureDefault(identityExtensionOptions.TablePrefix);
        }
    }
}