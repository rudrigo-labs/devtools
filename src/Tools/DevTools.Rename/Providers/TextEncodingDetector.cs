using System.Text;

namespace DevTools.Rename.Providers;

public static class TextEncodingDetector
{
    public static DetectedText Detect(byte[] bytes)
    {
        if (bytes.Length == 0)
            return new DetectedText(string.Empty, new UTF8Encoding(false), false, false);

        if (HasUtf8Bom(bytes))
            return new DetectedText(Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3), new UTF8Encoding(true), true, false);

        if (HasUtf32LeBom(bytes))
            return new DetectedText(Encoding.UTF32.GetString(bytes, 4, bytes.Length - 4), new UTF32Encoding(false, true), true, false);

        if (HasUtf32BeBom(bytes))
            return new DetectedText(new UTF32Encoding(true, true).GetString(bytes, 4, bytes.Length - 4), new UTF32Encoding(true, true), true, false);

        if (HasUtf16LeBom(bytes))
            return new DetectedText(Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2), new UnicodeEncoding(false, true), true, false);

        if (HasUtf16BeBom(bytes))
            return new DetectedText(Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2), new UnicodeEncoding(true, true), true, false);

        if (bytes.Any(b => b == 0))
            return new DetectedText(string.Empty, new UTF8Encoding(false), false, true);

        if (TryDecodeUtf8(bytes, out var utf8))
            return new DetectedText(utf8, new UTF8Encoding(false), false, false);

        var fallback = Encoding.GetEncoding(1252);
        return new DetectedText(fallback.GetString(bytes), fallback, false, false);
    }

    private static bool TryDecodeUtf8(byte[] bytes, out string content)
    {
        try
        {
            var utf8 = new UTF8Encoding(false, true);
            content = utf8.GetString(bytes);
            return true;
        }
        catch
        {
            content = string.Empty;
            return false;
        }
    }

    private static bool HasUtf8Bom(byte[] bytes)
        => bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

    private static bool HasUtf16LeBom(byte[] bytes)
        => bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE;

    private static bool HasUtf16BeBom(byte[] bytes)
        => bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF;

    private static bool HasUtf32LeBom(byte[] bytes)
        => bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00;

    private static bool HasUtf32BeBom(byte[] bytes)
        => bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF;
}

public sealed record DetectedText(
    string Content,
    Encoding Encoding,
    bool HasBom,
    bool IsBinary);
