using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Utf8Convert.Abstractions;
using DevTools.Utf8Convert.Models;
using DevTools.Utf8Convert.Providers;
using DevTools.Core.Utilities;
using DevTools.Utf8Convert.Validation;

namespace DevTools.Utf8Convert.Engine;

public sealed class Utf8ConvertEngine : IDevToolEngine<Utf8ConvertRequest, Utf8ConvertResponse>
{
    private readonly IUtf8FileSystem _fs;

    public Utf8ConvertEngine(IUtf8FileSystem? fileSystem = null)
    {
        _fs = fileSystem ?? new SystemUtf8FileSystem();
    }

    public async Task<RunResult<Utf8ConvertResponse>> ExecuteAsync(
        Utf8ConvertRequest request,
        IProgressReporter? progress = null,
        CancellationToken ct = default)
    {
        var errors = Utf8ConvertRequestValidator.Validate(request);
        if (errors.Count > 0)
            return RunResult<Utf8ConvertResponse>.Fail(errors);

        var rootPath = Path.GetFullPath(request.RootPath);
        var filter = new PathFilter(request.IncludeGlobs, request.ExcludeGlobs);

        var items = new List<Utf8ConvertItem>();
        var scanned = 0;
        var converted = 0;
        var already = 0;
        var skippedBinary = 0;
        var skippedExcluded = 0;
        var failed = 0;

        var files = _fs.EnumerateFiles(rootPath, request.Recursive).ToList();
        var total = files.Count;
        var step = 0;

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            step++;
            progress?.Report(new ProgressEvent("Processing file", Percent(step, total), "file"));

            var relative = Normalize(Path.GetRelativePath(rootPath, file));
            if (filter.IsExcluded(relative) || !filter.IsIncluded(relative))
            {
                skippedExcluded++;
                items.Add(new Utf8ConvertItem(file, Utf8ConvertStatus.SkippedExcluded, null, null, null));
                continue;
            }

            scanned++;

            try
            {
                var detected = await TextFileHelper.ReadAsync(_fs, file, ct).ConfigureAwait(false);

                if (detected.IsBinary)
                {
                    skippedBinary++;
                    items.Add(new Utf8ConvertItem(file, Utf8ConvertStatus.SkippedBinary, detected.DetectedName, null, null));
                    continue;
                }

                var detectedName = detected.DetectedName ?? detected.Encoding.WebName;
                var outputEncodingName = request.OutputBom ? "utf-8-bom" : "utf-8";

                var needsConversion = NeedsConversion(detected, request.OutputBom);

                if (!needsConversion)
                {
                    already++;
                    items.Add(new Utf8ConvertItem(file, Utf8ConvertStatus.AlreadyUtf8, detectedName, outputEncodingName, null));
                    continue;
                }

                if (!request.DryRun)
                {
                    if (request.CreateBackup)
                    {
                        var backupPath = CreateBackupPath(file);
                        _fs.CopyFile(file, backupPath, overwrite: false);
                    }

                    await TextFileHelper.WriteUtf8Async(_fs, file, detected.Content, request.OutputBom, ct).ConfigureAwait(false);
                }

                converted++;
                items.Add(new Utf8ConvertItem(file, Utf8ConvertStatus.Converted, detectedName, outputEncodingName, null));
            }
            catch (Exception ex)
            {
                failed++;
                items.Add(new Utf8ConvertItem(file, Utf8ConvertStatus.Error, null, null, ex.Message));
            }
        }

        var summary = new Utf8ConvertSummary(scanned, converted, already, skippedBinary, skippedExcluded, failed);
        var response = new Utf8ConvertResponse(summary, items);

        if (failed > 0)
        {
            return new RunResult<Utf8ConvertResponse>
            {
                IsSuccess = false,
                Errors = new[] { new ErrorDetail("utf8.convert.failed", "One or more files failed to convert.") },
                Value = response
            };
        }

        return RunResult<Utf8ConvertResponse>.Success(response);
    }

    private static bool NeedsConversion(DetectedText detected, bool outputBom)
    {
        var isUtf8 = detected.Encoding.WebName.Equals("utf-8", StringComparison.OrdinalIgnoreCase);
        if (!isUtf8)
            return true;

        if (outputBom && !detected.HasBom)
            return true;

        if (!outputBom && detected.HasBom)
            return true;

        return false;
    }

    private static string CreateBackupPath(string file)
    {
        var basePath = file + ".bak";
        if (!File.Exists(basePath))
            return basePath;

        var index = 1;
        while (true)
        {
            var candidate = basePath + index;
            if (!File.Exists(candidate))
                return candidate;
            index++;
        }
    }

    private static int? Percent(int step, int total)
    {
        if (total <= 0) return null;
        return (int)Math.Round(step * 100d / total);
    }

    private static string Normalize(string path)
        => path.Replace('\\', '/');
}
