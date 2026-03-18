using System.Text;
using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Harvest.Engine;
using DevTools.Harvest.Models;
using DevTools.Harvest.Repositories;
using DevTools.Harvest.Services;
using DevTools.Organizer.Engine;
using DevTools.Organizer.Models;
using DevTools.Organizer.Repositories;
using DevTools.Organizer.Services;
using DevTools.Snapshot.Engine;
using DevTools.Snapshot.Models;
using DevTools.Snapshot.Repositories;
using DevTools.Snapshot.Services;

namespace DevTools.IntegrationTests.Execution;

public sealed class ExecutionAndConfigurationIntegrationTests
{
    [Fact]
    public async Task SnapshotDeveSalvarConfigurarExecutarEAlternarComSucesso()
    {
        var tempRoot = CreateTempDirectory("snapshot");
        var sourceDir = Path.Combine(tempRoot, "source");
        var outputV1 = Path.Combine(tempRoot, "out-v1");
        var outputV2 = Path.Combine(tempRoot, "out-v2");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outputV1);
        Directory.CreateDirectory(outputV2);

        await File.WriteAllTextAsync(Path.Combine(sourceDir, "a.cs"), "namespace Demo; public class A { }", Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "b.txt"), "conteudo de teste", Encoding.UTF8);

        try
        {
            var repo = new SnapshotRepositoryStub();
            var service = new SnapshotEntityService(repo);
            var engine = new SnapshotEngine();

            var cfgV1 = new SnapshotEntity
            {
                Name = "Snapshot V1",
                RootPath = sourceDir,
                OutputBasePath = outputV1,
                GenerateText = true
            };

            var saveV1 = await service.UpsertAsync(cfgV1);
            Assert.True(saveV1.IsValid);

            var execV1 = await engine.ExecuteAsync(ToRequest(cfgV1));
            Assert.True(execV1.IsSuccess);
            Assert.NotEmpty(execV1.Value!.GeneratedArtifacts);

            cfgV1.OutputBasePath = outputV2;
            cfgV1.GenerateJsonNested = true;
            cfgV1.GenerateText = false;

            var saveV2 = await service.UpsertAsync(cfgV1);
            Assert.True(saveV2.IsValid);

            var execV2 = await engine.ExecuteAsync(ToRequest(cfgV1));
            Assert.True(execV2.IsSuccess);
            Assert.NotEmpty(execV2.Value!.GeneratedArtifacts);

            TestResultWriter.Write(nameof(SnapshotDeveSalvarConfigurarExecutarEAlternarComSucesso),
                "PASS",
                $"saveV1={saveV1.IsValid}; execV1={execV1.IsSuccess}; saveV2={saveV2.IsValid}; execV2={execV2.IsSuccess}; artifactsV1={execV1.Value!.GeneratedArtifacts.Count}; artifactsV2={execV2.Value!.GeneratedArtifacts.Count}");
        }
        finally
        {
            SafeDeleteDirectory(tempRoot);
        }
    }

    [Fact]
    public async Task HarvestDeveSalvarConfigurarExecutarEAlternarComSucesso()
    {
        var tempRoot = CreateTempDirectory("harvest");
        var sourceDir = Path.Combine(tempRoot, "source");
        var outputDir = Path.Combine(tempRoot, "out");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outputDir);

        await File.WriteAllTextAsync(Path.Combine(sourceDir, "Program.cs"),
            """
            namespace Demo;
            public static class Program
            {
                public static void Main() {}
            }
            """,
            Encoding.UTF8);

        try
        {
            var repo = new HarvestRepositoryStub();
            var service = new HarvestEntityService(repo);
            var engine = new HarvestEngine();

            var cfg = new HarvestEntity
            {
                Name = "Harvest V1",
                RootPath = sourceDir,
                OutputPath = outputDir,
                MinScore = 0,
                CopyFiles = false
            };

            var saveV1 = await service.UpsertAsync(cfg);
            Assert.True(saveV1.IsValid);

            var execV1 = await engine.ExecuteAsync(ToRequest(cfg));
            Assert.True(execV1.IsSuccess);
            Assert.True(execV1.Value!.Report.TotalFilesAnalyzed >= 1);

            cfg.MinScore = 1000;
            var saveV2 = await service.UpsertAsync(cfg);
            Assert.True(saveV2.IsValid);

            var execV2 = await engine.ExecuteAsync(ToRequest(cfg));
            Assert.True(execV2.IsSuccess);

            TestResultWriter.Write(nameof(HarvestDeveSalvarConfigurarExecutarEAlternarComSucesso),
                "PASS",
                $"saveV1={saveV1.IsValid}; execV1={execV1.IsSuccess}; saveV2={saveV2.IsValid}; execV2={execV2.IsSuccess}; analyzed={execV2.Value!.Report.TotalFilesAnalyzed}; scoredV2={execV2.Value!.Report.TotalFilesScored}");
        }
        finally
        {
            SafeDeleteDirectory(tempRoot);
        }
    }

    [Fact]
    public async Task OrganizerDeveSalvarConfigurarExecutarEAlternarComSucesso()
    {
        var tempRoot = CreateTempDirectory("organizer");
        var inboxDir = Path.Combine(tempRoot, "inbox");
        var outputDir = Path.Combine(tempRoot, "out");
        Directory.CreateDirectory(inboxDir);
        Directory.CreateDirectory(outputDir);

        await File.WriteAllTextAsync(Path.Combine(inboxDir, "contrato.txt"),
            "Contrato de prestação de serviços com cláusulas e vigência.",
            Encoding.UTF8);

        try
        {
            var repo = new OrganizerRepositoryStub();
            var service = new OrganizerEntityService(repo);
            var engine = new OrganizerEngine();

            var cfg = new OrganizerEntity
            {
                Name = "Organizer V1",
                InboxPath = inboxDir,
                OutputPath = outputDir,
                Apply = false
            };

            var saveV1 = await service.UpsertAsync(cfg);
            Assert.True(saveV1.IsValid);

            var execV1 = await engine.ExecuteAsync(ToRequest(cfg));
            Assert.True(execV1.IsSuccess);
            Assert.NotEmpty(execV1.Value!.Plan);

            cfg.Apply = true;
            var saveV2 = await service.UpsertAsync(cfg);
            Assert.True(saveV2.IsValid);

            var execV2 = await engine.ExecuteAsync(ToRequest(cfg));
            Assert.True(execV2.IsSuccess);

            TestResultWriter.Write(nameof(OrganizerDeveSalvarConfigurarExecutarEAlternarComSucesso),
                "PASS",
                $"saveV1={saveV1.IsValid}; execV1={execV1.IsSuccess}; saveV2={saveV2.IsValid}; execV2={execV2.IsSuccess}; totalPlan={execV2.Value!.Plan.Count}; moved={execV2.Value.Stats.WouldMove}");
        }
        finally
        {
            SafeDeleteDirectory(tempRoot);
        }
    }

    private static SnapshotRequest ToRequest(SnapshotEntity entity) => new()
    {
        RootPath = entity.RootPath,
        OutputBasePath = entity.OutputBasePath,
        GenerateText = entity.GenerateText,
        GenerateJsonNested = entity.GenerateJsonNested,
        GenerateJsonRecursive = entity.GenerateJsonRecursive,
        GenerateHtmlPreview = entity.GenerateHtmlPreview,
        IgnoredDirectories = entity.IgnoredDirectories,
        IgnoredExtensions = entity.IgnoredExtensions,
        IncludedExtensions = entity.IncludedExtensions,
        MaxFileSizeKb = entity.MaxFileSizeKb
    };

    private static HarvestRequest ToRequest(HarvestEntity entity) => new()
    {
        RootPath = entity.RootPath,
        OutputPath = entity.OutputPath,
        CopyFiles = entity.CopyFiles,
        MinScore = entity.MinScore,
        IgnoredDirectories = entity.IgnoredDirectories,
        IgnoredExtensions = entity.IgnoredExtensions,
        IncludedExtensions = entity.IncludedExtensions,
        MaxFileSizeKb = entity.MaxFileSizeKb,
        FanInWeight = entity.FanInWeight,
        FanOutWeight = entity.FanOutWeight,
        KeywordDensityWeight = entity.KeywordDensityWeight,
        DensityScale = entity.DensityScale,
        StaticMethodThreshold = entity.StaticMethodThreshold,
        StaticMethodBonus = entity.StaticMethodBonus,
        DeadCodePenalty = entity.DeadCodePenalty,
        LargeFileThresholdLines = entity.LargeFileThresholdLines,
        LargeFilePenalty = entity.LargeFilePenalty,
        Categories = entity.Categories
    };

    private static OrganizerRequest ToRequest(OrganizerEntity entity) => new()
    {
        InboxPath = entity.InboxPath,
        OutputPath = entity.OutputPath,
        MinScore = entity.MinScore,
        Apply = entity.Apply,
        AllowedExtensions = entity.AllowedExtensions,
        FileNameWeight = entity.FileNameWeight,
        DeduplicateByHash = entity.DeduplicateByHash,
        DeduplicateByName = entity.DeduplicateByName,
        DeduplicateFirstLines = entity.DeduplicateFirstLines,
        DuplicatesFolderName = entity.DuplicatesFolderName,
        OthersFolderName = entity.OthersFolderName,
        Categories = entity.Categories
    };

    private static string CreateTempDirectory(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), $"devtools-it-{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void SafeDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Ignora limpeza para não mascarar o resultado do teste.
        }
    }

    private abstract class InMemoryRepositoryBase<T> : IRepository<T> where T : NamedConfiguration
    {
        protected readonly List<T> Items = [];

        public Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<T>)Items.ToList());

        public Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task UpsertAsync(T entity, CancellationToken cancellationToken = default)
        {
            var idx = Items.FindIndex(x => x.Id == entity.Id);
            if (idx >= 0)
                Items[idx] = entity;
            else
                Items.Add(entity);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class SnapshotRepositoryStub : InMemoryRepositoryBase<SnapshotEntity>, ISnapshotEntityRepository
    {
        public Task<SnapshotEntity?> GetDefaultAsync(CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.IsDefault));

        public Task SetDefaultAsync(string id, CancellationToken ct = default)
        {
            foreach (var item in Items)
                item.IsDefault = item.Id == id;
            return Task.CompletedTask;
        }
    }

    private sealed class HarvestRepositoryStub : InMemoryRepositoryBase<HarvestEntity>, IHarvestEntityRepository
    {
        public Task<HarvestEntity?> GetDefaultAsync(CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.IsDefault));

        public Task SetDefaultAsync(string id, CancellationToken ct = default)
        {
            foreach (var item in Items)
                item.IsDefault = item.Id == id;
            return Task.CompletedTask;
        }
    }

    private sealed class OrganizerRepositoryStub : InMemoryRepositoryBase<OrganizerEntity>, IOrganizerEntityRepository
    {
        public Task<OrganizerEntity?> GetDefaultAsync(CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.IsDefault));

        public Task SetDefaultAsync(string id, CancellationToken ct = default)
        {
            foreach (var item in Items)
                item.IsDefault = item.Id == id;
            return Task.CompletedTask;
        }
    }
}

internal static class TestResultWriter
{
    private static readonly object Sync = new();

    public static void Write(string testName, string status, string details)
    {
        var repoRoot = ResolveRepositoryRoot();
        var reportDir = Path.Combine(repoRoot, "docs", "tests");
        Directory.CreateDirectory(reportDir);

        var reportPath = Path.Combine(reportDir, "resultado-testes-integracao-execucao-configuracao.log");
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss zzz} | {testName} | {status} | {details}";

        lock (Sync)
        {
            File.AppendAllLines(reportPath, [line], Encoding.UTF8);
        }
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "src", "DevTools.slnx")))
                return current.FullName;

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
