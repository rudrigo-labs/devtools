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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSettingEntity>(entity =>
        {
            entity.ToTable("app_settings");
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasColumnName("key").HasMaxLength(200);
            entity.Property(x => x.ValueJson).HasColumnName("value_json").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        });

        modelBuilder.Entity<ToolConfigurationEntity>(entity =>
        {
            entity.ToTable("tool_configurations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").HasMaxLength(200);
            entity.Property(x => x.ToolSlug).HasColumnName("tool_slug").HasMaxLength(120).IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
            entity.Property(x => x.IsDefault).HasColumnName("is_default").IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnName("payload_json").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

            entity.HasIndex(x => x.ToolSlug).HasDatabaseName("ix_tool_cfg_tool_slug");
            entity.HasIndex(x => new { x.ToolSlug, x.Name }).IsUnique().HasDatabaseName("ux_tool_cfg_tool_slug_name");
            entity.HasIndex(x => new { x.ToolSlug, x.IsDefault }).HasDatabaseName("ix_tool_cfg_tool_slug_default");
        });
    }
}

