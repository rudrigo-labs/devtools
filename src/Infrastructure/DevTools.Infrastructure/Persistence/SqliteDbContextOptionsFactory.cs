using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence;

public sealed class SqliteDbContextOptionsFactory
{
    private readonly SqlitePathProvider _pathProvider;

    public SqliteDbContextOptionsFactory(SqlitePathProvider pathProvider)
    {
        _pathProvider = pathProvider;
    }

    public DbContextOptions<DevToolsDbContext> Create()
    {
        var builder = new DbContextOptionsBuilder<DevToolsDbContext>()
            .UseSqlite(_pathProvider.GetConnectionString())
            .AddInterceptors(new SqlitePragmaConnectionInterceptor());

        return builder.Options;
    }
}

