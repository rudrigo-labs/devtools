using DevTools.Notes.Engine;
using DevTools.Notes.Models;

namespace DevTools.Tests;

public class NotesBackupVolumeIntegrationTests
{
    [Fact]
    public async Task ExportImport_WithManyNotes_RestoresAllItems()
    {
        using var source = new TempNotesScope();
        using var target = new TempNotesScope();
        var engine = new NotesEngine();

        const int totalNotes = 40;
        var fixedDate = new DateTime(2026, 3, 4);

        for (var i = 1; i <= totalNotes; i++)
        {
            var create = await engine.ExecuteAsync(new NotesRequest(
                Action: NotesAction.CreateItem,
                NotesRootPath: source.RootPath,
                Title: $"Nota {i:000}",
                Content: $"conteudo-{i:000}",
                LocalDate: fixedDate,
                UseMarkdown: i % 2 == 0,
                CreateDateFolder: false));

            Assert.True(create.IsSuccess);
        }

        var exportDir = Path.Combine(source.RootPath, "exports-target");
        Directory.CreateDirectory(exportDir);

        var export = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.ExportZip,
            NotesRootPath: source.RootPath,
            OutputPath: exportDir));

        Assert.True(export.IsSuccess);
        var zipPath = export.Value?.ExportedZipPath;
        Assert.False(string.IsNullOrWhiteSpace(zipPath));
        Assert.True(File.Exists(zipPath!));

        var import = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.ImportZip,
            NotesRootPath: target.RootPath,
            ZipPath: zipPath));

        Assert.True(import.IsSuccess);
        var report = import.Value?.BackupReport;
        Assert.NotNull(report);
        Assert.Equal(totalNotes, report!.ImportedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Equal(0, report.ConflictCount);

        var list = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.ListItems,
            NotesRootPath: target.RootPath));

        Assert.True(list.IsSuccess);
        Assert.Equal(totalNotes, list.Value!.ListResult!.Items.Count);
    }

    [Fact]
    public async Task ImportZip_WithDifferentContentForSameFile_CreatesConflictFile()
    {
        using var source = new TempNotesScope();
        using var target = new TempNotesScope();
        var engine = new NotesEngine();
        var fixedDate = new DateTime(2026, 3, 4);

        var original = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.CreateItem,
            NotesRootPath: source.RootPath,
            Title: "Mesma Nota",
            Content: "conteudo-original",
            LocalDate: fixedDate,
            UseMarkdown: true,
            CreateDateFolder: false));
        Assert.True(original.IsSuccess);

        var targetCreate = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.CreateItem,
            NotesRootPath: target.RootPath,
            Title: "Mesma Nota",
            Content: "conteudo-local-diferente",
            LocalDate: fixedDate,
            UseMarkdown: true,
            CreateDateFolder: false));
        Assert.True(targetCreate.IsSuccess);

        var exportDir = Path.Combine(source.RootPath, "exports-target");
        Directory.CreateDirectory(exportDir);
        var export = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.ExportZip,
            NotesRootPath: source.RootPath,
            OutputPath: exportDir));
        Assert.True(export.IsSuccess);
        var zipPath = export.Value?.ExportedZipPath;
        Assert.False(string.IsNullOrWhiteSpace(zipPath));

        var import = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.ImportZip,
            NotesRootPath: target.RootPath,
            ZipPath: zipPath));

        Assert.True(import.IsSuccess);
        var report = import.Value?.BackupReport;
        Assert.NotNull(report);
        Assert.Equal(0, report!.ImportedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Equal(1, report.ConflictCount);
        Assert.Single(report.ConflictFiles);

        var itemsDir = Path.Combine(target.RootPath, "items");
        var conflictFiles = Directory.GetFiles(itemsDir, "*conflict*.*", SearchOption.AllDirectories);
        Assert.NotEmpty(conflictFiles);
    }

    private sealed class TempNotesScope : IDisposable
    {
        public TempNotesScope()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "devtools-notes-backup-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootPath))
                {
                    Directory.Delete(RootPath, true);
                }
            }
            catch
            {
                // ignore temporary cleanup failures
            }
        }
    }
}

