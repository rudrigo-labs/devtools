namespace DevTools.Core.Models;

/// <summary>
/// Base para ferramentas que varrem diretórios.
/// Herda NamedConfiguration pois toda configuração de ferramenta é nomeada.
/// Utilizada por Snapshot, Harvest, Utf8Convert, SearchText e similares.
/// </summary>
public abstract class FileToolOptions : NamedConfiguration
{
    public string RootPath { get; set; } = string.Empty;
    public IReadOnlyList<string> IgnoredDirectories { get; set; } = [];
    public IReadOnlyList<string> IgnoredExtensions { get; set; } = [];
    public IReadOnlyList<string> IncludedExtensions { get; set; } = [];

    /// <summary>
    /// Tamanho máximo de arquivo a ser lido, em KB.
    /// O teto absoluto é definido em AppSettings.FileTools.AbsoluteMaxFileSizeKb.
    /// </summary>
    public int? MaxFileSizeKb { get; set; }
}
