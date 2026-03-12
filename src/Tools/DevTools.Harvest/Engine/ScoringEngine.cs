using DevTools.Harvest.Models;

namespace DevTools.Harvest.Engine;

internal sealed class ScoringEngine
{
    public HarvestHit Score(
        FileNode file,
        DependencyGraphBuilder.DependencyGraph graph,
        HarvestRequest request,
        IReadOnlyList<KeywordDensity> densities)
    {
        var tags = new List<string>();
        var reasons = new List<string>();
        var score = 0.0;

        var fanIn = graph.GetFanIn(file.FullPath);
        if (fanIn > 0)
        {
            var s = fanIn * request.FanInWeight;
            score += s;
            tags.Add("fan-in");
            reasons.Add($"Fan-In: {fanIn} (+{s:0.##})");
        }

        var fanOut = graph.GetFanOut(file.FullPath);
        if (fanOut > 0)
        {
            var s = fanOut * request.FanOutWeight;
            score -= s;
            tags.Add("fan-out");
            reasons.Add($"Fan-Out: {fanOut} (-{s:0.##})");
        }

        foreach (var density in densities)
        {
            if (density.Hits <= 0) continue;

            var category = request.Categories.FirstOrDefault(c =>
                string.Equals(c.Name, density.Category, StringComparison.OrdinalIgnoreCase));

            var catWeight = category?.Weight ?? 1.0;
            var s = density.Density * request.KeywordDensityWeight * catWeight;

            if (s <= 0) continue;

            score += s;
            tags.Add(density.Category.ToLowerInvariant());
            reasons.Add($"KeywordDensity {density.Category}: {density.Hits} hits (+{s:0.##})");
        }

        if (file.PublicStaticMethodCount >= request.StaticMethodThreshold && request.StaticMethodBonus > 0)
        {
            score += request.StaticMethodBonus;
            tags.Add("static-methods");
            reasons.Add($"Métodos estáticos públicos: {file.PublicStaticMethodCount} (+{request.StaticMethodBonus:0.##})");
        }

        if (request.LargeFileThresholdLines > 0
            && file.LineCount > request.LargeFileThresholdLines
            && request.LargeFilePenalty > 0)
        {
            score -= request.LargeFilePenalty;
            tags.Add("large-file");
            reasons.Add($"Arquivo grande: {file.LineCount} linhas (-{request.LargeFilePenalty:0.##})");
        }

        if (!file.IsEntrypointCandidate && fanIn == 0 && request.DeadCodePenalty > 0)
        {
            score -= request.DeadCodePenalty;
            tags.Add("dead-code-suspect");
            reasons.Add($"Sem referências detectadas (-{request.DeadCodePenalty:0.##})");
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
