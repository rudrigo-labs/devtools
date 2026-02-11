namespace DevTools.Core.Utilities;

public sealed class PathFilter
{
    private readonly IReadOnlyList<GlobMatcher> _includes;
    private readonly IReadOnlyList<GlobMatcher> _excludes;

    public PathFilter(IReadOnlyList<string>? includeGlobs, IReadOnlyList<string>? excludeGlobs)
    {
        _includes = (includeGlobs ?? Array.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => new GlobMatcher(s.Trim()))
            .ToList();

        _excludes = (excludeGlobs ?? Array.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => new GlobMatcher(s.Trim()))
            .ToList();
    }

    public bool IsExcluded(string relativePath)
    {
        if (_excludes.Count == 0)
            return false;

        return _excludes.Any(m => m.IsMatch(relativePath));
    }

    public bool IsIncluded(string relativePath)
    {
        if (_includes.Count == 0)
            return true;

        return _includes.Any(m => m.IsMatch(relativePath));
    }
}
