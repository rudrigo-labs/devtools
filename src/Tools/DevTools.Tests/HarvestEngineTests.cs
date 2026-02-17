using DevTools.Core.Providers;
using DevTools.Harvest.Engine;
using DevTools.Harvest.Models;

namespace DevTools.Tests;

public class HarvestEngineTests
{
    [Fact]
    public async Task ExecuteAsync_FindsAndCopiesMatchingFiles()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "DevTools_HarvestTests_" + Guid.NewGuid());
        Directory.CreateDirectory(tempRoot);

        var source = Path.Combine(tempRoot, "src");
        var output = Path.Combine(tempRoot, "out");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(output);

        var filePath = Path.Combine(source, "SecurityHelper.cs");
        await File.WriteAllTextAsync(filePath, "public static class SecurityHelper { void Encrypt() {} }");

        try
        {
            var fs = new SystemFileSystem();
            var engine = new HarvestEngine(fs);

            var request = new HarvestRequest(
                RootPath: source,
                OutputPath: output,
                ConfigPath: null,
                MinScore: 0,
                CopyFiles: true
            );

            var result = await engine.ExecuteAsync(request, progress: null, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Report.Hits.Count > 0);
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsErrorWhenRootDoesNotExist()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "DevTools_HarvestTests_" + Guid.NewGuid());
        Directory.CreateDirectory(tempRoot);

        var source = Path.Combine(tempRoot, "missing");
        var output = Path.Combine(tempRoot, "out");
        Directory.CreateDirectory(output);

        try
        {
            var fs = new SystemFileSystem();
            var engine = new HarvestEngine(fs);

            var request = new HarvestRequest(
                RootPath: source,
                OutputPath: output,
                ConfigPath: null,
                MinScore: 0,
                CopyFiles: true
            );

            var result = await engine.ExecuteAsync(request, progress: null, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, e => e.Code == "harvest.root.not_found");
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
            }
        }
    }
}
