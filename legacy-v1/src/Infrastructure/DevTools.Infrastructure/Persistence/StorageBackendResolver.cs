using System;

namespace DevTools.Infrastructure.Persistence;

public static class StorageBackendResolver
{
    private const string BackendEnvVar = "DEVTOOLS_STORAGE_BACKEND";

    public static StorageBackend Resolve()
    {
        var raw = Environment.GetEnvironmentVariable(BackendEnvVar);
        if (string.Equals(raw, "sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return StorageBackend.Sqlite;
        }

        return StorageBackend.Json;
    }
}


