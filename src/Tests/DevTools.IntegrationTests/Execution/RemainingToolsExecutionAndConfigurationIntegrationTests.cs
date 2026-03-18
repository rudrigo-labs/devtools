using System.Text;
using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Image.Engine;
using DevTools.Image.Models;
using DevTools.Migrations.Engine;
using DevTools.Migrations.Models;
using DevTools.Migrations.Repositories;
using DevTools.Migrations.Services;
using DevTools.Ngrok.Engine;
using DevTools.Ngrok.Models;
using DevTools.Ngrok.Repositories;
using DevTools.Ngrok.Services;
using DevTools.Rename.Engine;
using DevTools.Rename.Models;
using DevTools.SearchText.Engine;
using DevTools.SearchText.Models;
using DevTools.SSHTunnel.Engine;
using DevTools.SSHTunnel.Models;
using DevTools.SSHTunnel.Repositories;
using DevTools.SSHTunnel.Services;
using DevTools.Utf8Convert.Engine;
using DevTools.Utf8Convert.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DevTools.IntegrationTests.Execution;

public sealed class RemainingToolsExecutionAndConfigurationIntegrationTests
{
    [Fact]
    public async Task MigrationsDeveSalvarConfigurarExecutarEAlternarComSucesso()
    {
        var tempRoot = CreateTempDirectory("migrations");
        var startupProject = Path.Combine(tempRoot, "App.Startup.csproj");
        var migrationsProject = Path.Combine(tempRoot, "App.Migrations.csproj");
        await File.WriteAllTextAsync(startupProject, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>", Encoding.UTF8);
        await File.WriteAllTextAsync(migrationsProject, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>", Encoding.UTF8);

        try
        {
            var repo = new MigrationsRepositoryStub();
            var service = new MigrationsEntityService(repo);
            var runner = new FakeProcessRunner();
            var engine = new MigrationsEngine(runner);

            var config = new MigrationsEntity
            {
                Name = "Migrations V1",
                RootPath = tempRoot,
                StartupProjectPath = startupProject,
                DbContextFullName = "DemoDbContext",
                Targets =
                [
                    new MigrationTarget
                    {
                        Provider = DatabaseProvider.Sqlite,
                        MigrationsProjectPath = migrationsProject
                    }
                ]
            };

            var saveV1 = await service.UpsertAsync(config);
            Assert.True(saveV1.IsValid);

            var execV1 = await engine.ExecuteAsync(new MigrationsRequest
            {
                Action = MigrationsAction.AddMigration,
                Provider = DatabaseProvider.Sqlite,
                Settings = config,
                MigrationName = "InitMigration",
                DryRun = true
            });
            Assert.True(execV1.IsSuccess);
            Assert.True(execV1.Value!.WasDryRun);

            config.AdditionalArgs = "--verbose";
            var saveV2 = await service.UpsertAsync(config);
            Assert.True(saveV2.IsValid);

            var execV2 = await engine.ExecuteAsync(new MigrationsRequest
            {
                Action = MigrationsAction.UpdateDatabase,
                Provider = DatabaseProvider.Sqlite,
                Settings = config,
                DryRun = true
            });
            Assert.True(execV2.IsSuccess);
            Assert.True(execV2.Value!.WasDryRun);
            Assert.Equal(0, runner.Calls);

            TestResultWriter.Write(nameof(MigrationsDeveSalvarConfigurarExecutarEAlternarComSucesso),
                "PASS",
                $"saveV1={saveV1.IsValid}; execV1={execV1.IsSuccess}; saveV2={saveV2.IsValid}; execV2={execV2.IsSuccess}; runnerCalls={runner.Calls}");
        }
        finally
        {
            SafeDeleteDirectory(tempRoot);
        }
    }

    [Fact]
    public async Task SshTunnelDeveSalvarConfigurarExecutarEAlternarComSucesso()
    {
        var repo = new SshTunnelRepositoryStub();
        var service = new SshTunnelEntityService(repo);
        var runner = new FakeProcessRunner();
        var engine = new SshTunnelEngine(runner);

        var config = new SshTunnelEntity
        {
            Name = "SSH Tunnel V1",
            SshHost = "localhost",
            SshPort = 22,
            SshUser = "devtools",
            LocalBindHost = "127.0.0.1",
            LocalPort = 15432,
            RemoteHost = "127.0.0.1",
            RemotePort = 5432
        };

        var saveV1 = await service.UpsertAsync(config);
        Assert.True(saveV1.IsValid);

        var execV1 = await engine.ExecuteAsync(new SshTunnelRequest { Action = SshTunnelAction.Status });
        Assert.True(execV1.IsSuccess);
        Assert.NotNull(execV1.Value);

        config.LocalPort = 15433;
        var saveV2 = await service.UpsertAsync(config);
        Assert.True(saveV2.IsValid);

        var execV2 = await engine.ExecuteAsync(new SshTunnelRequest { Action = SshTunnelAction.Stop });
        Assert.True(execV2.IsSuccess);
        Assert.NotNull(execV2.Value);
        Assert.Equal(0, runner.Calls);

        TestResultWriter.Write(nameof(SshTunnelDeveSalvarConfigurarExecutarEAlternarComSucesso),
            "PASS",
            $"saveV1={saveV1.IsValid}; execV1={execV1.IsSuccess}; saveV2={saveV2.IsValid}; execV2={execV2.IsSuccess}; runnerCalls={runner.Calls}");
    }

    [Fact]
    public async Task NgrokDeveSalvarConfigurarExecutarEAlternarComSucesso()
    {
        var repo = new NgrokRepositoryStub();
        var service = new NgrokEntityService(repo);
        var engine = new NgrokEngine();

        var config = new NgrokEntity
        {
            Name = "Ngrok V1",
            BaseUrl = "http://127.0.0.1:4040/"
        };

        var saveV1 = await service.UpsertAsync(config);
        Assert.True(saveV1.IsValid);

        var execV1 = await engine.ExecuteAsync(new NgrokRequest { Action = NgrokAction.Status });
        Assert.True(execV1.IsSuccess);
        Assert.NotNull(execV1.Value);

        config.BaseUrl = "http://localhost:4040/";
        var saveV2 = await service.UpsertAsync(config);
        Assert.True(saveV2.IsValid);

        var execV2 = await engine.ExecuteAsync(new NgrokRequest { Action = NgrokAction.KillAll });
        Assert.True(execV2.IsSuccess);
        Assert.NotNull(execV2.Value);

        TestResultWriter.Write(nameof(NgrokDeveSalvarConfigurarExecutarEAlternarComSucesso),
            "PASS",
            $"saveV1={saveV1.IsValid}; execV1={execV1.IsSuccess}; saveV2={saveV2.IsValid}; execV2={execV2.IsSuccess}; killed={execV2.Value!.Killed}");
    }

    [Fact]
    public async Task RenameDeveExecutarEAlternarConfiguracaoDeExecucaoComSucesso()
    {
        var tempRoot = CreateTempDirectory("rename");
        var inputFile = Path.Combine(tempRoot, "FooFile.txt");
        await File.WriteAllTextAsync(inputFile, "Foo valor inicial", Encoding.UTF8);

        try
        {
            var engine = new RenameEngine();

            var execV1 = await engine.ExecuteAsync(new RenameRequest
            {
                RootPath = tempRoot,
                OldText = "Foo",
                NewText = "Bar",
                Mode = RenameMode.General,
                DryRun = true,
                BackupEnabled = false,
                WriteUndoLog = false
            });
            Assert.True(execV1.IsSuccess);
            Assert.NotNull(execV1.Value);

            var execV2 = await engine.ExecuteAsync(new RenameRequest
            {
                RootPath = tempRoot,
                OldText = "Foo",
                NewText = "Baz",
                Mode = RenameMode.General,
                DryRun = false,
                BackupEnabled = false,
                WriteUndoLog = false
            });
            Assert.True(execV2.IsSuccess);
            Assert.NotNull(execV2.Value);

            TestResultWriter.Write(nameof(RenameDeveExecutarEAlternarConfiguracaoDeExecucaoComSucesso),
                "PASS",
                $"execV1={execV1.IsSuccess}; execV2={execV2.IsSuccess}; filesScannedV2={execV2.Value!.Summary.FilesScanned}; filesUpdatedV2={execV2.Value.Summary.FilesUpdated}");
        }
        finally
        {
            SafeDeleteDirectory(tempRoot);
        }
    }

    [Fact]
    public async Task SearchTextDeveExecutarEAlternarPadroesComSucesso()
    {
        var tempRoot = CreateTempDirectory("searchtext");
        await File.WriteAllTextAsync(Path.Combine(tempRoot, "notes.txt"), "tokenA tokenB tokenA", Encoding.UTF8);

        try
        {
            var engine = new SearchTextEngine();

            var execV1 = await engine.ExecuteAsync(new SearchTextRequest
            {
                RootPath = tempRoot,
                Pattern = "tokenA",
                UseRegex = false
            });
            Assert.True(execV1.IsSuccess);
            Assert.True(execV1.Value!.TotalOccurrences >= 1);

            var execV2 = await engine.ExecuteAsync(new SearchTextRequest
            {
                RootPath = tempRoot,
                Pattern = "naoExiste",
                UseRegex = false
            });
            Assert.True(execV2.IsSuccess);
            Assert.Equal(0, execV2.Value!.TotalOccurrences);

            TestResultWriter.Write(nameof(SearchTextDeveExecutarEAlternarPadroesComSucesso),
                "PASS",
                $"execV1={execV1.IsSuccess}; occurrencesV1={execV1.Value!.TotalOccurrences}; execV2={execV2.IsSuccess}; occurrencesV2={execV2.Value!.TotalOccurrences}");
        }
        finally
        {
            SafeDeleteDirectory(tempRoot);
        }
    }

    [Fact]
    public async Task Utf8ConvertDeveExecutarEAlternarBomComSucesso()
    {
        var tempRoot = CreateTempDirectory("utf8");
        var filePath = Path.Combine(tempRoot, "texto.txt");
        await File.WriteAllTextAsync(filePath, "conteudo utf8 sem bom", new UTF8Encoding(false));

        try
        {
            var engine = new Utf8ConvertEngine();

            var execV1 = await engine.ExecuteAsync(new Utf8ConvertRequest
            {
                RootPath = tempRoot,
                Recursive = false,
                DryRun = true,
                OutputBom = false,
                CreateBackup = false
            });
            Assert.True(execV1.IsSuccess);

            var execV2 = await engine.ExecuteAsync(new Utf8ConvertRequest
            {
                RootPath = tempRoot,
                Recursive = false,
                DryRun = false,
                OutputBom = true,
                CreateBackup = false
            });
            Assert.True(execV2.IsSuccess);

            TestResultWriter.Write(nameof(Utf8ConvertDeveExecutarEAlternarBomComSucesso),
                "PASS",
                $"execV1={execV1.IsSuccess}; convertedV1={execV1.Value!.Summary.Converted}; execV2={execV2.IsSuccess}; convertedV2={execV2.Value!.Summary.Converted}");
        }
        finally
        {
            SafeDeleteDirectory(tempRoot);
        }
    }

    [Fact]
    public async Task ImageSplitDeveExecutarEAlternarFiltroDeRegiaoComSucesso()
    {
        var tempRoot = CreateTempDirectory("imagesplit");
        var outputV1 = Path.Combine(tempRoot, "out-v1");
        var outputV2 = Path.Combine(tempRoot, "out-v2");
        Directory.CreateDirectory(outputV1);
        Directory.CreateDirectory(outputV2);

        var inputImage = Path.Combine(tempRoot, "input.png");
        using (var image = new Image<Rgba32>(8, 8))
        {
            image[1, 1] = new Rgba32(255, 0, 0, 255);
            image[2, 1] = new Rgba32(255, 0, 0, 255);
            image[5, 5] = new Rgba32(0, 255, 0, 255);
            image[5, 6] = new Rgba32(0, 255, 0, 255);
            await image.SaveAsPngAsync(inputImage);
        }

        try
        {
            var engine = new ImageSplitEngine();

            var execV1 = await engine.ExecuteAsync(new ImageSplitRequest
            {
                InputPath = inputImage,
                OutputDirectory = outputV1,
                OutputBaseName = "piece",
                OutputExtension = ".png",
                MinRegionWidth = 1,
                MinRegionHeight = 1,
                StartIndex = 1,
                Overwrite = true
            });
            Assert.True(execV1.IsSuccess);
            Assert.True(execV1.Value!.Outputs.Count >= 2);

            var execV2 = await engine.ExecuteAsync(new ImageSplitRequest
            {
                InputPath = inputImage,
                OutputDirectory = outputV2,
                OutputBaseName = "piece",
                OutputExtension = ".png",
                MinRegionWidth = 4,
                MinRegionHeight = 4,
                StartIndex = 1,
                Overwrite = true
            });
            Assert.True(execV2.IsSuccess);
            Assert.Empty(execV2.Value!.Outputs);

            TestResultWriter.Write(nameof(ImageSplitDeveExecutarEAlternarFiltroDeRegiaoComSucesso),
                "PASS",
                $"execV1={execV1.IsSuccess}; outputsV1={execV1.Value!.Outputs.Count}; execV2={execV2.IsSuccess}; outputsV2={execV2.Value!.Outputs.Count}");
        }
        finally
        {
            SafeDeleteDirectory(tempRoot);
        }
    }

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
            // ignora
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

    private sealed class MigrationsRepositoryStub : InMemoryRepositoryBase<MigrationsEntity>, IMigrationsEntityRepository
    {
        public Task<MigrationsEntity?> GetDefaultAsync(CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.IsDefault));

        public Task SetDefaultAsync(string id, CancellationToken ct = default)
        {
            foreach (var item in Items)
                item.IsDefault = item.Id == id;
            return Task.CompletedTask;
        }
    }

    private sealed class SshTunnelRepositoryStub : InMemoryRepositoryBase<SshTunnelEntity>, ISshTunnelEntityRepository
    {
        public Task<SshTunnelEntity?> GetDefaultAsync(CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.IsDefault));

        public Task SetDefaultAsync(string id, CancellationToken ct = default)
        {
            foreach (var item in Items)
                item.IsDefault = item.Id == id;
            return Task.CompletedTask;
        }
    }

    private sealed class NgrokRepositoryStub : InMemoryRepositoryBase<NgrokEntity>, INgrokEntityRepository
    {
        public Task<NgrokEntity?> GetDefaultAsync(CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.IsDefault));

        public Task SetDefaultAsync(string id, CancellationToken ct = default)
        {
            foreach (var item in Items)
                item.IsDefault = item.Id == id;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProcessRunner : IProcessRunner
    {
        public int Calls { get; private set; }

        public Task<ProcessResult> RunAsync(
            string fileName,
            string arguments,
            string? workingDirectory = null,
            IDictionary<string, string?>? environment = null,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new ProcessResult(0, "ok", string.Empty, TimeSpan.Zero));
        }
    }
}
