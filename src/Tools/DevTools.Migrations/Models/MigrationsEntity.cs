using DevTools.Core.Models;

namespace DevTools.Migrations.Models;

/// <summary>
/// Configuração nomeada do Migrations.
/// Herda NamedConfiguration — persistida via ToolConfigurationEntity.
/// Targets é serializado como JSON no payload.
/// </summary>
public sealed class MigrationsEntity : NamedConfiguration
{
    public string RootPath { get; set; } = string.Empty;
    public string StartupProjectPath { get; set; } = string.Empty;
    public string DbContextFullName { get; set; } = string.Empty;
    public string? AdditionalArgs { get; set; }
    public List<MigrationTarget> Targets { get; set; } = new();
}
