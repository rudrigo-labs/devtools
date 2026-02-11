namespace DevTools.Harvest.Configuration;

public sealed class HarvestConfig
{
    public HarvestRules Rules { get; set; } = new();
    public HarvestWeights Weights { get; set; } = new();
    public List<HarvestKeywordCategory> Categories { get; set; } = new();

    public int MinScoreDefault { get; set; } = 0;
    public int TopDefault { get; set; } = 100;

    public void Normalize()
    {
        Rules ??= new HarvestRules();
        Weights ??= new HarvestWeights();
        Categories ??= new List<HarvestKeywordCategory>();
        Rules.Normalize();
        Weights.Normalize();

        foreach (var category in Categories)
            category.Normalize();
    }
}

public sealed class HarvestRules
{
    public List<string> Extensions { get; set; } = new();
    public List<string> ExcludeDirectories { get; set; } = new();
    public List<string> IgnoreUsingPrefixes { get; set; } = new();

    public int? MaxFileSizeKb { get; set; }

    public void Normalize()
    {
        Extensions ??= new List<string>();
        ExcludeDirectories ??= new List<string>();
        IgnoreUsingPrefixes ??= new List<string>();
    }
}

public sealed class HarvestWeights
{
    public double FanInWeight { get; set; } = 2.0;
    public double FanOutWeight { get; set; } = 0.5;
    public double KeywordDensityWeight { get; set; } = 1.0;

    public int DensityScale { get; set; } = 100;

    public int StaticMethodThreshold { get; set; } = 3;
    public double StaticMethodBonus { get; set; } = 10.0;

    public double DeadCodePenalty { get; set; } = 5.0;
    public int LargeFileThresholdLines { get; set; } = 0;
    public double LargeFilePenalty { get; set; } = 0;

    public void Normalize()
    {
        if (DensityScale <= 0) DensityScale = 100;
        if (FanInWeight < 0) FanInWeight = 0;
        if (FanOutWeight < 0) FanOutWeight = 0;
        if (KeywordDensityWeight < 0) KeywordDensityWeight = 0;
        if (StaticMethodThreshold < 0) StaticMethodThreshold = 0;
        if (StaticMethodBonus < 0) StaticMethodBonus = 0;
        if (DeadCodePenalty < 0) DeadCodePenalty = 0;
        if (LargeFileThresholdLines < 0) LargeFileThresholdLines = 0;
        if (LargeFilePenalty < 0) LargeFilePenalty = 0;
    }
}

public sealed class HarvestKeywordCategory
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0;
    public List<string> Keywords { get; set; } = new();

    public void Normalize()
    {
        Name ??= string.Empty;
        Keywords ??= new List<string>();
    }
}
