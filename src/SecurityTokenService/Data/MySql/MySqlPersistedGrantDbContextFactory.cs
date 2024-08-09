using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityTokenService.Data.MySql;

public class MySqlPersistedGrantDbContextFactory : IDesignTimeDbContextFactory<MySqlPersistedGrantDbContext>
{
    public MySqlPersistedGrantDbContext CreateDbContext(string[] args)
    {
        var service = Program.CreateApp([]).Services;
        return (MySqlPersistedGrantDbContext)service.CreateScope()
            .ServiceProvider.GetRequiredService(typeof(MySqlPersistedGrantDbContext));
    }
}
