using DevTools.Core.Models;
using DevTools.Core.Configuration;
using DevTools.Harvest.Configuration;
using DevTools.Presentation.Wpf.Models;
using DevTools.Infrastructure.Persistence;
using DevTools.Infrastructure.Persistence.Stores;
using DevTools.Notes.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Tests;

public class SqliteStoresIntegrationTests
{
    [Fact]
    public void SqliteSettingsStore_SaveAndReadSection_RoundTripsData()
    {
        using var scope = new SqliteScope();
        var store = new SqliteSettingsStore(scope.Options, scope.DatabasePath);

        var input = new GoogleDriveSettings
        {
            IsEnabled = true,
            ClientId = "client-123",
            ClientSecret = "secret-123",
            ProjectId = "project-123",
            FolderName = "DevToolsNotes"
        };

        store.SaveSection("GoogleDrive", input);
        var output = store.GetSection<GoogleDriveSettings>("GoogleDrive");

        Assert.True(store.IsConfigured());
        Assert.True(output.IsEnabled);
        Assert.Equal("client-123", output.ClientId);
        Assert.Equal("secret-123", output.ClientSecret);
        Assert.Equal("project-123", output.ProjectId);
        Assert.Equal("DevToolsNotes", output.FolderName);
    }

    [Fact]
    public void SqliteSettingsStore_CreateDefaultIfNotExists_IsIdempotent()
    {
        using var scope = new SqliteScope();
        var store = new SqliteSettingsStore(scope.Options, scope.DatabasePath);

        store.CreateDefaultIfNotExists();
        var firstCount = CountAppSettings(scope.Options);

        var harvest = store.GetSection<HarvestConfig>("Harvest");

        Assert.NotNull(harvest.Rules);
        Assert.Contains(".cs", harvest.Rules.Extensions);
        Assert.True(firstCount >= 3);

        store.CreateDefaultIfNotExists();
        var secondCount = CountAppSettings(scope.Options);
        Assert.Equal(firstCount, secondCount);
    }

    [Fact]
    public void SqliteToolConfigurationStore_SaveAndLoad_RespectsOrderingAndReplacement()
    {
        using var scope = new SqliteScope();
        var store = new SqliteToolConfigurationStore(scope.Options);

        store.SaveConfigurations("Rename", new List<ToolConfiguration>
        {
            new()
            {
                Name = "Zulu",
                IsDefault = false,
                Options = new Dictionary<string, string> { ["root"] = "c:/tmp/z" }
            },
            new()
            {
                Name = "Alpha",
                IsDefault = true,
                Options = new Dictionary<string, string> { ["root"] = "c:/tmp/a" }
            }
        });

        var firstLoad = store.LoadConfigurations("Rename");
        Assert.Equal(2, firstLoad.Count);
        Assert.Equal("Alpha", firstLoad[0].Name);
        Assert.True(firstLoad[0].IsDefault);
        Assert.Equal("c:/tmp/a", firstLoad[0].Options["root"]);

        store.SaveConfigurations("Rename", new List<ToolConfiguration>
        {
            new()
            {
                Name = "Only",
                IsDefault = true,
                Options = new Dictionary<string, string> { ["root"] = "c:/tmp/only" }
            }
        });

        var secondLoad = store.LoadConfigurations("Rename");
        Assert.Single(secondLoad);
        Assert.Equal("Only", secondLoad[0].Name);
    }

    [Fact]
    public void ToolConfigurationManager_WithRepository_UsesInfrastructureStore()
    {
        using var scope = new SqliteScope();
        var store = new SqliteToolConfigurationStore(scope.Options);
        var repository = new ToolConfigurationRepository(store);
        var manager = new ToolConfigurationManager(repository);

        manager.SaveConfiguration("Snapshot", new ToolConfiguration
        {
            Name = "ProjetoPrincipal",
            IsDefault = true,
            Options = new Dictionary<string, string>
            {
                ["project-path"] = "c:/repo/app",
                ["generate-html"] = "true"
            }
        });

        var loaded = manager.LoadConfigurations("Snapshot");
        Assert.Single(loaded);
        Assert.Equal("ProjetoPrincipal", loaded[0].Name);
        Assert.Equal("Snapshot", loaded[0].ToolSlug);
        Assert.True(loaded[0].IsDefault);
        Assert.Equal("c:/repo/app", loaded[0].Options["project-path"]);
        Assert.Equal("true", loaded[0].Options["generate-html"]);
    }

    [Fact]
    public void SqliteNoteMetadataStore_UpsertGetAndDelete_WorkAsExpected()
    {
        using var scope = new SqliteScope();
        var store = new SqliteNoteMetadataStore(scope.Options);
        var key = "2026-03-04/note.md";
        var now = DateTime.UtcNow;

        store.Upsert(new NoteMetadataEntity
        {
            NoteKey = key,
            Title = "Minha Nota",
            Extension = ".md",
            LastLocalWriteUtc = now,
            LastCloudStatus = "Pending",
            Hash = "hash-v1"
        });

        var first = store.GetByKey(key);
        Assert.NotNull(first);
        Assert.Equal("Minha Nota", first!.Title);
        Assert.Equal("Pending", first.LastCloudStatus);
        Assert.Equal("hash-v1", first.Hash);

        store.Upsert(new NoteMetadataEntity
        {
            NoteKey = key,
            Title = "Minha Nota",
            Extension = ".md",
            LastLocalWriteUtc = now.AddMinutes(1),
            LastCloudSyncUtc = now.AddMinutes(2),
            LastCloudStatus = "Success",
            Hash = "hash-v2"
        });

        var second = store.GetByKey(key);
        Assert.NotNull(second);
        Assert.Equal("Success", second!.LastCloudStatus);
        Assert.Equal("hash-v2", second.Hash);
        Assert.NotNull(second.LastCloudSyncUtc);

        store.Delete(key);
        var removed = store.GetByKey(key);
        Assert.Null(removed);
    }

    private static int CountAppSettings(DbContextOptions<DevToolsDbContext> options)
    {
        using var db = new DevToolsDbContext(options);
        return db.AppSettings.Count();
    }

    private sealed class SqliteScope : IDisposable
    {
        private readonly string _root;

        public SqliteScope()
        {
            _root = Path.Combine(Path.GetTempPath(), "devtools-sqlite-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            DatabasePath = Path.Combine(_root, "devtools.db");

            Options = new DbContextOptionsBuilder<DevToolsDbContext>()
                .UseSqlite($"Data Source={DatabasePath}")
                .Options;

            using var db = new DevToolsDbContext(Options);
            db.Database.EnsureCreated();
        }

        public string DatabasePath { get; }
        public DbContextOptions<DevToolsDbContext> Options { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_root))
                {
                    Directory.Delete(_root, true);
                }
            }
            catch
            {
                // ignore cleanup issues in temp folder
            }
        }
    }
}


