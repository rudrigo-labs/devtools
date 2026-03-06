using DevTools.Notes.Engine;
using DevTools.Notes.Models;
using DevTools.Notes.Providers;

namespace DevTools.Tests;

public class NotesDeleteFlowIntegrationTests
{
    [Fact]
    public async Task DeleteFlow_RemovesFileAndIndexEntry()
    {
        using var scope = new TempNotesScope();
        var engine = new NotesEngine();

        var create = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.CreateItem,
            NotesRootPath: scope.RootPath,
            Title: "Nota Excluir",
            Content: "conteudo",
            UseMarkdown: false,
            CreateDateFolder: false));

        Assert.True(create.IsSuccess);
        var created = create.Value?.CreateResult;
        Assert.NotNull(created);
        Assert.True(File.Exists(created!.Path));

        await DeleteNoteByKeyAsync(scope.RootPath, created.FileName);

        var list = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.ListItems,
            NotesRootPath: scope.RootPath));

        Assert.True(list.IsSuccess);
        Assert.Empty(list.Value!.ListResult!.Items);
        Assert.False(File.Exists(created.Path));
    }

    private static async Task DeleteNoteByKeyAsync(string storagePath, string noteKey)
    {
        string root = NotesPaths.ResolveRoot(storagePath);
        string itemsRoot = NotesPaths.ItemsDir(root);

        string normalized = noteKey
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        string fullPath = Path.GetFullPath(Path.Combine(itemsRoot, normalized));
        string rootWithSeparator = itemsRoot.EndsWith(Path.DirectorySeparatorChar)
            ? itemsRoot
            : itemsRoot + Path.DirectorySeparatorChar;

        if (fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        var indexStore = new NotesIndexStore();
        var load = await indexStore.LoadAsync(root);
        if (load.IsSuccess && load.Value != null)
        {
            load.Value.Items.RemoveAll(x =>
                string.Equals(x.FileName, noteKey, StringComparison.OrdinalIgnoreCase));
            await indexStore.SaveAsync(root, load.Value);
        }
    }

    private sealed class TempNotesScope : IDisposable
    {
        public TempNotesScope()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "devtools-notes-delete-tests-" + Guid.NewGuid().ToString("N"));
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
