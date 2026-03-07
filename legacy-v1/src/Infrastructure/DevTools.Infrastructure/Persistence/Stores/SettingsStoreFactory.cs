using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence.Stores;

public static class SettingsStoreFactory
{
    public static ISettingsStore Create(StorageBackend backend, string jsonConfigPath)
    {
        if (backend != StorageBackend.Sqlite)
        {
            return new JsonSettingsStore(jsonConfigPath);
        }

        var pathProvider = new SqlitePathProvider();
        var dbPath = pathProvider.GetDatabasePath();
        var dbOptions = new DbContextOptionsBuilder<DevToolsDbContext>()
            .UseSqlite(pathProvider.GetConnectionString())
            .Options;

        return new SqliteSettingsStore(dbOptions, dbPath);
    }
}
