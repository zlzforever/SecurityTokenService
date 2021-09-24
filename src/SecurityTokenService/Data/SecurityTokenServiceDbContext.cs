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

            var identityExtensionOptions = this.GetService<IOptionsMonitor<IdentityExtensionOptions>>().CurrentValue;

            builder.Entity<IdentityUser>(b =>
            {
                if (identityExtensionOptions.Tables != null &&
                    !string.IsNullOrEmpty(identityExtensionOptions.Tables.User))
                {
                    b.ToTable(identityExtensionOptions.Tables.User);
                }
                else
                {
                    b.ToTable("user");
                }
            });
            builder.Entity<IdentityRole>(b =>
            {
                if (identityExtensionOptions.Tables != null &&
                    !string.IsNullOrEmpty(identityExtensionOptions.Tables.Role))
                {
                    b.ToTable(identityExtensionOptions.Tables.Role);
                }
                else
                {
                    b.ToTable("role");
                }
            });
            builder.Entity<IdentityUserRole<string>>(b =>
            {
                if (identityExtensionOptions.Tables != null &&
                    !string.IsNullOrEmpty(identityExtensionOptions.Tables.UserRole))
                {
                    b.ToTable(identityExtensionOptions.Tables.UserRole);
                }
                else
                {
                    b.ToTable("user_role");
                }
            });
            builder.Entity<IdentityRoleClaim<string>>(b =>
            {
                if (identityExtensionOptions.Tables != null &&
                    !string.IsNullOrEmpty(identityExtensionOptions.Tables.RoleClaim))
                {
                    b.ToTable(identityExtensionOptions.Tables.RoleClaim);
                }
                else
                {
                    b.ToTable("role_claim");
                }
            });
            builder.Entity<IdentityUserClaim<string>>(b =>
            {
                if (identityExtensionOptions.Tables != null &&
                    !string.IsNullOrEmpty(identityExtensionOptions.Tables.UserClaim))
                {
                    b.ToTable(identityExtensionOptions.Tables.UserClaim);
                }
                else
                {
                    b.ToTable("user_claim");
                }
            });
            builder.Entity<IdentityUserLogin<string>>(b =>
            {
                if (identityExtensionOptions.Tables != null &&
                    !string.IsNullOrEmpty(identityExtensionOptions.Tables.UserLogin))
                {
                    b.ToTable(identityExtensionOptions.Tables.UserLogin);
                }
                else
                {
                    b.ToTable("user_login");
                }
            });
            builder.Entity<IdentityUserToken<string>>(b =>
            {
                if (identityExtensionOptions.Tables != null &&
                    !string.IsNullOrEmpty(identityExtensionOptions.Tables.UserToken))
                {
                    b.ToTable(identityExtensionOptions.Tables.UserToken);
                }
                else
                {
                    b.ToTable("user_token");
                }
            });

            builder.SetDefaultStringLength();

            var tablePrefix = identityExtensionOptions.TablePrefix;
            if (!string.IsNullOrWhiteSpace(tablePrefix))
            {
                builder.SetTablePrefix(tablePrefix);
            }

            builder.SetSnakeCaseNaming();
        }
    }
}