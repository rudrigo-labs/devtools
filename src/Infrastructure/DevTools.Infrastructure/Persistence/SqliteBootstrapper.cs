using DevTools.Infrastructure.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence;

public sealed class SqliteBootstrapper
{
    private readonly SqlitePathProvider _pathProvider;
    private readonly SqliteDbContextOptionsFactory _optionsFactory;

    public SqliteBootstrapper(SqlitePathProvider pathProvider, SqliteDbContextOptionsFactory optionsFactory)
    {
        _pathProvider = pathProvider;
        _optionsFactory = optionsFactory;
    }

    public string DatabasePath => _pathProvider.GetDatabasePath();

    public void Migrate()
    {
        try
        {
            using var dbContext = new DevToolsDbContext(_optionsFactory.Create());
            dbContext.Database.Migrate();
            InfraLogger.Info($"SQLite migrated at '{DatabasePath}'.");
        }
        catch (Exception ex)
        {
            InfraLogger.Error("Failed to migrate SQLite database.", ex);
            throw;
        }
    }

    public async Task MigrateAsync(CancellationToken ct = default)
    {
        try
        {
            await using var dbContext = new DevToolsDbContext(_optionsFactory.Create());
            await dbContext.Database.MigrateAsync(ct);
            InfraLogger.Info($"SQLite migrated at '{DatabasePath}'.");
        }
        catch (Exception ex)
        {
            InfraLogger.Error("Failed to migrate SQLite database.", ex);
            throw;
        }
    }

    public bool CanConnect()
    {
        try
        {
            using var dbContext = new DevToolsDbContext(_optionsFactory.Create());
            return dbContext.Database.CanConnect();
        }
        catch (Exception ex)
        {
            InfraLogger.Error("Failed to connect to SQLite database.", ex);
            return false;
        }
    }
}

