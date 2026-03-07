using DevTools.Notes.Engine;
using DevTools.Notes.Models;
using System.Text.Json;

namespace DevTools.Tests;

public class NotesEngineIntegrationTests
{
    [Fact]
    public async Task CreateListAndLoad_WithMarkdown_PreservesMdExtensionAndHeader()
    {
        using var scope = new TempNotesScope();
        var engine = new NotesEngine();

        var create = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.CreateItem,
            NotesRootPath: scope.RootPath,
            Title: "Minha Nota",
            Content: "conteudo markdown",
            UseMarkdown: true,
            CreateDateFolder: false));

        Assert.True(create.IsSuccess);
        var created = create.Value?.CreateResult;
        Assert.NotNull(created);
        Assert.EndsWith(".md", created!.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(created.Path));

        var list = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.ListItems,
            NotesRootPath: scope.RootPath));

        Assert.True(list.IsSuccess);
        var item = Assert.Single(list.Value!.ListResult!.Items);
        Assert.EndsWith(".md", item.FileName, StringComparison.OrdinalIgnoreCase);
        var content = File.ReadAllText(created.Path);
        Assert.StartsWith("# Minha Nota", content, StringComparison.Ordinal);
        Assert.Contains("conteudo markdown", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateItem_WithPlainText_UsesTxtExtension()
    {
        using var scope = new TempNotesScope();
        var engine = new NotesEngine();

        var create = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.CreateItem,
            NotesRootPath: scope.RootPath,
            Title: "Nota Texto",
            Content: "conteudo texto",
            UseMarkdown: false,
            CreateDateFolder: false));

        Assert.True(create.IsSuccess);
        var created = create.Value?.CreateResult;
        Assert.NotNull(created);
        Assert.EndsWith(".txt", created!.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(created.Path));
        var content = File.ReadAllText(created.Path);
        Assert.StartsWith("Nota Texto", content, StringComparison.Ordinal);
        Assert.Contains("conteudo texto", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAndLoad_ExistingSimpleNote_UpdatesFileAndIndex()
    {
        using var scope = new TempNotesScope();
        var engine = new NotesEngine();

        var create = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.CreateItem,
            NotesRootPath: scope.RootPath,
            Title: "Nota Editavel",
            Content: "versao-1",
            UseMarkdown: true,
            CreateDateFolder: false));

        Assert.True(create.IsSuccess);
        var created = create.Value?.CreateResult;
        Assert.NotNull(created);

        var key = created!.FileName;
        var save = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.SaveNote,
            NotesRootPath: scope.RootPath,
            NoteKey: key,
            Content: "versao-2"));

        Assert.True(save.IsSuccess);
        var write = save.Value?.WriteResult;
        Assert.NotNull(write);
        Assert.Equal(key, write!.Key);

        var load = await engine.ExecuteAsync(new NotesRequest(
            Action: NotesAction.LoadNote,
            NotesRootPath: scope.RootPath,
            NoteKey: key));

        Assert.True(load.IsSuccess);
        Assert.Equal("versao-2", load.Value?.ReadResult?.Content);

        var indexPath = Path.Combine(scope.RootPath, "index.json");
        Assert.True(File.Exists(indexPath));

        var indexJson = File.ReadAllText(indexPath);
        var index = JsonSerializer.Deserialize<NotesIndex>(
            indexJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(index);

        var entry = index!.Items.Single(x => string.Equals(x.FileName, key, StringComparison.OrdinalIgnoreCase));
        Assert.False(string.IsNullOrWhiteSpace(entry.Sha256));
    }

    private sealed class TempNotesScope : IDisposable
    {
        public TempNotesScope()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "devtools-notes-tests-" + Guid.NewGuid().ToString("N"));
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
