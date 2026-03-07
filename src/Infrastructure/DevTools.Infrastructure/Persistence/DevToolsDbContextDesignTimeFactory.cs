using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DevTools.Infrastructure.Persistence;

public sealed class DevToolsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<DevToolsDbContext>
{
    public DevToolsDbContext CreateDbContext(string[] args)
    {
        var pathProvider = new SqlitePathProvider();
        var optionsFactory = new SqliteDbContextOptionsFactory(pathProvider);
        return new DevToolsDbContext(optionsFactory.Create());
    }
}

