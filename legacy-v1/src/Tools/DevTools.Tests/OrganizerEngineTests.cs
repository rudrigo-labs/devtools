using DevTools.Core.Providers;
using DevTools.Organizer.Engine;
using DevTools.Organizer.Models;
using DevTools.Core.Results;

namespace DevTools.Tests;

public class OrganizerEngineTests
{
    [Fact]
    public async Task ExecuteAsync_MovesFilesToMatchingCategory()
    {
        // arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), "DevTools_OrganizerTests_" + Guid.NewGuid());
        Directory.CreateDirectory(tempRoot);

        var inbox = Path.Combine(tempRoot, "Inbox");
        var output = Path.Combine(tempRoot, "Output");
        Directory.CreateDirectory(inbox);
        Directory.CreateDirectory(output);

        var filePath = Path.Combine(inbox, "curriculo_junior.pdf");
        await File.WriteAllTextAsync(filePath, "Meu curriculo junior");

        try
        {
            var fs = new SystemFileSystem();
            var engine = new OrganizerEngine(fs);

            var request = new OrganizerRequest(
                InboxPath: inbox,
                OutputPath: output,
                ConfigPath: null,
                MinScore: 0,
                Apply: true
            );

            var result = await engine.ExecuteAsync(request, progress: null, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.True(File.Exists(Path.Combine(output, "Curriculos", Path.GetFileName(filePath))));
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
    public async Task ExecuteAsync_KeepsNonMatchingFilesInInbox()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "DevTools_OrganizerTests_" + Guid.NewGuid());
        Directory.CreateDirectory(tempRoot);

        var inbox = Path.Combine(tempRoot, "Inbox");
        var output = Path.Combine(tempRoot, "Output");
        Directory.CreateDirectory(inbox);
        Directory.CreateDirectory(output);

        var ignoredFile = Path.Combine(inbox, "foto_vacaciones.jpg");
        await File.WriteAllTextAsync(ignoredFile, "imagem aleatória");

        try
        {
            var fs = new SystemFileSystem();
            var engine = new OrganizerEngine(fs);

            var request = new OrganizerRequest(
                InboxPath: inbox,
                OutputPath: output,
                ConfigPath: null,
                MinScore: 0,
                Apply: true
            );

            var result = await engine.ExecuteAsync(request, progress: null, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.True(File.Exists(ignoredFile));
            Assert.False(Directory.EnumerateFiles(output, "*.jpg", SearchOption.AllDirectories).Any());
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
    public async Task ExecuteAsync_ReturnsErrorWhenInboxDoesNotExist()
    {
        // arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), "DevTools_OrganizerTests_" + Guid.NewGuid());
        Directory.CreateDirectory(tempRoot);

        var inbox = Path.Combine(tempRoot, "InboxMissing");
        var output = Path.Combine(tempRoot, "Output");
        Directory.CreateDirectory(output);

        try
        {
            var fs = new SystemFileSystem();
            var engine = new OrganizerEngine(fs);

            var request = new OrganizerRequest(
                InboxPath: inbox,
                OutputPath: output,
                ConfigPath: null,
                MinScore: 0,
                Apply: true
            );

            var result = await engine.ExecuteAsync(request, progress: null, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, e => e.Code == "organizer.inbox.not_found");
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
