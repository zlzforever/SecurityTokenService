using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using SecurityTokenService.Identity;

namespace SecurityTokenService.Data.PostgreSql
{
    public class PostgreSqlSecurityTokenServiceDbContext(
        DbContextOptions<PostgreSqlSecurityTokenServiceDbContext> options)
        : IdentityDbContext<User>(options), IDataProtectionKeyContext
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var identityExtensionOptions = this.GetService<IOptionsMonitor<IdentityExtensionOptions>>().CurrentValue;
            builder.ConfigureIdentity(identityExtensionOptions);
            builder.ConfigureDefault(identityExtensionOptions.TablePrefix);

            var entityTypeBuilder = builder.Entity<DataProtectionKey>();
            entityTypeBuilder.ToTable("system_data_protection_keys");
            entityTypeBuilder.Property(x => x.Id).HasColumnName("id");
            entityTypeBuilder.Property(x => x.FriendlyName).HasMaxLength(64).HasColumnName("friendly_name");
            entityTypeBuilder.Property(x => x.Xml).HasMaxLength(1200).HasColumnName("xml")
                .HasConversion(v => Util.Encrypt(Util.DataProtectionKeyAes, v),
                    v => Util.Decrypt(Util.DataProtectionKeyAes, v));

            entityTypeBuilder.HasKey(x => x.Id);
        }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    }
}
