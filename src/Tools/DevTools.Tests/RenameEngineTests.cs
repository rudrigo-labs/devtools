using DevTools.Rename.Engine;
using DevTools.Rename.Models;

namespace DevTools.Tests;

public class RenameEngineTests
{
    [Fact]
    public async Task ExecuteAsync_DryRunRenamesIdentifiersAndFiles()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "DevTools_RenameTests_" + Guid.NewGuid());
        Directory.CreateDirectory(tempRoot);

        var srcDir = Path.Combine(tempRoot, "src");
        Directory.CreateDirectory(srcDir);

        var filePath = Path.Combine(srcDir, "OldService.cs");
        var source = """
namespace MyApp;

public class OldService
{
    public void Run()
    {
        var value = "OldService";
        var instance = new OldService();
    }
}
""";
        await File.WriteAllTextAsync(filePath, source);

        try
        {
            var engine = new RenameEngine();

            var request = new RenameRequest(
                RootPath: tempRoot,
                OldText: "OldService",
                NewText: "NewService",
                Mode: RenameMode.General,
                DryRun: true,
                IncludeGlobs: new[] { "**/*.cs" },
                ExcludeGlobs: null,
                BackupEnabled: false,
                WriteUndoLog: false,
                UndoLogPath: null,
                ReportPath: null,
                MaxDiffLinesPerFile: 200
            );

            var result = await engine.ExecuteAsync(request, progress: null, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Summary.FilesUpdated > 0);
            Assert.True(result.Value.Summary.FilesRenamed > 0);

            Assert.True(File.Exists(filePath));
            Assert.False(File.Exists(Path.Combine(srcDir, "NewService.cs")));
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
    public async Task ExecuteAsync_WritesUndoLogWhenEnabled()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "DevTools_RenameTests_" + Guid.NewGuid());
        Directory.CreateDirectory(tempRoot);

        var srcDir = Path.Combine(tempRoot, "src");
        Directory.CreateDirectory(srcDir);

        var filePath = Path.Combine(srcDir, "OldService.cs");
        var source = """
namespace MyApp;

public class OldService
{
    public void Run()
    {
        var value = "OldService";
        var instance = new OldService();
    }
}
""";
        await File.WriteAllTextAsync(filePath, source);

        var undoLogPath = Path.Combine(tempRoot, "rename-undo-test.json");

        try
        {
            var engine = new RenameEngine();

            var request = new RenameRequest(
                RootPath: tempRoot,
                OldText: "OldService",
                NewText: "NewService",
                Mode: RenameMode.General,
                DryRun: false,
                IncludeGlobs: new[] { "**/*.cs" },
                ExcludeGlobs: null,
                BackupEnabled: false,
                WriteUndoLog: true,
                UndoLogPath: undoLogPath,
                ReportPath: null,
                MaxDiffLinesPerFile: 200
            );

            var result = await engine.ExecuteAsync(request, progress: null, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.False(string.IsNullOrWhiteSpace(result.Value.UndoLogPath));
            Assert.True(File.Exists(result.Value.UndoLogPath));
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
        var tempRoot = Path.Combine(Path.GetTempPath(), "DevTools_RenameTests_" + Guid.NewGuid());
        Directory.CreateDirectory(tempRoot);

        var missingRoot = Path.Combine(tempRoot, "missing");

        try
        {
            var engine = new RenameEngine();

            var request = new RenameRequest(
                RootPath: missingRoot,
                OldText: "Old",
                NewText: "New",
                Mode: RenameMode.General,
                DryRun: true,
                IncludeGlobs: null,
                ExcludeGlobs: null,
                BackupEnabled: false,
                WriteUndoLog: false,
                UndoLogPath: null,
                ReportPath: null,
                MaxDiffLinesPerFile: 200
            );

            var result = await engine.ExecuteAsync(request, progress: null, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, e => e.Code == "rename.root.not_found");
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
