namespace DevTools.Core.Models;

/// <summary>
/// Base para ferramentas que possuem configurações reutilizáveis nomeadas.
/// Conceito: Configuration = Request + Metadata.
/// </summary>
public abstract class NamedConfiguration
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
