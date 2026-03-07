using System.Net;
using System.Text.Json;
using DevTools.Ngrok.Models;

namespace DevTools.Ngrok.Engine;

public sealed class NgrokApiClient
{
    private readonly HttpClient _http;
    private readonly Uri _baseUri;

    public NgrokApiClient(HttpClient http, Uri baseUri)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
    }

    public async Task<IReadOnlyList<TunnelInfo>> GetTunnelsAsync(
        TimeSpan timeout,
        int retryCount,
        CancellationToken ct = default)
    {
        using var resp = await SendAsync(HttpMethod.Get, "api/tunnels", timeout, retryCount, ct).ConfigureAwait(false);
        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        try
        {
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("tunnels", out var tunnelsEl) ||
                tunnelsEl.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<TunnelInfo>();
            }

            var list = new List<TunnelInfo>();

            foreach (var t in tunnelsEl.EnumerateArray())
            {
                var name = GetStringOrEmpty(t, "name");
                var proto = GetStringOrEmpty(t, "proto");
                var publicUrl = GetStringOrEmpty(t, "public_url");

                string? addr = null;
                if (t.TryGetProperty("config", out var cfg) && cfg.ValueKind == JsonValueKind.Object)
                {
                    if (cfg.TryGetProperty("addr", out var addrEl) && addrEl.ValueKind == JsonValueKind.String)
                        addr = addrEl.GetString();
                }

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(publicUrl))
                    continue;

                list.Add(new TunnelInfo(name, proto, publicUrl, addr));
            }

            return list;
        }
        catch (JsonException ex)
        {
            throw new NgrokApiException("ngrok.api.json_invalid", "Ngrok API returned invalid JSON.", ex.Message, resp.StatusCode, ex);
        }
    }

    public async Task<bool> CloseTunnelAsync(
        string name,
        TimeSpan timeout,
        int retryCount,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new NgrokApiException("ngrok.tunnel.required", "Tunnel name is required.");

        var encoded = Uri.EscapeDataString(name);
        using var resp = await SendAsync(HttpMethod.Delete, $"api/tunnels/{encoded}", timeout, retryCount, ct)
            .ConfigureAwait(false);

        return resp.IsSuccessStatusCode;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativePath,
        TimeSpan timeout,
        int retryCount,
        CancellationToken ct)
    {
        var attempts = Math.Max(0, retryCount) + 1;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            using var request = new HttpRequestMessage(method, new Uri(_baseUri, relativePath));
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            try
            {
                var resp = await _http.SendAsync(request, cts.Token).ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await SafeReadBody(resp, ct).ConfigureAwait(false);
                    throw new NgrokApiException(
                        "ngrok.api.http_error",
                        $"Ngrok API returned {(int)resp.StatusCode} ({resp.StatusCode}).",
                        body,
                        resp.StatusCode);
                }

                return resp;
            }
            catch (NgrokApiException)
            {
                throw;
            }
            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
            {
                if (attempt < attempts)
                {
                    await Task.Delay(200, ct).ConfigureAwait(false);
                    continue;
                }

                throw new NgrokApiException(
                    "ngrok.api.timeout",
                    "Ngrok API request timed out.",
                    ex.Message,
                    null,
                    ex);
            }
            catch (HttpRequestException ex)
            {
                if (attempt < attempts)
                {
                    await Task.Delay(200, ct).ConfigureAwait(false);
                    continue;
                }

                throw new NgrokApiException(
                    "ngrok.api.unreachable",
                    "Ngrok API is unreachable.",
                    ex.Message,
                    null,
                    ex);
            }
        }

        throw new NgrokApiException("ngrok.api.unreachable", "Ngrok API is unreachable.");
    }

    private static async Task<string?> SafeReadBody(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(body) ? null : body.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static string GetStringOrEmpty(JsonElement obj, string prop)
    {
        if (!obj.TryGetProperty(prop, out var el))
            return string.Empty;

        return el.ValueKind == JsonValueKind.String
            ? (el.GetString() ?? string.Empty)
            : string.Empty;
    }
}
