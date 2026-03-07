using DevTools.Rename.Engine;
using DevTools.Rename.Models;
using Xunit;

namespace DevTools.Tests;

public class RoslynRenamerTests
{
    [Fact]
    public void Rename_ReplacesIdentifiers_AndKeepsStringLiterals()
    {
        const string source = @"
namespace MyApp;

public class Old
{
    public void Run()
    {
        var value = ""Old"";
        var instance = new Old();
    }
}
";

        var result = RoslynRenamer.Rename(source, "Old", "New", RenameMode.General);

        Assert.Contains("class New", result);
        Assert.Contains("new New()", result);
        Assert.Contains("\"Old\"", result);
    }
}
