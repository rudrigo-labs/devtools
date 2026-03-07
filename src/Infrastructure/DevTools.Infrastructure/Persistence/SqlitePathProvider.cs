using System.IO;
using Microsoft.Data.Sqlite;

namespace DevTools.Infrastructure.Persistence;

public sealed class SqlitePathProvider
{
    private const string DatabasePathEnvVar = "DEVTOOLS_SQLITE_PATH";
    private readonly string _appDataRoot;

    public SqlitePathProvider()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevTools"))
    {
    }

    public SqlitePathProvider(string appDataRoot)
    {
        _appDataRoot = appDataRoot;
    }

    public string AppDataRoot => _appDataRoot;

    public string GetDatabasePath()
    {
        var envPath = Environment.GetEnvironmentVariable(DatabasePathEnvVar);
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            var fullPath = Path.GetFullPath(envPath);
            var envDir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(envDir))
            {
                Directory.CreateDirectory(envDir);
            }

            return fullPath;
        }

        Directory.CreateDirectory(_appDataRoot);
        return Path.Combine(_appDataRoot, "devtools.db");
    }

    public string GetConnectionString()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = GetDatabasePath(),
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true,
            ForeignKeys = true
        };

        return builder.ToString();
    }
}

