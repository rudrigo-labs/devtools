using DevTools.Infrastructure.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence;

public sealed class SqliteBootstrapper
{
    private const string BaselineMigrationId = "20260307052922_SnapshotEntityBase";
    private const string BaselineProductVersion = "9.0.0";
    private readonly SqlitePathProvider _pathProvider;
    private readonly SqliteDbContextOptionsFactory _optionsFactory;

    public SqliteBootstrapper(SqlitePathProvider pathProvider, SqliteDbContextOptionsFactory optionsFactory)
    {
        _pathProvider = pathProvider;
        _optionsFactory = optionsFactory;
    }

    public string DatabasePath => _pathProvider.GetDatabasePath();

    public void Migrate()
    {
        try
        {
            using var dbContext = new DevToolsDbContext(_optionsFactory.Create());
            TryMigrateWithBaseline(dbContext);
            EnsureAppSettingsArtifacts((SqliteConnection)dbContext.Database.GetDbConnection());
            InfraLogger.Info($"SQLite migrated at '{DatabasePath}'.");
        }
        catch (Exception ex)
        {
            InfraLogger.Error("Failed to migrate SQLite database.", ex);
            throw;
        }
    }

    public async Task MigrateAsync(CancellationToken ct = default)
    {
        try
        {
            await using var dbContext = new DevToolsDbContext(_optionsFactory.Create());
            await TryMigrateWithBaselineAsync(dbContext, ct);
            await EnsureAppSettingsArtifactsAsync((SqliteConnection)dbContext.Database.GetDbConnection(), ct);
            InfraLogger.Info($"SQLite migrated at '{DatabasePath}'.");
        }
        catch (Exception ex)
        {
            InfraLogger.Error("Failed to migrate SQLite database.", ex);
            throw;
        }
    }

    public bool CanConnect()
    {
        try
        {
            using var dbContext = new DevToolsDbContext(_optionsFactory.Create());
            return dbContext.Database.CanConnect();
        }
        catch (Exception ex)
        {
            InfraLogger.Error("Failed to connect to SQLite database.", ex);
            return false;
        }
    }

    private void TryMigrateWithBaseline(DevToolsDbContext dbContext)
    {
        try
        {
            dbContext.Database.Migrate();
        }
        catch (SqliteException ex) when (IsTableAlreadyExistsError(ex))
        {
            if (!TryRegisterBaselineMigration(dbContext))
                throw;

            InfraLogger.Warn("SQLite baseline migration registered for existing database.");
            dbContext.Database.Migrate();
        }
    }

    private async Task TryMigrateWithBaselineAsync(DevToolsDbContext dbContext, CancellationToken ct)
    {
        try
        {
            await dbContext.Database.MigrateAsync(ct);
        }
        catch (SqliteException ex) when (IsTableAlreadyExistsError(ex))
        {
            if (!await TryRegisterBaselineMigrationAsync(dbContext, ct))
                throw;

            InfraLogger.Warn("SQLite baseline migration registered for existing database.");
            await dbContext.Database.MigrateAsync(ct);
        }
    }

