using System.Reflection;
using System.Text.Json;
using DevTools.Core.Abstractions;
using DevTools.Core.Results;

namespace DevTools.Harvest.Configuration;

public static class HarvestConfigLoader
{
    private const string EmbeddedConfigName = "DevTools.Harvest.Configuration.HarvestConfig.json";

    public static async Task<RunResult<HarvestConfig>> LoadAsync(
        IFileSystem fs,
        string? configPath,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            if (!fs.FileExists(configPath))
            {
                return RunResult<HarvestConfig>.Fail(new ErrorDetail(
                    "harvest.config.not_found",
                    "Config file not found.",
                    configPath));
            }

            try
            {
                var json = await fs.ReadAllTextAsync(configPath, ct).ConfigureAwait(false);
                var cfg = Deserialize(json);
                return RunResult<HarvestConfig>.Success(cfg);
            }
            catch (Exception ex)
            {
                return RunResult<HarvestConfig>.Fail(new ErrorDetail(
                    "harvest.config.invalid",
                    "Failed to read or parse config file.",
                    configPath,
                    ex));
            }
        }

        var embedded = ReadEmbeddedConfig();
        if (!string.IsNullOrWhiteSpace(embedded))
        {
            try
            {
                var cfg = Deserialize(embedded);
                return RunResult<HarvestConfig>.Success(cfg);
            }
            catch (Exception ex)
            {
                return RunResult<HarvestConfig>.Fail(new ErrorDetail(
                    "harvest.config.embedded_invalid",
                    "Failed to parse embedded config.",
                    EmbeddedConfigName,
                    ex));
            }
        }

        return RunResult<HarvestConfig>.Success(new HarvestConfig());
    }

    private static HarvestConfig Deserialize(string json)
    {
        var cfg = JsonSerializer.Deserialize<HarvestConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new HarvestConfig();

        cfg.Normalize();
        return cfg;
    }

    private static string? ReadEmbeddedConfig()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(EmbeddedConfigName);
            if (stream is null) return null;

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch
        {
            return null;
        }
    }
}
