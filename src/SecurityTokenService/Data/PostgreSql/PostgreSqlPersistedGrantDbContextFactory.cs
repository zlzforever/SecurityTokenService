using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityTokenService.Data.PostgreSql;

public class PostgreSqlPersistedGrantDbContextFactory
    : IDesignTimeDbContextFactory<PostgreSqlPersistedGrantDbContext>
{
    public PostgreSqlPersistedGrantDbContext CreateDbContext(string[] args)
    {
        var service = Program.CreateApp([]).Services;
        return (PostgreSqlPersistedGrantDbContext)service.CreateScope()
            .ServiceProvider.GetRequiredService(typeof(PostgreSqlPersistedGrantDbContext));
    }
}