    private static bool IsTableAlreadyExistsError(SqliteException ex)
    {
        return ex.SqliteErrorCode == 1
            && ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryRegisterBaselineMigration(DevToolsDbContext dbContext)
    {
        var connection = (SqliteConnection)dbContext.Database.GetDbConnection();
        connection.Open();

        EnsureAppSettingsArtifacts(connection);
        if (!TableExists(connection, "app_settings"))
            return false;

        EnsureToolConfigurationsArtifacts(connection);
        if (!TableExists(connection, "tool_configurations"))
            return false;

        EnsureMigrationsHistoryTable(connection);
        if (MigrationHistoryContains(connection, BaselineMigrationId))
            return true;

        using var transaction = connection.BeginTransaction();
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText =
            "INSERT INTO __EFMigrationsHistory(MigrationId, ProductVersion) VALUES ($migrationId, $productVersion);";
        insert.Parameters.AddWithValue("$migrationId", BaselineMigrationId);
        insert.Parameters.AddWithValue("$productVersion", BaselineProductVersion);
        insert.ExecuteNonQuery();
        transaction.Commit();
        return true;
    }

    private static async Task<bool> TryRegisterBaselineMigrationAsync(DevToolsDbContext dbContext, CancellationToken ct)
    {
        var connection = (SqliteConnection)dbContext.Database.GetDbConnection();
        await connection.OpenAsync(ct);

        await EnsureAppSettingsArtifactsAsync(connection, ct);
        if (!await TableExistsAsync(connection, "app_settings", ct))
            return false;

        await EnsureToolConfigurationsArtifactsAsync(connection, ct);
        if (!await TableExistsAsync(connection, "tool_configurations", ct))
            return false;

        await EnsureMigrationsHistoryTableAsync(connection, ct);
        if (await MigrationHistoryContainsAsync(connection, BaselineMigrationId, ct))
            return true;

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);
        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText =
            "INSERT INTO __EFMigrationsHistory(MigrationId, ProductVersion) VALUES ($migrationId, $productVersion);";
        insert.Parameters.AddWithValue("$migrationId", BaselineMigrationId);
        insert.Parameters.AddWithValue("$productVersion", BaselineProductVersion);
        await insert.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private static void EnsureMigrationsHistoryTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
                MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
                ProductVersion TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private static void EnsureToolConfigurationsArtifacts(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS tool_configurations (
                id TEXT NOT NULL CONSTRAINT PK_tool_configurations PRIMARY KEY,
                tool_slug TEXT NOT NULL,
                name TEXT NOT NULL,
                description TEXT NOT NULL,
                is_active INTEGER NOT NULL,
                is_default INTEGER NOT NULL,
                payload_json TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_tool_cfg_tool_slug ON tool_configurations(tool_slug);
            CREATE INDEX IF NOT EXISTS ix_tool_cfg_tool_slug_default ON tool_configurations(tool_slug, is_default);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_tool_cfg_tool_slug_name ON tool_configurations(tool_slug, name);
            """;
        command.ExecuteNonQuery();
    }

    private static async Task EnsureMigrationsHistoryTableAsync(SqliteConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
                MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
                ProductVersion TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task EnsureToolConfigurationsArtifactsAsync(SqliteConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS tool_configurations (
                id TEXT NOT NULL CONSTRAINT PK_tool_configurations PRIMARY KEY,
                tool_slug TEXT NOT NULL,
                name TEXT NOT NULL,
                description TEXT NOT NULL,
                is_active INTEGER NOT NULL,
                is_default INTEGER NOT NULL,
                payload_json TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_tool_cfg_tool_slug ON tool_configurations(tool_slug);
            CREATE INDEX IF NOT EXISTS ix_tool_cfg_tool_slug_default ON tool_configurations(tool_slug, is_default);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_tool_cfg_tool_slug_name ON tool_configurations(tool_slug, name);
            """;
        await command.ExecuteNonQueryAsync(ct);
    }

    private static bool MigrationHistoryContains(SqliteConnection connection, string migrationId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM __EFMigrationsHistory WHERE MigrationId = $migrationId;";
        command.Parameters.AddWithValue("$migrationId", migrationId);
        var count = Convert.ToInt32(command.ExecuteScalar());
        return count > 0;
    }

    private static async Task<bool> MigrationHistoryContainsAsync(
        SqliteConnection connection,
        string migrationId,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM __EFMigrationsHistory WHERE MigrationId = $migrationId;";
        command.Parameters.AddWithValue("$migrationId", migrationId);
        var scalar = await command.ExecuteScalarAsync(ct);
        var count = Convert.ToInt32(scalar);
        return count > 0;
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = $name;";
        command.Parameters.AddWithValue("$name", tableName);
        var count = Convert.ToInt32(command.ExecuteScalar());
        return count > 0;
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = $name;";
        command.Parameters.AddWithValue("$name", tableName);
        var scalar = await command.ExecuteScalarAsync(ct);
        var count = Convert.ToInt32(scalar);
        return count > 0;
    }

    private static void EnsureAppSettingsArtifacts(SqliteConnection connection)
    {
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            connection.Open();

        try
        {
            using var create = connection.CreateCommand();
            create.CommandText = """
                CREATE TABLE IF NOT EXISTS app_settings (
                    key TEXT NOT NULL CONSTRAINT PK_app_settings PRIMARY KEY,
                    value_json TEXT NOT NULL,
                    updated_at_utc TEXT NOT NULL
                );
                """;
            create.ExecuteNonQuery();

            if (!ColumnExists(connection, "app_settings", "value_json"))
            {
                using var alter = connection.CreateCommand();
                alter.CommandText = "ALTER TABLE app_settings ADD COLUMN value_json TEXT NOT NULL DEFAULT '{}';";
                alter.ExecuteNonQuery();
            }

            if (ColumnExists(connection, "app_settings", "value"))
            {
                using var copyLegacyValue = connection.CreateCommand();
                copyLegacyValue.CommandText = """
                    UPDATE app_settings
                    SET value_json = COALESCE("value", '{}')
                    WHERE value_json IS NULL OR TRIM(value_json) = '' OR value_json = '{}';
                    """;
                copyLegacyValue.ExecuteNonQuery();
            }

            if (!ColumnExists(connection, "app_settings", "updated_at_utc"))
            {
                using var alter = connection.CreateCommand();
                alter.CommandText = "ALTER TABLE app_settings ADD COLUMN updated_at_utc TEXT NOT NULL DEFAULT '1970-01-01T00:00:00Z';";
                alter.ExecuteNonQuery();
            }
        }
        finally
        {
            if (shouldClose)
                connection.Close();
        }
    }

    private static async Task EnsureAppSettingsArtifactsAsync(SqliteConnection connection, CancellationToken ct)
    {
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync(ct);

        try
        {
            await using var create = connection.CreateCommand();
            create.CommandText = """
                CREATE TABLE IF NOT EXISTS app_settings (
                    key TEXT NOT NULL CONSTRAINT PK_app_settings PRIMARY KEY,
                    value_json TEXT NOT NULL,
                    updated_at_utc TEXT NOT NULL
                );
                """;
            await create.ExecuteNonQueryAsync(ct);

            if (!await ColumnExistsAsync(connection, "app_settings", "value_json", ct))
            {
                await using var alter = connection.CreateCommand();
                alter.CommandText = "ALTER TABLE app_settings ADD COLUMN value_json TEXT NOT NULL DEFAULT '{}';";
                await alter.ExecuteNonQueryAsync(ct);
            }

            if (await ColumnExistsAsync(connection, "app_settings", "value", ct))
            {
                await using var copyLegacyValue = connection.CreateCommand();
                copyLegacyValue.CommandText = """
                    UPDATE app_settings
                    SET value_json = COALESCE("value", '{}')
                    WHERE value_json IS NULL OR TRIM(value_json) = '' OR value_json = '{}';
                    """;
                await copyLegacyValue.ExecuteNonQueryAsync(ct);
            }

            if (!await ColumnExistsAsync(connection, "app_settings", "updated_at_utc", ct))
            {
                await using var alter = connection.CreateCommand();
                alter.CommandText = "ALTER TABLE app_settings ADD COLUMN updated_at_utc TEXT NOT NULL DEFAULT '1970-01-01T00:00:00Z';";
                await alter.ExecuteNonQueryAsync(ct);
            }
        }
        finally
        {
            if (shouldClose)
                connection.Close();
        }
    }

    private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var current = reader["name"]?.ToString();
            if (string.Equals(current, columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static async Task<bool> ColumnExistsAsync(SqliteConnection connection, string tableName, string columnName, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var current = reader["name"]?.ToString();
            if (string.Equals(current, columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
