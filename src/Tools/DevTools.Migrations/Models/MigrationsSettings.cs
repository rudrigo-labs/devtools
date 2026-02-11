namespace DevTools.Migrations.Models;

public sealed class MigrationsSettings
{
    public string RootPath { get; set; } = string.Empty;
    public string StartupProjectPath { get; set; } = string.Empty;
    public string DbContextFullName { get; set; } = string.Empty;

    public List<MigrationTarget> Targets { get; set; } = new();

    public string? AdditionalArgs { get; set; }
}
