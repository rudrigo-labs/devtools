namespace DevTools.Migrations.Models;

public sealed record MigrationsRequest(
    MigrationsAction Action,
    DatabaseProvider Provider,
    MigrationsSettings Settings,
    string? MigrationName = null,
    bool DryRun = false,
    string? WorkingDirectory = null);
