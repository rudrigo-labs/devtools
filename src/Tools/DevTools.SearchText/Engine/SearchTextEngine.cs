using System.Text;
using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Core.Utilities;
using DevTools.Core.Validation;
using DevTools.SearchText.Models;
using DevTools.SearchText.Validators;

namespace DevTools.SearchText.Engine;

public sealed class SearchTextEngine : IDevToolEngine<SearchTextRequest, SearchTextResult>
{
    private static readonly UTF8Encoding Utf8Strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private const int SampleBytes = 4096;

    private readonly IValidator<SearchTextRequest> _validator;

    public SearchTextEngine(IValidator<SearchTextRequest>? validator = null)
    {
        _validator = validator ?? new SearchTextRequestValidator();
    }

    public async Task<RunResult<SearchTextResult>> ExecuteAsync(
        SearchTextRequest request,
        IProgressReporter? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(e => new ErrorDetail($"searchtext.{e.Field}", e.Message))
                .ToList();
            return RunResult<SearchTextResult>.Fail(errors);
        }

        if (!Directory.Exists(request.RootPath))
        {
            return RunResult<SearchTextResult>.Fail(new ErrorDetail(
                "searchtext.root.not_found",
                "Pasta raiz não encontrada.",
                Cause: request.RootPath));
        }

        var exclude = request.ExcludeGlobs?.Where(g => !string.IsNullOrWhiteSpace(g)).ToList()
            ?? new List<string>();
        if (exclude.Count == 0)
            exclude = SearchTextDefaults.DefaultExcludeGlobs.ToList();

        var include = request.IncludeGlobs?.Where(g => !string.IsNullOrWhiteSpace(g)).ToList()
            ?? new List<string>();

        var matcher = new TextMatcher(request.Pattern, request.UseRegex, request.CaseSensitive, request.WholeWord);
        var root = Path.GetFullPath(request.RootPath);
        var results = new List<SearchTextFileMatch>();
        var totalOccurrences = 0;
        var totalScanned = 0;

        progress?.Report(new ProgressEvent("Buscando arquivos", 5, "scan"));

        var idx = 0;
        foreach (var file in EnumerateFiles(root, include, exclude, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            idx++;
            totalScanned++;

            if (idx % 100 == 0)
                progress?.Report(new ProgressEvent($"Processados {idx} arquivos", null, "scan"));

            if (request.MaxFileSizeKb.HasValue)
            {
                try
                {
                    var info = new FileInfo(file);
                    if (info.Length > request.MaxFileSizeKb.Value * 1024L)
                        continue;
                }
                catch { continue; }
            }

            var match = await SearchFileAsync(file, root, matcher, request, cancellationToken)
                .ConfigureAwait(false);
            if (match is null) continue;

            results.Add(match);
            totalOccurrences += match.Occurrences;
        }

        progress?.Report(new ProgressEvent("Concluído", 100, "done"));

        var result = new SearchTextResult(root, totalScanned, results.Count, totalOccurrences, results);
        return RunResult<SearchTextResult>.Success(result);
    }

