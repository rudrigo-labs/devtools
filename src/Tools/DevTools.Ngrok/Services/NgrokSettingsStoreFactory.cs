using DevTools.Ngrok.Models;

namespace DevTools.Ngrok.Services;

public static class NgrokSettingsStoreFactory
{
    private const string StorageBackendEnvVar = "DEVTOOLS_STORAGE_BACKEND";

    public static INgrokSettingsStore CreateDefault()
    {
        return ResolveBackend() == NgrokStorageBackend.Sqlite
            ? new NgrokSqliteSettingsStore()
            : new NgrokJsonSettingsStore();
    }

    public static NgrokStorageBackend ResolveBackend()
    {
        var raw = Environment.GetEnvironmentVariable(StorageBackendEnvVar);
        return string.Equals(raw, "sqlite", StringComparison.OrdinalIgnoreCase)
            ? NgrokStorageBackend.Sqlite
            : NgrokStorageBackend.Json;
    }
}
