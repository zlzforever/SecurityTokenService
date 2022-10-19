using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecurityTokenService.Extensions;
using SecurityTokenService.Identity;

namespace SecurityTokenService.Data;

public static class ModelBuilderExtensions
{
    public static void ConfigureIdentity(this ModelBuilder builder,
        IdentityExtensionOptions identityExtensionOptions)
    {
        builder.Entity<User>(b =>
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

            if (!string.IsNullOrWhiteSpace(identityExtensionOptions.SoftDeleteColumn))
            {
                b.Property<bool>(identityExtensionOptions.SoftDeleteColumn);
            }

            b.Property(x => x.FamilyName).HasMaxLength(100);
            b.Property(x => x.GivenName).HasMaxLength(100);
            b.Property(x => x.Picture).HasMaxLength(500);
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
    }

    public static void ConfigureDefault(this ModelBuilder builder,
        string tablePrefix)
    {
        builder.SetDefaultStringLength();

        // var tablePrefix = identityExtensionOptions.TablePrefix;

        if (!string.IsNullOrWhiteSpace(tablePrefix))
        {
            builder.SetTablePrefix(tablePrefix);
        }

        builder.SetSnakeCaseNaming();
    }
}