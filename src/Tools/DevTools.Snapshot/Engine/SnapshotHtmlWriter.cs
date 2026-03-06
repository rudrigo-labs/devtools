using System.Net;
using System.Text;

namespace DevTools.Snapshot.Engine;

internal sealed class SnapshotHtmlWriter
{
    private const string PrismBase = "https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0";
    private const string PrismThemeUrl = PrismBase + "/themes/prism-tomorrow.min.css";
    private const string PrismCoreUrl = PrismBase + "/prism.min.js";

    private static readonly Dictionary<string, string> PrismByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = "csharp",
        [".csx"] = "csharp",
        [".json"] = "json",
        [".yml"] = "yaml",
        [".yaml"] = "yaml",
        [".md"] = "markdown",
        [".xml"] = "markup",
        [".config"] = "markup",
        [".csproj"] = "markup",
        [".html"] = "markup",
        [".htm"] = "markup",
        [".cshtml"] = "markup",
        [".razor"] = "markup",
        [".css"] = "css",
        [".js"] = "javascript",
        [".mjs"] = "javascript",
        [".cjs"] = "javascript",
        [".ts"] = "typescript",
        [".tsx"] = "tsx",
        [".jsx"] = "jsx",
        [".sql"] = "sql",
        [".ps1"] = "powershell",
        [".sh"] = "bash",
        [".bat"] = "batch",
        [".graphql"] = "graphql",
        [".proto"] = "protobuf"
    };

    private static readonly Dictionary<string, string[]> PrismDeps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["typescript"] = new[] { "javascript" },
        ["tsx"] = new[] { "typescript", "jsx", "javascript" },
        ["jsx"] = new[] { "javascript" },
        ["graphql"] = new[] { "javascript" }
    };

    private IReadOnlyList<string> _prismScripts = Array.Empty<string>();
    private string? _firstFileFallback;
    private string? _programCsFile;
    private string? _readmeMdFile;

    public async Task WriteAsync(
        string projectName,
        string rootPath,
        string outDir,
        IReadOnlySet<string> ignoreDirs,
        long? maxFileSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("rootPath is required.", nameof(rootPath));

        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"RootPath not found: {rootPath}");

        if (string.IsNullOrWhiteSpace(outDir))
            throw new ArgumentException("outDir is required.", nameof(outDir));

        var ignore = new HashSet<string>(ignoreDirs, StringComparer.OrdinalIgnoreCase)
        {
            "Snapshot",
            new DirectoryInfo(outDir).Name
        };

        var filesDir = Path.Combine(outDir, "source_preview");

        if (Directory.Exists(outDir))
            Directory.Delete(outDir, true);

        Directory.CreateDirectory(filesDir);

        var neededPrism = DetectNeededPrismComponents(rootPath);
        ResolvePrismDependencies(neededPrism);
        var availablePrism = await EnsurePrismFilesAsync(outDir, neededPrism);

        _prismScripts = availablePrism
            .Select(x => $"../prism-{x}.min.js")
            .ToList();

        _firstFileFallback = null;
        _programCsFile = null;
        _readmeMdFile = null;

        var sbIndex = new StringBuilder();
        BuildHtmlHeader(sbIndex, projectName);

        var root = new DirectoryInfo(rootPath);
        await ProcessDirectoryAsync(root, rootPath, filesDir, sbIndex, ignore, maxFileSizeBytes);

        var initialSrc = _readmeMdFile ?? _programCsFile ?? _firstFileFallback ?? string.Empty;

        sbIndex.Append($"</div><div class='content'><iframe name='viewer' src='{WebUtility.HtmlEncode(initialSrc)}'></iframe></div></body></html>");

        await File.WriteAllTextAsync(Path.Combine(outDir, "index.html"), sbIndex.ToString(), Encoding.UTF8);
    }

    private async Task ProcessDirectoryAsync(
        DirectoryInfo dir,
        string rootPath,
        string filesDir,
        StringBuilder sb,
        IReadOnlySet<string> ignore,
        long? maxFileSizeBytes)
    {
        var entries = dir.GetFileSystemInfos()
            .Where(e => !ignore.Contains(e.Name))
            .OrderBy(e => e is FileInfo ? 1 : 0)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);

        sb.Append("<ul>");

        foreach (var entry in entries)
        {
            if (entry is DirectoryInfo subDir)
            {
                sb.Append($"<li><details><summary>📁 {WebUtility.HtmlEncode(subDir.Name)}</summary>");
                await ProcessDirectoryAsync(subDir, rootPath, filesDir, sb, ignore, maxFileSizeBytes);
                sb.Append("</details></li>");
                continue;
            }

            if (entry is FileInfo file && IsPreviewable(file, maxFileSizeBytes))
            {
                var relativePath = Path.GetRelativePath(rootPath, file.FullName);
                var htmlFileName = relativePath
                    .Replace(Path.DirectorySeparatorChar, '_')
                    .Replace(Path.AltDirectorySeparatorChar, '_')
                    + ".html";

                await GenerateCodeHtmlPageAsync(file, Path.Combine(filesDir, htmlFileName));

                var viewerHref = $"source_preview/{htmlFileName}";

                _firstFileFallback ??= viewerHref;

                if (_readmeMdFile == null && file.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase))
                    _readmeMdFile = viewerHref;
                else if (_programCsFile == null && file.Name.Equals("Program.cs", StringComparison.OrdinalIgnoreCase))
                    _programCsFile = viewerHref;

                sb.Append($"<li><a href='{WebUtility.HtmlEncode(viewerHref)}' target='viewer'>📄 {WebUtility.HtmlEncode(file.Name)}</a></li>");
            }
            else if (entry is FileInfo f2 && SnapshotDefaults.ImageExtensions.Contains(f2.Extension))
            {
                sb.Append($"<li><span style='opacity:.7'>🖼️ {WebUtility.HtmlEncode(f2.Name)} (image)</span></li>");
            }
        }

        sb.Append("</ul>");
    }

    private static bool IsPreviewable(FileInfo file, long? maxFileSizeBytes)
    {
        if (SnapshotDefaults.ImageExtensions.Contains(file.Extension))
            return false;

        if (maxFileSizeBytes.HasValue && file.Length > maxFileSizeBytes.Value)
            return false;

        if (string.IsNullOrEmpty(file.Extension))
            return SnapshotDefaults.TextFilesWithoutExtension.Contains(file.Name);

        return SnapshotDefaults.PreviewExtensions.Contains(file.Extension);
    }

    private async Task GenerateCodeHtmlPageAsync(FileInfo file, string targetPath)
    {
        var code = await File.ReadAllTextAsync(file.FullName);
        var langClass = GetLanguageClass(file);

        var scripts = new StringBuilder();
        scripts.AppendLine("<script src='../prism.min.js'></script>");
        foreach (var s in _prismScripts)
            scripts.AppendLine($"<script src='{s}'></script>");

        var html = $@"<html><head><meta charset='UTF-8'>
<link rel='stylesheet' href='../prism-tomorrow.min.css'>
<style>
body {{ background: #1d1d1d; margin: 0; padding: 10px; font-size: 14px; }}
pre {{ border-radius: 6px; }}
</style>
</head><body>
<pre><code class='{langClass}'>{WebUtility.HtmlEncode(code)}</code></pre>
{scripts}
</body></html>";

        await File.WriteAllTextAsync(targetPath, html, Encoding.UTF8);
    }

    private static string GetLanguageClass(FileInfo file)
    {
        if (string.IsNullOrEmpty(file.Extension))
            return "language-none";

        if (PrismByExtension.TryGetValue(file.Extension, out var prismComponent))
            return $"language-{prismComponent}";

        return "language-none";
    }

    private static HashSet<string> DetectNeededPrismComponents(string rootPath)
    {
        var needed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "csharp",
            "json",
            "markup",
            "markdown",
            "yaml"
        };

        foreach (var file in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file);
            if (string.IsNullOrEmpty(ext))
                continue;

            if (PrismByExtension.TryGetValue(ext, out var component))
                needed.Add(component);
        }

        return needed;
    }

    private static void ResolvePrismDependencies(HashSet<string> needed)
    {
        bool changed;
        do
        {
            changed = false;
            foreach (var c in needed.ToList())
            {
                if (!PrismDeps.TryGetValue(c, out var deps))
                    continue;

                foreach (var dep in deps)
                {
                    if (needed.Add(dep))
                        changed = true;
                }
            }
        } while (changed);
    }

    private async Task<IReadOnlyList<string>> EnsurePrismFilesAsync(string targetDir, IReadOnlySet<string> components)
    {
        Directory.CreateDirectory(targetDir);
        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevTools",
            "Snapshot",
            "PrismCache",
            "1.29.0");
        Directory.CreateDirectory(cacheDir);

        var availableComponents = new List<string>();
        var fallbackCss = "/* offline fallback */ body{background:#1d1d1d;color:#ccc;} pre{background:#1e1e1e;}";
        var fallbackJs = "window.Prism=window.Prism||{};window.Prism.highlightAll=window.Prism.highlightAll||function(){};";
        var fallbackComponentJs = "window.Prism=window.Prism||{};";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SnapshotGenerator/1.0");

        var themeCache = Path.Combine(cacheDir, "prism-tomorrow.min.css");
        var coreCache = Path.Combine(cacheDir, "prism.min.js");

        await EnsureAssetCachedAsync(client, PrismThemeUrl, themeCache, fallbackCss);
        await EnsureAssetCachedAsync(client, PrismCoreUrl, coreCache, fallbackJs);

        File.Copy(themeCache, Path.Combine(targetDir, "prism-tomorrow.min.css"), true);
        File.Copy(coreCache, Path.Combine(targetDir, "prism.min.js"), true);

        foreach (var component in components.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var fileName = $"prism-{component}.min.js";
            var sourceUrl = $"{PrismBase}/components/{fileName}";
            var componentCache = Path.Combine(cacheDir, fileName);
            await EnsureAssetCachedAsync(client, sourceUrl, componentCache, fallbackComponentJs);
            File.Copy(componentCache, Path.Combine(targetDir, fileName), true);
            availableComponents.Add(component);
        }

        return availableComponents;
    }

    private static async Task EnsureAssetCachedAsync(HttpClient client, string url, string cachePath, string fallbackContent)
    {
        if (File.Exists(cachePath))
            return;

        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using var httpStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(cachePath);
            await httpStream.CopyToAsync(fileStream);
        }
        catch
        {
            await File.WriteAllTextAsync(cachePath, fallbackContent, Encoding.UTF8);
        }
    }

    private static void BuildHtmlHeader(StringBuilder sb, string title)
    {
        sb.Append($@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Preview: {WebUtility.HtmlEncode(title)}</title>
    <style>
        body {{ display: flex; height: 100vh; margin: 0; font-family: 'Segoe UI', Tahoma, sans-serif; background: #252526; color: #ccc; }}
        .sidebar {{ width: 340px; border-right: 1px solid #333; overflow-y: auto; padding: 15px; }}
        .content {{ flex: 1; background: #1e1e1e; }}
        iframe {{ width: 100%; height: 100%; border: none; }}
        ul {{ list-style: none; padding-left: 15px; margin: 5px 0; }}
        li {{ margin: 3px 0; }}
        summary {{ cursor: pointer; padding: 2px; color: #e1e1e1; outline: none; }}
        summary:hover {{ background: #37373d; }}
        a {{ color: #4fc1ff; text-decoration: none; padding: 2px 4px; display: inline-block; }}
        a:hover {{ background: #2a2d2e; color: #fff; }}
        h3 {{ color: #fff; border-bottom: 1px solid #444; padding-bottom: 10px; margin: 0 0 10px 0; }}
    </style>
</head>
<body>
<div class='sidebar'>
    <h3>🔍 {WebUtility.HtmlEncode(title)}</h3>");
    }
}
