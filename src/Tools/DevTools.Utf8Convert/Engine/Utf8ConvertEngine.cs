using System.Text;
using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Core.Utilities;
using DevTools.Core.Validation;
using DevTools.Utf8Convert.Infrastructure;
using DevTools.Utf8Convert.Models;
using DevTools.Utf8Convert.Validators;

namespace DevTools.Utf8Convert.Engine;

public sealed class Utf8ConvertEngine : IDevToolEngine<Utf8ConvertRequest, Utf8ConvertResult>
{
    private readonly IValidator<Utf8ConvertRequest> _validator;

    public Utf8ConvertEngine(IValidator<Utf8ConvertRequest>? validator = null)
    {
        _validator = validator ?? new Utf8ConvertRequestValidator();
    }

    public async Task<RunResult<Utf8ConvertResult>> ExecuteAsync(
        Utf8ConvertRequest request,
        IProgressReporter? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            var errs = validation.Errors
                .Select(e => new ErrorDetail($"utf8.{e.Field}", e.Message))
                .ToList();
            return RunResult<Utf8ConvertResult>.Fail(errs);
        }

        if (!Directory.Exists(request.RootPath))
        {
            return RunResult<Utf8ConvertResult>.Fail(new ErrorDetail(
                "utf8.root.not_found",
                "Pasta raiz não encontrada.",
                Cause: request.RootPath));
        }

        var rootPath = Path.GetFullPath(request.RootPath);
        var filter = new PathFilter(request.IncludeGlobs, request.ExcludeGlobs);

        var items = new List<Utf8ConvertItem>();
        var scanned = 0;
        var converted = 0;
        var already = 0;
        var skippedBinary = 0;
        var skippedExcluded = 0;
        var failed = 0;

        var searchOption = request.Recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var files = Directory.EnumerateFiles(rootPath, "*", searchOption).ToList();
        var total = files.Count;
        var step = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            step++;
            progress?.Report(new ProgressEvent("Processando arquivo", Percent(step, total), "file"));

            var relative = Path.GetRelativePath(rootPath, file).Replace('\\', '/');
            if (filter.IsExcluded(relative) || !filter.IsIncluded(relative))
            {
                skippedExcluded++;
                items.Add(new Utf8ConvertItem(file, Utf8ConvertStatus.SkippedExcluded, null, null, null));
                continue;
            }

            scanned++;

            try
            {
                var bytes = await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false);
                var detected = TextEncodingDetector.Detect(bytes);

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
                        File.Copy(file, backupPath, overwrite: false);
                    }

                    var encoding = new UTF8Encoding(request.OutputBom, true);
                    var payload = encoding.GetBytes(detected.Content);
                    await File.WriteAllBytesAsync(file, payload, cancellationToken).ConfigureAwait(false);
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
        var result = new Utf8ConvertResult(summary, items);

        if (failed > 0)
        {
            return new RunResult<Utf8ConvertResult>
            {
                IsSuccess = false,
                Errors = [new ErrorDetail("utf8.convert.partial_failure", $"{failed} arquivo(s) falharam na conversão.")],
                Value = result
            };
        }

        return RunResult<Utf8ConvertResult>.Success(result);
    }

    private static bool NeedsConversion(DetectedText detected, bool outputBom)
    {
        var isUtf8 = detected.Encoding.WebName.Equals("utf-8", StringComparison.OrdinalIgnoreCase);
        if (!isUtf8) return true;
        if (outputBom && !detected.HasBom) return true;
        if (!outputBom && detected.HasBom) return true;
        return false;
    }

    private static string CreateBackupPath(string file)
    {
        var basePath = file + ".bak";
        if (!File.Exists(basePath)) return basePath;
        var index = 1;
        while (true)
        {
            var candidate = basePath + index;
            if (!File.Exists(candidate)) return candidate;
            index++;
        }
    }

    private static int? Percent(int step, int total)
    {
        if (total <= 0) return null;
        return (int)Math.Round(step * 100d / total);
    }
}
