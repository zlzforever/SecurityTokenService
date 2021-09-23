using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using SecurityTokenService.Extensions;
using SecurityTokenService.Identity;

namespace SecurityTokenService.Data
{
    public class SecurityTokenServiceDbContext : IdentityDbContext
    {
        public SecurityTokenServiceDbContext(DbContextOptions<SecurityTokenServiceDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var identityExtensionOptions = this.GetService<IOptions<IdentityExtensionOptions>>().Value;

            if (identityExtensionOptions.Tables != null)
            {
                if (!string.IsNullOrEmpty(identityExtensionOptions.Tables.User))
                {
                    builder.Entity<IdentityUser>(b => { b.ToTable(identityExtensionOptions.Tables.User); });
                }

                if (!string.IsNullOrEmpty(identityExtensionOptions.Tables.Role))
                {
                    builder.Entity<IdentityRole>(b => { b.ToTable(identityExtensionOptions.Tables.Role); });
                }

                if (!string.IsNullOrEmpty(identityExtensionOptions.Tables.UserRole))
                {
                    builder.Entity<IdentityUserRole<string>>(b =>
                    {
                        b.ToTable(identityExtensionOptions.Tables.UserRole);
                    });
                }

                if (!string.IsNullOrEmpty(identityExtensionOptions.Tables.RoleClaim))
                {
                    builder.Entity<IdentityRoleClaim<string>>(
                        b => { b.ToTable(identityExtensionOptions.Tables.RoleClaim); });
                }

                if (!string.IsNullOrEmpty(identityExtensionOptions.Tables.UserClaim))
                {
                    builder.Entity<IdentityUserClaim<string>>(
                        b => { b.ToTable(identityExtensionOptions.Tables.UserClaim); });
                }

                if (!string.IsNullOrEmpty(identityExtensionOptions.Tables.UserLogin))
                {
                    builder.Entity<IdentityUserLogin<string>>(
                        b => { b.ToTable(identityExtensionOptions.Tables.UserLogin); });
                }

                if (!string.IsNullOrEmpty(identityExtensionOptions.Tables.UserToken))
                {
                    builder.Entity<IdentityUserToken<string>>(
                        b => { b.ToTable(identityExtensionOptions.Tables.UserToken); });
                }

                builder.SetDefaultStringLength();

                var tablePrefix = identityExtensionOptions.TablePrefix;
                if (!string.IsNullOrWhiteSpace(tablePrefix))
                {
                    builder.SetTablePrefix(tablePrefix);
                }
            }

            builder.SetSnakeCaseNaming();
        }
    }
}