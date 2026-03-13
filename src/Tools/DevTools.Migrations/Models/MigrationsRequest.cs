namespace DevTools.Migrations.Models;

public sealed class MigrationsRequest
{
    public MigrationsAction Action { get; set; }
    public DatabaseProvider Provider { get; set; }
    public MigrationsEntity Settings { get; set; } = new();
    public string? MigrationName { get; set; }
    public bool DryRun { get; set; }
    public string? WorkingDirectory { get; set; }
}
