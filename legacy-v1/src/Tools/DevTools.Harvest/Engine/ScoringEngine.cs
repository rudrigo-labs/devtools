using DevTools.Harvest.Configuration;
using DevTools.Harvest.Models;

namespace DevTools.Harvest.Engine;

internal sealed class ScoringEngine
{
    public HarvestHit Score(
        FileNode file,
        DependencyGraphBuilder.DependencyGraph graph,
        HarvestConfig config,
        IReadOnlyList<KeywordDensity> densities)
    {
        var weights = config.Weights;
        var tags = new List<string>();
        var reasons = new List<string>();

        var score = 0.0;

        var fanIn = graph.GetFanIn(file.FullPath);
        if (fanIn > 0)
        {
            var s = fanIn * weights.FanInWeight;
            score += s;
            tags.Add("fan-in");
            reasons.Add($"Fan-In: {fanIn} (+{s:0.##})");
        }

        var fanOut = graph.GetFanOut(file.FullPath);
        if (fanOut > 0)
        {
            var s = fanOut * weights.FanOutWeight;
            score -= s;
            tags.Add("fan-out");
            reasons.Add($"Fan-Out: {fanOut} (-{s:0.##})");
        }

        foreach (var density in densities)
        {
            if (density.Hits <= 0)
                continue;

            var category = config.Categories.FirstOrDefault(c =>
                string.Equals(c.Name, density.Category, StringComparison.OrdinalIgnoreCase));

            var catWeight = category?.Weight ?? 1.0;
            var s = density.Density * weights.KeywordDensityWeight * catWeight;

            if (s <= 0)
                continue;

            score += s;
            tags.Add(density.Category.ToLowerInvariant());
            reasons.Add($"KeywordDensity {density.Category}: {density.Hits} hits (+{s:0.##})");
        }

        if (file.PublicStaticMethodCount >= weights.StaticMethodThreshold && weights.StaticMethodBonus > 0)
        {
            score += weights.StaticMethodBonus;
            tags.Add("static-methods");
            reasons.Add($"Public static methods: {file.PublicStaticMethodCount} (+{weights.StaticMethodBonus:0.##})");
        }

        if (weights.LargeFileThresholdLines > 0 && file.LineCount > weights.LargeFileThresholdLines && weights.LargeFilePenalty > 0)
        {
            score -= weights.LargeFilePenalty;
            tags.Add("large-file");
            reasons.Add($"Large file: {file.LineCount} lines (-{weights.LargeFilePenalty:0.##})");
        }

        if (!file.IsEntrypointCandidate && fanIn == 0 && weights.DeadCodePenalty > 0)
        {
            score -= weights.DeadCodePenalty;
            tags.Add("dead-code-suspect");
            reasons.Add($"No references detected (-{weights.DeadCodePenalty:0.##})");
        }

        return new HarvestHit(
            file.RelativePath,
            score,
            fanIn,
            fanOut,
            tags,
            reasons,
            densities);
    }
}
