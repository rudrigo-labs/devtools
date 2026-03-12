using DevTools.Core.Models;

namespace DevTools.Snapshot.Models;

/// <summary>
/// Configuração nomeada do Snapshot.
/// Herda FileToolOptions (que herda NamedConfiguration).
/// Adiciona apenas propriedades específicas do Snapshot.
/// </summary>
public sealed class SnapshotEntity : FileToolOptions
{
    public bool GenerateText { get; set; } = true;
    public bool GenerateJsonNested { get; set; }
    public bool GenerateJsonRecursive { get; set; }
    public bool GenerateHtmlPreview { get; set; }
    public string OutputBasePath { get; set; } = string.Empty;
}
