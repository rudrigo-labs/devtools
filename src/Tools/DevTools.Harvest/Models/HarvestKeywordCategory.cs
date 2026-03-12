namespace DevTools.Harvest.Models;

public sealed class HarvestKeywordCategory
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0;
    public List<string> Keywords { get; set; } = new();
}
