using DevTools.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Tests;

public class StorageBackendAndBootstrapperTests
{
    [Fact]
    public void StorageBackendResolver_ResolvesSqlite_WhenEnvVarIsSqlite()
    {
        WithStorageBackendEnv("sqlite", () =>
        {
            var backend = StorageBackendResolver.Resolve();
            Assert.Equal(StorageBackend.Sqlite, backend);
        });
    }

    [Fact]
    public void StorageBackendResolver_FallsBackToJson_WhenEnvVarMissingOrUnknown()
    {
        WithStorageBackendEnv(null, () =>
        {
            var backend = StorageBackendResolver.Resolve();
            Assert.Equal(StorageBackend.Json, backend);
        });

        WithStorageBackendEnv("something-else", () =>
        {
            var backend = StorageBackendResolver.Resolve();
            Assert.Equal(StorageBackend.Json, backend);
        });
    }

    [Fact]
    public void SqliteBootstrapper_EnsureDatabase_IsIdempotentAndConnectable()
    {
        var root = Path.Combine(Path.GetTempPath(), "devtools-bootstrapper-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var provider = new SqlitePathProvider(root);
            var bootstrapper = new SqliteBootstrapper(provider);

            bootstrapper.EnsureDatabase();
            bootstrapper.EnsureDatabase();

            var dbPath = provider.GetDatabasePath();
            Assert.True(File.Exists(dbPath));

            var options = new DbContextOptionsBuilder<DevToolsDbContext>()
                .UseSqlite(provider.GetConnectionString())
                .Options;

            using var db = new DevToolsDbContext(options);
            Assert.True(db.Database.CanConnect());
        }
        finally
        {
            try
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            catch
            {
                // ignore cleanup issues in temp folder
            }
        }
    }

    private static void WithStorageBackendEnv(string? value, Action action)
    {
        const string envVar = "DEVTOOLS_STORAGE_BACKEND";
        var original = Environment.GetEnvironmentVariable(envVar);

        try
        {
            Environment.SetEnvironmentVariable(envVar, value);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVar, original);
        }
    }
}


