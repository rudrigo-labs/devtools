using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Harvest.Models;
using DevTools.Harvest.Repositories;
using DevTools.Harvest.Services;
using DevTools.Migrations.Models;
using DevTools.Migrations.Repositories;
using DevTools.Migrations.Services;
using DevTools.Ngrok.Models;
using DevTools.Ngrok.Repositories;
using DevTools.Ngrok.Services;
using DevTools.Organizer.Models;
using DevTools.Organizer.Repositories;
using DevTools.Organizer.Services;
using DevTools.Snapshot.Models;
using DevTools.Snapshot.Repositories;
using DevTools.Snapshot.Services;
using DevTools.SSHTunnel.Models;
using DevTools.SSHTunnel.Repositories;
using DevTools.SSHTunnel.Services;

namespace DevTools.IntegrationTests.Validators;

public sealed class EntityServicesValidatorIntegrationTests
{
    [Fact]
    public async Task SnapshotUpsertComConfiguracaoInvalidaNaoPersiste()
    {
        var repo = new SnapshotRepositoryStub();
        var service = new SnapshotEntityService(repo);
        var entity = new SnapshotEntity
        {
            Name = "Snapshot",
            RootPath = "C:\\repo",
            OutputBasePath = string.Empty,
            GenerateText = true
        };

        var result = await service.UpsertAsync(entity);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "outputBasePath");
        Assert.Equal(0, repo.UpsertCalls);
    }

    [Fact]
    public async Task SnapshotUpsertComConfiguracaoValidaPersiste()
    {
        var repo = new SnapshotRepositoryStub();
        var service = new SnapshotEntityService(repo);
        var entity = new SnapshotEntity
        {
            Name = "Snapshot",
            RootPath = "C:\\repo",
            OutputBasePath = "C:\\out",
            GenerateText = true
        };

        var result = await service.UpsertAsync(entity);

        Assert.True(result.IsValid);
        Assert.Equal(1, repo.UpsertCalls);
        Assert.False(string.IsNullOrWhiteSpace(entity.Id));
    }

    [Fact]
    public async Task HarvestUpsertComConfiguracaoInvalidaNaoPersiste()
    {
        var repo = new HarvestRepositoryStub();
        var service = new HarvestEntityService(repo);
        var entity = new HarvestEntity
        {
            Name = "Harvest",
            RootPath = string.Empty,
            OutputPath = "C:\\out"
        };

        var result = await service.UpsertAsync(entity);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "rootPath");
        Assert.Equal(0, repo.UpsertCalls);
    }

    [Fact]
    public async Task OrganizerUpsertComConfiguracaoInvalidaNaoPersiste()
    {
        var repo = new OrganizerRepositoryStub();
        var service = new OrganizerEntityService(repo);
        var entity = new OrganizerEntity
        {
            Name = string.Empty
        };

        var result = await service.UpsertAsync(entity);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "name");
        Assert.Equal(0, repo.UpsertCalls);
    }

    [Fact]
    public async Task MigrationsUpsertComConfiguracaoInvalidaNaoPersiste()
    {
        var repo = new MigrationsRepositoryStub();
        var service = new MigrationsEntityService(repo);
        var entity = new MigrationsEntity
        {
            Name = "Migração",
            RootPath = "C:\\repo",
            StartupProjectPath = string.Empty,
            DbContextFullName = "MeuDbContext"
        };

        var result = await service.UpsertAsync(entity);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "startupProjectPath");
        Assert.Equal(0, repo.UpsertCalls);
    }

    [Fact]
    public async Task SshTunnelUpsertComConfiguracaoInvalidaNaoPersiste()
    {
        var repo = new SshTunnelRepositoryStub();
        var service = new SshTunnelEntityService(repo);
        var entity = new SshTunnelEntity
        {
            Name = "Tunnel",
            SshHost = "localhost",
            SshUser = "devtools",
            SshPort = 0
        };

        var result = await service.UpsertAsync(entity);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "sshPort");
        Assert.Equal(0, repo.UpsertCalls);
    }

    [Fact]
    public async Task NgrokUpsertComConfiguracaoInvalidaNaoPersiste()
    {
        var repo = new NgrokRepositoryStub();
        var service = new NgrokEntityService(repo);
        var entity = new NgrokEntity
        {
            Name = string.Empty
        };

        var result = await service.UpsertAsync(entity);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "name");
        Assert.Equal(0, repo.UpsertCalls);
    }

    private abstract class InMemoryRepositoryBase<T> : IRepository<T> where T : NamedConfiguration
    {
        protected readonly List<T> Items = [];
        public int UpsertCalls { get; private set; }

        public Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<T>)Items.ToList());

        public Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task UpsertAsync(T entity, CancellationToken cancellationToken = default)
        {
            UpsertCalls++;
            var index = Items.FindIndex(x => x.Id == entity.Id);
            if (index >= 0)
                Items[index] = entity;
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
}
