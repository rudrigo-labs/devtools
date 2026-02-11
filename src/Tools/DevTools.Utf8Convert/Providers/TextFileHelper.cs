using System.Text;
using DevTools.Utf8Convert.Abstractions;

namespace DevTools.Utf8Convert.Providers;

public static class TextFileHelper
{
    public static async Task<DetectedText> ReadAsync(IUtf8FileSystem fs, string path, CancellationToken ct)
    {
        var bytes = await fs.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        return TextEncodingDetector.Detect(bytes);
    }

    public static async Task WriteUtf8Async(IUtf8FileSystem fs, string path, string content, bool outputBom, CancellationToken ct)
    {
        var encoding = new UTF8Encoding(outputBom, true);
        var payload = encoding.GetBytes(content);
        await fs.WriteAllBytesAsync(path, payload, ct).ConfigureAwait(false);
    }
}
