using DevTools.Rename.Abstractions;

namespace DevTools.Rename.Providers;

public static class TextFileHelper
{
    public static async Task<DetectedText> ReadAsync(IRenameFileSystem fs, string path, CancellationToken ct)
    {
        var bytes = await fs.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        return TextEncodingDetector.Detect(bytes);
    }

    public static async Task WriteAsync(IRenameFileSystem fs, string path, string content, DetectedText detected, CancellationToken ct)
    {
        var encoding = detected.Encoding;
        var payload = encoding.GetBytes(content);
        var preamble = detected.HasBom ? encoding.GetPreamble() : Array.Empty<byte>();

        if (preamble.Length == 0)
        {
            await fs.WriteAllBytesAsync(path, payload, ct).ConfigureAwait(false);
            return;
        }

        var combined = new byte[preamble.Length + payload.Length];
        Buffer.BlockCopy(preamble, 0, combined, 0, preamble.Length);
        Buffer.BlockCopy(payload, 0, combined, preamble.Length, payload.Length);
        await fs.WriteAllBytesAsync(path, combined, ct).ConfigureAwait(false);
    }
}
