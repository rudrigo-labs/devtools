using DevTools.Core.Utilities;
using Xunit;

namespace DevTools.Tests;

public class PathFilterTests
{
    [Fact]
    public void Includes_WhenNoIncludeGlobsProvided()
    {
        var filter = new PathFilter(null, null);

        Assert.True(filter.IsIncluded("src/App/Program.cs"));
    }

    [Fact]
    public void Excludes_WhenExcludeGlobMatches()
    {
        var filter = new PathFilter(new[] { "src/**/*.cs" }, new[] { "src/**/bin/**" });

        Assert.True(filter.IsExcluded("src/App/bin/Debug/Program.cs"));
        Assert.True(filter.IsIncluded("src/App/Program.cs"));
    }

    [Fact]
    public void ReturnsFalseWhenIncludeGlobsDoNotMatch()
    {
        var filter = new PathFilter(new[] { "src/**/*.cs" }, Array.Empty<string>());

        Assert.False(filter.IsIncluded("docs/readme.md"));
    }
}
