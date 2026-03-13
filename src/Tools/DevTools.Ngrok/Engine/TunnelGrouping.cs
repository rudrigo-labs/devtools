using DevTools.Ngrok.Models;

namespace DevTools.Ngrok.Engine;

public static class TunnelGrouping
{
    public static IReadOnlyList<TunnelGroup> GroupByBaseName(IEnumerable<TunnelInfo> tunnels)
    {
        var map = new Dictionary<string, (TunnelInfo? http, TunnelInfo? https)>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in tunnels)
        {
            var baseName = ExtractBaseName(t.Name);

            map.TryGetValue(baseName, out var g);
            if (t.Proto.Equals("https", StringComparison.OrdinalIgnoreCase)) g.https = t;
            else if (t.Proto.Equals("http", StringComparison.OrdinalIgnoreCase)) g.http = t;

            map[baseName] = g;
        }

        return map
            .Select(kv => new TunnelGroup(kv.Key, kv.Value.http, kv.Value.https))
            .OrderBy(x => x.BaseName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string ExtractBaseName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "(tunnel)";
        var idx = name.IndexOf(" (", StringComparison.Ordinal);
        return idx > 0 ? name[..idx] : name;
    }

    public static bool TryExtractPort(string? addr, out int port)
    {
        port = 0;
        if (string.IsNullOrWhiteSpace(addr)) return false;
        if (!Uri.TryCreate(addr, UriKind.Absolute, out var uri)) return false;

        if (uri.Port > 0) { port = uri.Port; return true; }
        port = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : 80;
        return true;
    }
}
