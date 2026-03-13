namespace DevTools.Image.Models;

/// <summary>
/// Request de execução do ImageSplit.
/// Não herda FileToolOptions pois opera sobre um arquivo de imagem único,
/// não sobre uma árvore de diretórios.
/// </summary>
public sealed class ImageSplitRequest
{
    public string InputPath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public string OutputBaseName { get; set; } = string.Empty;
    public string OutputExtension { get; set; } = string.Empty;
    public byte AlphaThreshold { get; set; } = 10;
    public int StartIndex { get; set; } = 1;
    public bool Overwrite { get; set; } = false;
    public int MinRegionWidth { get; set; } = 3;
    public int MinRegionHeight { get; set; } = 3;
}
