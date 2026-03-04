using System;
using System.IO;

namespace DevTools.Presentation.Wpf.Persistence;

public sealed class SqlitePathProvider
{
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
        Directory.CreateDirectory(_appDataRoot);
        return Path.Combine(_appDataRoot, "devtools.db");
    }

    public string GetConnectionString()
    {
        return $"Data Source={GetDatabasePath()}";
    }
}

