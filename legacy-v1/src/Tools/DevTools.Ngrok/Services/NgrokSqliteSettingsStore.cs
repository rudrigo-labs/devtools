using DevTools.Ngrok.Models;
using Microsoft.Data.Sqlite;

namespace DevTools.Ngrok.Services;

public sealed class NgrokSqliteSettingsStore : INgrokSettingsStore
{
    private readonly string _connectionString;

    public NgrokSqliteSettingsStore()
        : this(BuildDefaultConnectionString())
    {
    }

    public NgrokSqliteSettingsStore(string connectionString)
    {
        _connectionString = connectionString;
        EnsureSchema();
    }

    public NgrokSettings Load()
    {
        EnsureSchema();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Authtoken, ExecutablePath, AdditionalArgs
            FROM NgrokSettings
            WHERE Id = 1
            LIMIT 1;
            """;

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return new NgrokSettings();

        var settings = new NgrokSettings
        {
            AuthToken = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
            ExecutablePath = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            AdditionalArgs = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
        };

        settings.Normalize();
        return settings;
    }

    public void Save(NgrokSettings settings)
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));

        settings.Normalize();
        EnsureSchema();

        var now = DateTime.UtcNow.ToString("O");

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO NgrokSettings (Id, Authtoken, ExecutablePath, AdditionalArgs, CreatedAt, UpdatedAt)
            VALUES (1, $token, $exePath, $args, $now, $now)
            ON CONFLICT(Id) DO UPDATE SET
                Authtoken = excluded.Authtoken,
                ExecutablePath = excluded.ExecutablePath,
                AdditionalArgs = excluded.AdditionalArgs,
                UpdatedAt = excluded.UpdatedAt;
            """;
        cmd.Parameters.AddWithValue("$token", settings.AuthToken);
        cmd.Parameters.AddWithValue("$exePath", settings.ExecutablePath);
        cmd.Parameters.AddWithValue("$args", settings.AdditionalArgs);
        cmd.Parameters.AddWithValue("$now", now);
        cmd.ExecuteNonQuery();
    }

    private void EnsureSchema()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS NgrokSettings (
                Id INTEGER NOT NULL PRIMARY KEY,
                Authtoken TEXT NOT NULL DEFAULT '',
                ExecutablePath TEXT NOT NULL DEFAULT '',
                AdditionalArgs TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private static string BuildDefaultConnectionString()
    {
        var appDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevTools");
        Directory.CreateDirectory(appDataRoot);
        var dbPath = Path.Combine(appDataRoot, "devtools.db");
        return $"Data Source={dbPath}";
    }
}
