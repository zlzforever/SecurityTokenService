using System.Globalization;
using System.Linq;
using EFCore.NamingConventions.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace SecurityTokenService.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static void SetDefaultStringLength(this ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                // 配置文本默认长度
                foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(string)))
                {
                    // 未配置
                    var maxLength = property.GetMaxLength();
                    if (maxLength is > 0)
                    {
                        continue;
                    }

                    property.SetMaxLength(!property.IsKey() ? 64 : 255);
                }
            }
        }

        public static void SetTablePrefix(this ModelBuilder builder, string tablePrefix)
        {
            if (tablePrefix == null)
            {
                return;
            }

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (!entityType.IsOwned())
                {
                    var tableName = tablePrefix + entityType.GetTableName();
                    entityType.SetTableName(tableName);
                }
            }
        }

        public static void SetSnakeCaseNaming(this ModelBuilder builder)
        {
            var nameRewriter = new SnakeCaseNameRewriter(CultureInfo.InvariantCulture);

            foreach (var entity in builder.Model.GetEntityTypes())
            {
                if (entity.IsOwned())
                {
                    continue;
                }

                var tableName = entity.GetTableName();

                if (tableName.Any(char.IsUpper))
                {
                    entity.SetTableName(nameRewriter.RewriteName(tableName));
                }

                foreach (var property in entity.GetProperties())
                {
                    var storeObjectIdentifier = StoreObjectIdentifier.Create(entity, StoreObjectType.Table);
                    var propertyName = property.GetColumnName(storeObjectIdentifier.GetValueOrDefault());
                    if (propertyName.Any(char.IsUpper))
                    {
                        property.SetColumnName(nameRewriter.RewriteName(propertyName));
                    }
                }
            }
        }
    }
}