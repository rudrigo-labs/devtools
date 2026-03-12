using DevTools.Core.Models;

namespace DevTools.Harvest.Models;

/// <summary>
/// Configuração nomeada do Harvest.
/// Herda FileToolOptions (que herda NamedConfiguration).
/// Contém tudo que a ferramenta precisa para executar + metadata de persistência.
/// </summary>
public sealed class HarvestEntity : FileToolOptions
{
    // --- Execução ---
    public string OutputPath { get; set; } = string.Empty;
    public bool CopyFiles { get; set; } = true;
    public int MinScore { get; set; } = HarvestDefaults.DefaultMinScore;

    // --- Pesos de scoring ---
    public double FanInWeight { get; set; } = HarvestDefaults.DefaultFanInWeight;
    public double FanOutWeight { get; set; } = HarvestDefaults.DefaultFanOutWeight;
    public double KeywordDensityWeight { get; set; } = HarvestDefaults.DefaultKeywordDensityWeight;
    public int DensityScale { get; set; } = HarvestDefaults.DefaultDensityScale;
    public int StaticMethodThreshold { get; set; } = HarvestDefaults.DefaultStaticMethodThreshold;
    public double StaticMethodBonus { get; set; } = HarvestDefaults.DefaultStaticMethodBonus;
    public double DeadCodePenalty { get; set; } = HarvestDefaults.DefaultDeadCodePenalty;
    public int LargeFileThresholdLines { get; set; } = HarvestDefaults.DefaultLargeFileThresholdLines;
    public double LargeFilePenalty { get; set; } = HarvestDefaults.DefaultLargeFilePenalty;

    // --- Categorias de keywords (serializado como JSON no banco) ---
    public List<HarvestKeywordCategory> Categories { get; set; } = HarvestDefaults.DefaultCategories();
}
