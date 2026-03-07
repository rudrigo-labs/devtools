namespace DevTools.Migrations.Models;

public sealed record MigrationsResponse(
    MigrationsAction Action,
    DatabaseProvider Provider,
    string Command,
    int? ExitCode,
    string? StdOut,
    string? StdErr,
    bool WasDryRun);
