namespace DevTools.Migrations.Models;

public sealed class MigrationTarget
{
    public DatabaseProvider Provider { get; set; }
    public string MigrationsProjectPath { get; set; } = string.Empty;
}
