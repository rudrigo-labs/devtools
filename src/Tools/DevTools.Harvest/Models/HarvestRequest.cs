using DevTools.Core.Models;

namespace DevTools.Harvest.Models;

/// <summary>
/// Request de execução do Harvest.
/// Herda FileToolOptions — RootPath, IgnoredDirectories, IgnoredExtensions,
/// IncludedExtensions e MaxFileSizeKb vêm da base.
/// O Host monta o request a partir do HarvestEntity selecionado.
/// </summary>
public sealed class HarvestRequest : FileToolOptions
{
    public string OutputPath { get; set; } = string.Empty;
    public bool CopyFiles { get; set; } = true;
    public int MinScore { get; set; } = HarvestDefaults.DefaultMinScore;

    public double FanInWeight { get; set; } = HarvestDefaults.DefaultFanInWeight;
    public double FanOutWeight { get; set; } = HarvestDefaults.DefaultFanOutWeight;
    public double KeywordDensityWeight { get; set; } = HarvestDefaults.DefaultKeywordDensityWeight;
    public int DensityScale { get; set; } = HarvestDefaults.DefaultDensityScale;
    public int StaticMethodThreshold { get; set; } = HarvestDefaults.DefaultStaticMethodThreshold;
    public double StaticMethodBonus { get; set; } = HarvestDefaults.DefaultStaticMethodBonus;
    public double DeadCodePenalty { get; set; } = HarvestDefaults.DefaultDeadCodePenalty;
    public int LargeFileThresholdLines { get; set; } = HarvestDefaults.DefaultLargeFileThresholdLines;
    public double LargeFilePenalty { get; set; } = HarvestDefaults.DefaultLargeFilePenalty;

    public List<HarvestKeywordCategory> Categories { get; set; } = HarvestDefaults.DefaultCategories();

    public HarvestRequest()
    {
        IgnoredDirectories = HarvestDefaults.DefaultIgnoredDirectories;
        IgnoredExtensions = HarvestDefaults.DefaultIgnoredExtensions;
        IncludedExtensions = HarvestDefaults.DefaultIncludedExtensions;
    }
}
