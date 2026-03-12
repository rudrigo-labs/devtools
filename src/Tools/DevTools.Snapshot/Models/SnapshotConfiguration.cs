using DevTools.Core.Models;

namespace DevTools.Snapshot.Models;

/// <summary>
/// Configuração nomeada do Snapshot. Conceito: Configuration = Request + Metadata.
/// </summary>
public sealed class SnapshotConfiguration : NamedConfiguration
{
    public string RootPath { get; set; } = string.Empty;
    public string OutputBasePath { get; set; } = string.Empty;
    public bool GenerateText { get; set; } = true;
    public bool GenerateJsonNested { get; set; }
    public bool GenerateJsonRecursive { get; set; }
    public bool GenerateHtmlPreview { get; set; }
    public IReadOnlyList<string> IgnoredDirectories { get; set; } = SnapshotDefaults.DefaultIgnoredDirectories;
    public IReadOnlyList<string> IgnoredExtensions { get; set; } = SnapshotDefaults.DefaultIgnoredExtensions;
    public int? MaxFileSizeKb { get; set; } = null;

    /// <summary>Converte esta configuração em um Request pronto para execução.</summary>
    public SnapshotRequest ToRequest() => new()
    {
        RootPath = RootPath,
        OutputBasePath = OutputBasePath,
        GenerateText = GenerateText,
        GenerateJsonNested = GenerateJsonNested,
        GenerateJsonRecursive = GenerateJsonRecursive,
        GenerateHtmlPreview = GenerateHtmlPreview,
        IgnoredDirectories = IgnoredDirectories,
        IgnoredExtensions = IgnoredExtensions,
        MaxFileSizeKb = MaxFileSizeKb
    };
}
