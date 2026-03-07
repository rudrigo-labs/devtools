using DevTools.Core.Configuration;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence.Stores;

public static class ToolConfigurationStoreFactory
{
    public static IToolConfigurationStore Create(StorageBackend backend)
    {
        if (backend != StorageBackend.Sqlite)
        {
            return new JsonFileToolConfigurationStore();
        }

        try
        {
            var pathProvider = new SqlitePathProvider();
            var dbOptions = new DbContextOptionsBuilder<DevToolsDbContext>()
                .UseSqlite(pathProvider.GetConnectionString())
                .Options;

            return new SqliteToolConfigurationStore(dbOptions);
        }
        catch
        {
            return new JsonFileToolConfigurationStore();
        }
    }
}
