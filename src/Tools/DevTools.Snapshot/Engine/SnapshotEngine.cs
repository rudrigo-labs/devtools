using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Snapshot.Models;
using DevTools.Core.Providers;
using DevTools.Snapshot.Validation;

namespace DevTools.Snapshot.Engine;

public sealed class SnapshotEngine : IDevToolEngine<SnapshotRequest, SnapshotResponse>
{
    private readonly IFileSystem _fs;
    private readonly SnapshotTextWriter _textWriter = new();
    private readonly SnapshotNestedJsonWriter _nestedWriter = new();
    private readonly SnapshotRecursiveJsonWriter _recursiveWriter = new();
    private readonly SnapshotHtmlWriter _htmlWriter = new();

    public SnapshotEngine(IFileSystem? fileSystem = null)
    {
        _fs = fileSystem ?? new SystemFileSystem();
    }

    public async Task<RunResult<SnapshotResponse>> ExecuteAsync(
        SnapshotRequest request,
        IProgressReporter? progress = null,
        CancellationToken ct = default)
    {
        var errors = SnapshotRequestValidator.Validate(request, _fs);
        if (errors.Count > 0)
            return RunResult<SnapshotResponse>.Fail(errors);

        var rootPath = Path.GetFullPath(request.RootPath);
        var outputBase = string.IsNullOrWhiteSpace(request.OutputBasePath)
            ? Path.Combine(rootPath, "Snapshot")
            : Path.GetFullPath(request.OutputBasePath);

        _fs.CreateDirectory(outputBase);

        var ignoreDirs = BuildIgnoreSet(request, outputBase);

        progress?.Report(new ProgressEvent("Scanning", 10, "scan"));
        var stats = CountStats(rootPath, ignoreDirs, ct);

        var artifacts = new List<SnapshotArtifact>();

        if (request.GenerateText)
        {
            progress?.Report(new ProgressEvent("Writing text snapshot", 30, "text"));
            var outFile = Path.Combine(outputBase, "Text", "snapshot.txt");
            await _textWriter.WriteAsync(rootPath, outFile, ignoreDirs).ConfigureAwait(false);
            artifacts.Add(new SnapshotArtifact(SnapshotArtifactKind.Text, outFile));
        }

        if (request.GenerateJsonNested)
        {
            progress?.Report(new ProgressEvent("Writing nested JSON snapshot", 50, "json"));
            var outFile = Path.Combine(outputBase, "JsonNested", "snapshot-nested.json");
            await _nestedWriter.WriteAsync(rootPath, outFile, ignoreDirs).ConfigureAwait(false);
            artifacts.Add(new SnapshotArtifact(SnapshotArtifactKind.JsonNested, outFile));
        }

        if (request.GenerateJsonRecursive)
        {
            progress?.Report(new ProgressEvent("Writing recursive JSON snapshot", 70, "json"));
            var outFile = Path.Combine(outputBase, "JsonRecursive", "snapshot-recursive.json");
            await _recursiveWriter.WriteAsync(rootPath, outFile, ignoreDirs).ConfigureAwait(false);
            artifacts.Add(new SnapshotArtifact(SnapshotArtifactKind.JsonRecursive, outFile));
        }

        if (request.GenerateHtmlPreview)
        {
            progress?.Report(new ProgressEvent("Writing HTML preview", 85, "html"));
            var outDir = Path.Combine(outputBase, "WebPreview");
            long? maxBytes = request.MaxFileSizeKb.HasValue
                ? request.MaxFileSizeKb.Value * 1024L
                : null;

            await _htmlWriter.WriteAsync(
                Path.GetFileName(rootPath.TrimEnd(Path.DirectorySeparatorChar)),
                rootPath,
                outDir,
                ignoreDirs,
                maxBytes).ConfigureAwait(false);

            artifacts.Add(new SnapshotArtifact(SnapshotArtifactKind.HtmlPreview, outDir));
        }

        progress?.Report(new ProgressEvent("Done", 100, "done"));

        var response = new SnapshotResponse(rootPath, outputBase, stats, artifacts);
        return RunResult<SnapshotResponse>.Success(response);
    }

    private static SnapshotStats CountStats(string rootPath, IReadOnlySet<string> ignoreDirs, CancellationToken ct)
    {
        var fileCount = 0;
        var dirCount = 0;

        var stack = new Stack<string>();
        stack.Push(rootPath);

        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var dir = stack.Pop();
            dirCount++;

            IEnumerable<string> subDirs;
            IEnumerable<string> files;

            try
            {
                subDirs = Directory.EnumerateDirectories(dir);
                files = Directory.EnumerateFiles(dir);
            }
            catch
            {
                continue;
            }

            foreach (var sd in subDirs)
            {
                var name = Path.GetFileName(sd);
                if (ignoreDirs.Contains(name))
                    continue;
                stack.Push(sd);
            }

            foreach (var f in files)
            {
                var name = Path.GetFileName(f);
                if (ignoreDirs.Contains(name))
                    continue;
                fileCount++;
            }
        }

        return new SnapshotStats(fileCount, Math.Max(0, dirCount - 1));
    }

    private static IReadOnlySet<string> BuildIgnoreSet(SnapshotRequest request, string outputBase)
    {
        var ignore = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (request.IgnoredDirectories is { Count: > 0 })
        {
            foreach (var dir in request.IgnoredDirectories)
            {
                if (!string.IsNullOrWhiteSpace(dir))
                    ignore.Add(dir.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
        }
        else
        {
            foreach (var dir in SnapshotDefaults.IgnoredDirectories)
                ignore.Add(dir);
        }

        var outputDirName = new DirectoryInfo(outputBase).Name;
        if (!string.IsNullOrWhiteSpace(outputDirName))
            ignore.Add(outputDirName);

        return ignore;
    }
}
