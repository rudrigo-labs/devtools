using DevTools.Migrations.Models;

namespace DevTools.Migrations.Engine;

public static class EfCommandBuilder
{
    public static string BuildAddMigration(MigrationsSettings s, DatabaseProvider provider, string migrationName)
    {
        var project = ResolveMigrationsProjectPath(s, provider);

        var cmd = $"ef migrations add {EscapeArg(migrationName)} " +
                  $"--project {Quote(project)} " +
                  $"--startup-project {Quote(s.StartupProjectPath)} " +
                  $"--context {EscapeArg(s.DbContextFullName)}";

        return AppendAdditionalArgs(cmd, s.AdditionalArgs);
    }

    public static string BuildUpdateDatabase(MigrationsSettings s, DatabaseProvider provider)
    {
        var project = ResolveMigrationsProjectPath(s, provider);

        var cmd = $"ef database update " +
                  $"--project {Quote(project)} " +
                  $"--startup-project {Quote(s.StartupProjectPath)} " +
                  $"--context {EscapeArg(s.DbContextFullName)}";

        return AppendAdditionalArgs(cmd, s.AdditionalArgs);
    }

    public static string ResolveMigrationsProjectPath(MigrationsSettings s, DatabaseProvider provider)
    {
        var target = s.Targets.FirstOrDefault(t => t.Provider == provider);
        if (target is null || string.IsNullOrWhiteSpace(target.MigrationsProjectPath))
            throw new InvalidOperationException($"Target not configured for provider {provider}.");

        return target.MigrationsProjectPath;
    }

    private static string AppendAdditionalArgs(string cmd, string? additionalArgs)
    {
        if (string.IsNullOrWhiteSpace(additionalArgs))
            return cmd;

        return cmd.TrimEnd() + " " + additionalArgs.Trim();
    }

    private static string Quote(string value)
        => $"\"{(value ?? string.Empty).Trim().Trim('"').Replace("\"", "\\\"")}\"";

    private static string EscapeArg(string value)
        => (value ?? string.Empty).Trim().Trim('"');
}