    private static async Task<SearchTextFileMatch?> SearchFileAsync(
        string fullPath,
        string root,
        TextMatcher matcher,
        SearchTextRequest request,
        CancellationToken ct)
    {
        using var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var sample = ReadSample(stream, SampleBytes);
        var detection = DetectEncoding(sample);

        if (request.SkipBinaryFiles && detection.IsBinary)
            return null;

        stream.Position = 0;
        using var reader = new StreamReader(stream, detection.Encoding, detectEncodingFromByteOrderMarks: true);

        var lineNo = 0;
        var occurrences = 0;
        var lineMatches = new List<SearchTextLineMatch>();

        string? line;
        while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) is not null)
        {
            lineNo++;
            var cols = new List<int>();

            foreach (var (idx, _) in matcher.FindMatches(line))
            {
                cols.Add(idx + 1);
                occurrences++;

                if (request.MaxMatchesPerFile > 0 && occurrences >= request.MaxMatchesPerFile)
                    break;
            }

            if (cols.Count > 0 && request.ReturnLines)
                lineMatches.Add(new SearchTextLineMatch(lineNo, line, cols));

            if (request.MaxMatchesPerFile > 0 && occurrences >= request.MaxMatchesPerFile)
                break;
        }

        if (occurrences == 0)
            return null;

        return new SearchTextFileMatch(
            fullPath,
            Path.GetRelativePath(root, fullPath).Replace('\\', '/'),
            occurrences,
            lineMatches);
    }

    private static IEnumerable<string> EnumerateFiles(
        string root,
        IReadOnlyList<string> include,
        IReadOnlyList<string> exclude,
        CancellationToken ct)
    {
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var dir = stack.Pop();
            IEnumerable<string> subDirs = Array.Empty<string>();
            IEnumerable<string> files = Array.Empty<string>();

            try
            {
                subDirs = Directory.EnumerateDirectories(dir);
                files = Directory.EnumerateFiles(dir);
            }
            catch { continue; }

            foreach (var sd in subDirs)
            {
                var rel = Path.GetRelativePath(root, sd).Replace('\\', '/') + "/";
                if (IsExcluded(rel, include, exclude)) continue;
                stack.Push(sd);
            }

            foreach (var file in files)
            {
                var rel = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (IsExcluded(rel, include, exclude)) continue;
                yield return file;
            }
        }
    }

    private static bool IsExcluded(
        string relativePath,
        IReadOnlyList<string> include,
        IReadOnlyList<string> exclude)
    {
        if (exclude.Count > 0 && exclude.Any(g => GlobMatcher.IsMatch(relativePath, g)))
            return true;

        if (include.Count == 0)
            return false;

        return !include.Any(g => GlobMatcher.IsMatch(relativePath, g));
    }

    private static EncodingDetection DetectEncoding(byte[] bytes)
    {
        if (bytes.Length == 0)
            return new EncodingDetection(new UTF8Encoding(false), false);

        var bom = GetBomEncoding(bytes, out _);
        if (bom is not null)
            return new EncodingDetection(bom, false);

        if (TryDetectUtf32(bytes, out var utf32))
            return new EncodingDetection(utf32, false);

        if (TryDetectUtf16(bytes, out var utf16))
            return new EncodingDetection(utf16, false);

        if (TryDecodeUtf8(bytes))
            return new EncodingDetection(new UTF8Encoding(false), false);

        if (LooksBinary(bytes))
            return new EncodingDetection(Encoding.UTF8, true);

        return new EncodingDetection(Encoding.Latin1, false);
    }

    private static byte[] ReadSample(Stream stream, int maxBytes)
    {
        var buffer = new byte[Math.Max(1, maxBytes)];
        var read = stream.Read(buffer, 0, buffer.Length);
        if (read == buffer.Length) return buffer;
        var trimmed = new byte[read];
        Array.Copy(buffer, trimmed, read);
        return trimmed;
    }

    private static Encoding? GetBomEncoding(byte[] bytes, out int bomLength)
    {
        bomLength = 0;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        { bomLength = 3; return new UTF8Encoding(true); }
        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
        { bomLength = 4; return new UTF32Encoding(false, true); }
        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
        { bomLength = 4; return new UTF32Encoding(true, true); }
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        { bomLength = 2; return new UnicodeEncoding(false, true); }
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        { bomLength = 2; return new UnicodeEncoding(true, true); }
        return null;
    }

    private static bool TryDecodeUtf8(byte[] bytes)
    {
        try { _ = Utf8Strict.GetString(bytes); return true; }
        catch { return false; }
    }

    private static bool TryDetectUtf16(byte[] bytes, out Encoding encoding)
    {
        encoding = new UnicodeEncoding(false, false);
        if (!bytes.Any(b => b == 0)) return false;

        var sample = bytes.Length > SampleBytes ? bytes.AsSpan(0, SampleBytes) : bytes.AsSpan();
        int evenZero = 0, oddZero = 0, evenCount = 0, oddCount = 0;
        for (int i = 0; i < sample.Length; i++)
        {
            if (i % 2 == 0) { evenCount++; if (sample[i] == 0) evenZero++; }
            else { oddCount++; if (sample[i] == 0) oddZero++; }
        }
        if (evenCount == 0 || oddCount == 0) return false;
        var evenRatio = evenZero / (double)evenCount;
        var oddRatio = oddZero / (double)oddCount;
        if (oddRatio > 0.6 && evenRatio < 0.2) { encoding = new UnicodeEncoding(false, false); return true; }
        if (evenRatio > 0.6 && oddRatio < 0.2) { encoding = new UnicodeEncoding(true, false); return true; }
        return false;
    }

    private static bool TryDetectUtf32(byte[] bytes, out Encoding encoding)
    {
        encoding = new UTF32Encoding(false, false);
        if (bytes.Length < 4 || !bytes.Any(b => b == 0)) return false;

        var sample = bytes.Length > SampleBytes ? bytes.AsSpan(0, SampleBytes) : bytes.AsSpan();
        int m0 = 0, m1 = 0, m2 = 0, m3 = 0, c0 = 0, c1 = 0, c2 = 0, c3 = 0;
        for (int i = 0; i < sample.Length; i++)
        {
            switch (i % 4)
            {
                case 0: c0++; if (sample[i] == 0) m0++; break;
                case 1: c1++; if (sample[i] == 0) m1++; break;
                case 2: c2++; if (sample[i] == 0) m2++; break;
                case 3: c3++; if (sample[i] == 0) m3++; break;
            }
        }
        if (c0 == 0 || c1 == 0 || c2 == 0 || c3 == 0) return false;
        if ((m1 + m2 + m3) / (double)(c1 + c2 + c3) > 0.7 && m0 / (double)c0 < 0.2)
        { encoding = new UTF32Encoding(false, false); return true; }
        if ((m0 + m1 + m2) / (double)(c0 + c1 + c2) > 0.7 && m3 / (double)c3 < 0.2)
        { encoding = new UTF32Encoding(true, false); return true; }
        return false;
    }

    private static bool LooksBinary(byte[] bytes)
    {
        var sample = bytes.Length > SampleBytes ? bytes.AsSpan(0, SampleBytes) : bytes.AsSpan();
        if (sample.Length == 0) return false;
        var ctrl = 0;
        foreach (var b in sample)
        {
            if (b == 0) return true;
            if (b < 9 || (b > 13 && b < 32)) ctrl++;
        }
        return ctrl / (double)sample.Length > 0.3;
    }

    private sealed record EncodingDetection(Encoding Encoding, bool IsBinary);
}
