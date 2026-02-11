using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Organizer.Models;
using DevTools.Core.Providers;
using DevTools.Organizer.Validation;

namespace DevTools.Organizer.Engine;

public sealed class OrganizerEngine : IDevToolEngine<OrganizerRequest, OrganizerResponse>
{
    private readonly IFileSystem _fs;
    private readonly TextExtractor _extractor = new();

    public OrganizerEngine(IFileSystem? fileSystem = null)
    {
        _fs = fileSystem ?? new SystemFileSystem();
    }

    public Task<RunResult<OrganizerResponse>> ExecuteAsync(
        OrganizerRequest request,
        IProgressReporter? progress = null,
        CancellationToken ct = default)
    {
        var errors = OrganizerRequestValidator.Validate(request, _fs);
        if (errors.Count > 0)
            return Task.FromResult(RunResult<OrganizerResponse>.Fail(errors));

        var inbox = Path.GetFullPath(request.InboxPath);
        var output = Path.GetFullPath(request.OutputPath);
        _fs.CreateDirectory(output);

        var duplicatesDir = Path.Combine(output, "Duplicates");
        var othersDir = Path.Combine(output, "Outros");
        _fs.CreateDirectory(duplicatesDir);
        _fs.CreateDirectory(othersDir);

        var config = OrganizerConfigLoader.Load(request.ConfigPath, output);
        var minScore = request.MinScore ?? config.MinScoreDefault;

        var allowed = new HashSet<string>(
            config.AllowedExtensions.Select(e => e.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);

        var plan = new List<OrganizerPlanItem>();
        var hashMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lineMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var eligibleCount = 0;

        var files = Directory.EnumerateFiles(inbox, "*", SearchOption.AllDirectories).ToList();
        var idx = 0;

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            idx++;

            if (idx % 100 == 0)
                progress?.Report(new ProgressEvent($"Processed {idx}/{files.Count}", (int)(idx * 100.0 / Math.Max(1, files.Count)), "scan"));

            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                plan.Add(new OrganizerPlanItem(file, file, "IGNORED", "Extensão não permitida", OrganizerAction.Ignored));
                continue;
            }

            eligibleCount++;

            var fileName = Path.GetFileName(file);
            var nameKey = BuildNameKey(fileName);

            if (config.DeduplicateByName && !string.IsNullOrWhiteSpace(nameKey) && nameMap.ContainsKey(nameKey))
            {
                plan.Add(new OrganizerPlanItem(
                    file,
                    UniqueTarget(duplicatesDir, fileName),
                    "DUPLICATE",
                    "Nome igual (dedup por nome)",
                    OrganizerAction.Duplicate));
                continue;
            }

            string? hash = null;
            if (config.DeduplicateByHash)
            {
                try
                {
                    hash = ComputeHash(file);
                }
                catch (Exception ex)
                {
                    plan.Add(new OrganizerPlanItem(file, file, "ERROR", $"Erro hash: {ex.Message}", OrganizerAction.Error));
                    continue;
                }

                if (hashMap.ContainsKey(hash))
                {
                    plan.Add(new OrganizerPlanItem(
                        file,
                        UniqueTarget(duplicatesDir, fileName),
                        "DUPLICATE",
                        "Hash igual (arquivo idêntico)",
                        OrganizerAction.Duplicate));
                    continue;
                }
            }

            var text = _extractor.Extract(file);
            var lineKey = BuildFirstLinesKey(text, config.DeduplicateFirstLines);
            if (!string.IsNullOrWhiteSpace(lineKey) && lineMap.ContainsKey(lineKey))
            {
                plan.Add(new OrganizerPlanItem(
                    file,
                    UniqueTarget(duplicatesDir, fileName),
                    "DUPLICATE",
                    "Primeiras linhas iguais (dedup por cabecalho)",
                    OrganizerAction.Duplicate));
                continue;
            }

            if (config.DeduplicateByName && !string.IsNullOrWhiteSpace(nameKey))
                nameMap[nameKey] = file;
            if (config.DeduplicateByHash && !string.IsNullOrWhiteSpace(hash))
                hashMap[hash] = file;
            if (!string.IsNullOrWhiteSpace(lineKey))
                lineMap[lineKey] = file;

            var (category, score, reason) = Classify(file, text, config, minScore);

            if (category is null || score < minScore)
            {
                plan.Add(new OrganizerPlanItem(
                    file,
                    UniqueTarget(othersDir, Path.GetFileName(file)),
                    "Outros",
                    $"Score baixo ({score}). {reason}",
                    OrganizerAction.WouldMove));
                continue;
            }

            var catDir = Path.Combine(output, category.Folder);
            _fs.CreateDirectory(catDir);

            plan.Add(new OrganizerPlanItem(
                file,
                UniqueTarget(catDir, Path.GetFileName(file)),
                category.Name,
                reason,
                OrganizerAction.WouldMove));
        }

        if (request.Apply)
            Apply(plan);

        var stats = new OrganizerStats(
            plan.Count,
            eligibleCount,
            plan.Count(p => p.Action == OrganizerAction.WouldMove || p.Action == OrganizerAction.Moved),
            plan.Count(p => p.Action == OrganizerAction.Duplicate),
            plan.Count(p => p.Action == OrganizerAction.Ignored),
            plan.Count(p => p.Action == OrganizerAction.Error));

