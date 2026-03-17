using DevTools.Core.Models;
using DevTools.Snapshot.Engine;
using DevTools.Snapshot.Models;

namespace DevTools.IntegrationTests.Validators;

public sealed class SnapshotEngineValidatorIntegrationTests
{
    [Fact]
    public async Task ExecuteAsyncComOutputBasePathVazioRetornaFalhaDeValidacao()
    {
        var engine = new SnapshotEngine();
        var request = new SnapshotRequest
        {
            RootPath = "C:\\repo",
            OutputBasePath = string.Empty,
            GenerateText = true
        };

        var result = await engine.ExecuteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Cause == "outputBasePath");
    }

    [Fact]
    public async Task ExecuteAsyncComMaxFileSizeAcimaDoLimiteRetornaFalhaDeValidacao()
    {
        var settings = new AppSettings
        {
            FileTools = new FileToolsSettings
            {
                MaxFileSizeKb = 500,
                AbsoluteMaxFileSizeKb = 1000
            }
        };

        var engine = new SnapshotEngine(settings);
        var request = new SnapshotRequest
        {
            RootPath = "C:\\repo",
            OutputBasePath = "C:\\out",
            GenerateText = true,
            MaxFileSizeKb = 1001
        };

        var result = await engine.ExecuteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Cause == "maxFileSizeKb");
    }
}
