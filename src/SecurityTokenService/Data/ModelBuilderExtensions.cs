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
            b.ToTable("user");

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
            b.ToTable("role");
        });
        builder.Entity<IdentityUserRole<string>>(b =>
        {
            b.ToTable("user_role");
        });
        builder.Entity<IdentityRoleClaim<string>>(b =>
        {
            b.ToTable("role_claim");
        });
        builder.Entity<IdentityUserClaim<string>>(b =>
        {
            b.ToTable("user_claim");
        });
        builder.Entity<IdentityUserLogin<string>>(b =>
        {
            b.ToTable("user_login");
        });
        builder.Entity<IdentityUserToken<string>>(b =>
        {
            b.ToTable("user_token");
        });
    }

    public static void ConfigureDefault(this ModelBuilder builder,
        string tablePrefix)
    {
        builder.SetDefaultStringLength();

        if (!string.IsNullOrWhiteSpace(tablePrefix))
        {
            builder.SetTablePrefix(tablePrefix);
        }

        builder.SetSnakeCaseNaming();
    }
}
