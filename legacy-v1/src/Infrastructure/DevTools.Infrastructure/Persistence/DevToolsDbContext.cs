using DevTools.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence;

public sealed class DevToolsDbContext : DbContext
{
    public DevToolsDbContext(DbContextOptions<DevToolsDbContext> options) : base(options)
    {
    }

    public DbSet<AppSettingEntity> AppSettings => Set<AppSettingEntity>();
    public DbSet<ToolConfigurationEntity> ToolConfigurations => Set<ToolConfigurationEntity>();
    public DbSet<NotesSettingsEntity> NotesSettings => Set<NotesSettingsEntity>();
    public DbSet<GoogleDriveSettingsEntity> GoogleDriveSettings => Set<GoogleDriveSettingsEntity>();
    public DbSet<NoteIndexEntity> NoteIndex => Set<NoteIndexEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSettingEntity>(entity =>
        {
            entity.ToTable("app_settings");
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasColumnName("key");
            entity.Property(x => x.Value).HasColumnName("value").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<ToolConfigurationEntity>(entity =>
        {
            entity.ToTable("tool_configurations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ToolKey).HasColumnName("tool_key").IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").IsRequired();
            entity.Property(x => x.IsDefault).HasColumnName("is_default").IsRequired();
            entity.Property(x => x.OptionsJson).HasColumnName("options_json").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at").IsRequired();
            entity.HasIndex(x => x.ToolKey).HasDatabaseName("ix_tool_configurations_tool_key");
        });

        modelBuilder.Entity<NotesSettingsEntity>(entity =>
        {
            entity.ToTable("notes_settings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.StoragePath).HasColumnName("storage_path");
            entity.Property(x => x.DefaultFormat).HasColumnName("default_format").IsRequired();
            entity.Property(x => x.AutoCloudSync).HasColumnName("auto_cloud_sync").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<GoogleDriveSettingsEntity>(entity =>
        {
            entity.ToTable("google_drive_settings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled").IsRequired();
            entity.Property(x => x.ClientId).HasColumnName("client_id");
            entity.Property(x => x.ProjectId).HasColumnName("project_id");
            entity.Property(x => x.ClientSecretProtected).HasColumnName("client_secret_protected");
            entity.Property(x => x.FolderName).HasColumnName("folder_name");
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<NoteIndexEntity>(entity =>
        {
            entity.ToTable("note_index");
            entity.HasKey(x => x.NoteKey);
            entity.Property(x => x.NoteKey).HasColumnName("note_key");
            entity.Property(x => x.Title).HasColumnName("title").IsRequired();
            entity.Property(x => x.Extension).HasColumnName("extension").IsRequired();
            entity.Property(x => x.LastLocalWriteUtc).HasColumnName("last_local_write_utc").IsRequired();
            entity.Property(x => x.LastCloudSyncUtc).HasColumnName("last_cloud_sync_utc");
            entity.Property(x => x.LastCloudStatus).HasColumnName("last_cloud_status");
            entity.Property(x => x.Hash).HasColumnName("hash");
        });
    }
}