        var response = new OrganizerResponse(inbox, output, stats, plan);
        return Task.FromResult(RunResult<OrganizerResponse>.Success(response));
    }

    private static (OrganizerCategory? cat, int score, string reason) Classify(
        string filePath,
        string extracted,
        OrganizerConfig config,
        int defaultMinScore)
    {
        var fileName = Path.GetFileName(filePath);
        var normalizedText = TextNormalizer.Normalize(extracted);
        var normalizedName = TextNormalizer.Normalize(fileName);

        OrganizerCategory? best = null;
        var bestScore = int.MinValue;
        var bestReason = "Sem match";

        foreach (var cat in config.Categories)
        {
            var score = 0;
            var hits = new List<string>();
            var negHits = new List<string>();

            ScoreKeywords(normalizedText, cat.Keywords, cat.KeywordWeight, hits, ref score);
            ScoreKeywords(normalizedName, cat.Keywords, (int)Math.Round(cat.KeywordWeight * config.FileNameWeight), hits, ref score);

            ScoreKeywords(normalizedText, cat.NegativeKeywords, -cat.NegativeWeight, negHits, ref score);
            ScoreKeywords(normalizedName, cat.NegativeKeywords, -(int)Math.Round(cat.NegativeWeight * config.FileNameWeight), negHits, ref score);

            var threshold = cat.MinScore ?? defaultMinScore;
            if (score >= threshold && score > bestScore)
            {
                bestScore = score;
                best = cat;
                bestReason = BuildReason(score, hits, negHits);
            }
        }

        if (best is null)
            return (null, bestScore == int.MinValue ? 0 : bestScore, bestReason);

        return (best, bestScore, bestReason);
    }

    private static void ScoreKeywords(
        string hay,
        string[] keywords,
        int weight,
        List<string> hits,
        ref int score)
    {
        if (keywords.Length == 0 || string.IsNullOrWhiteSpace(hay))
            return;

        foreach (var kw in keywords)
        {
            if (string.IsNullOrWhiteSpace(kw)) continue;

            if (kw.StartsWith("re:", StringComparison.OrdinalIgnoreCase))
            {
                var pattern = kw[3..];
                if (Regex.IsMatch(hay, pattern, RegexOptions.IgnoreCase))
                {
                    score += weight;
                    hits.Add(kw);
                }
                continue;
            }

            if (hay.Contains(TextNormalizer.Normalize(kw)))
            {
                score += weight;
                hits.Add(kw);
            }
        }
    }

    private static string BuildReason(int score, List<string> hits, List<string> negHits)
    {
        var h = hits.Count == 0 ? "Sem match" : $"Hits: {string.Join(", ", hits.Take(8))}";
        var n = negHits.Count == 0 ? "" : $" | Neg: {string.Join(", ", negHits.Take(6))}";
        return $"{h}{n} | Score={score}";
    }

    private static void Apply(List<OrganizerPlanItem> plan)
    {
        for (int i = 0; i < plan.Count; i++)
        {
            var item = plan[i];
            if (item.Action is OrganizerAction.Ignored or OrganizerAction.Error)
                continue;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(item.Target)!);
                if (!File.Exists(item.Source))
                {
                    plan[i] = item with { Action = OrganizerAction.Error, Reason = item.Reason + " | Source missing" };
                    continue;
                }

                File.Move(item.Source, item.Target);
                if (item.Action == OrganizerAction.Duplicate)
                    plan[i] = item;
                else
                    plan[i] = item with { Action = OrganizerAction.Moved };
            }
            catch (Exception ex)
            {
                plan[i] = item with { Action = OrganizerAction.Error, Reason = item.Reason + " | Move failed: " + ex.Message };
            }
        }
    }

    private static string UniqueTarget(string dir, string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var candidate = Path.Combine(dir, fileName);
        if (!File.Exists(candidate)) return candidate;

        for (int i = 1; i <= 9999; i++)
        {
            var alt = Path.Combine(dir, $"{baseName} ({i}){ext}");
            if (!File.Exists(alt)) return alt;
        }

        return Path.Combine(dir, $"{baseName} ({Guid.NewGuid():N}){ext}");
    }

    private static string BuildNameKey(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var normalized = TextNormalizer.Normalize(baseName);
        if (!string.IsNullOrWhiteSpace(normalized))
            return normalized;

        return TextNormalizer.Normalize(fileName);
    }

    private static string BuildFirstLinesKey(string extracted, int lineCount)
    {
        if (lineCount <= 0 || string.IsNullOrWhiteSpace(extracted))
            return string.Empty;

        var sb = new StringBuilder();
        using var reader = new StringReader(extracted);
        string? line;
        var count = 0;

        while (count < lineCount && (line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (sb.Length > 0)
                sb.Append('\n');

            sb.Append(TextNormalizer.Normalize(trimmed));
            count++;
        }

        return sb.ToString();
    }

    private static string ComputeHash(string file)
    {
        using var sha = SHA256.Create();
        using var fs = File.OpenRead(file);
        return Convert.ToHexString(sha.ComputeHash(fs));
    }
}
