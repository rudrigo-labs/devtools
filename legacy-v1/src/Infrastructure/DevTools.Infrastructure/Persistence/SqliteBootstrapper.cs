using System;
using DevTools.Infrastructure.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence;

public sealed class SqliteBootstrapper
{
    private readonly string _databasePath;
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;

    public SqliteBootstrapper(SqlitePathProvider pathProvider)
    {
        _databasePath = pathProvider.GetDatabasePath();
        _dbOptions = new DbContextOptionsBuilder<DevToolsDbContext>()
            .UseSqlite(pathProvider.GetConnectionString())
            .Options;
    }

    public string DatabasePath => _databasePath;

    public void EnsureDatabase()
    {
        try
        {
            using var dbContext = new DevToolsDbContext(_dbOptions);
            dbContext.Database.EnsureCreated();
            InfraLogger.Info($"SQLite ready at '{_databasePath}'.");
        }
        catch (Exception ex)
        {
            InfraLogger.Error("Failed to initialize SQLite database.", ex);
            throw;
        }
    }
}


