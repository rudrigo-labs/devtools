using System.Text;

namespace DevTools.Utf8Convert.Infrastructure;

public sealed record DetectedText(
    string Content,
    Encoding Encoding,
    bool HasBom,
    bool IsBinary,
    string? DetectedName);

public static class TextEncodingDetector
{
    public static DetectedText Detect(byte[] bytes)
    {
        if (bytes.Length == 0)
            return new DetectedText(string.Empty, new UTF8Encoding(false, true), false, false, "utf-8");

        var bom = GetBomEncoding(bytes, out var bomLength);
        if (bom is not null)
        {
            var content = bom.GetString(bytes, bomLength, bytes.Length - bomLength);
            return new DetectedText(content, bom, true, false, bom.WebName);
        }

        if (TryDetectUtf32(bytes, out var utf32))
        {
            var content = utf32.GetString(bytes);
            return new DetectedText(content, utf32, false, false, utf32.WebName);
        }

        if (TryDetectUtf16(bytes, out var utf16))
        {
            var content = utf16.GetString(bytes);
            return new DetectedText(content, utf16, false, false, utf16.WebName);
        }

        if (TryDecodeUtf8(bytes, out var utf8Text))
            return new DetectedText(utf8Text, new UTF8Encoding(false, true), false, false, "utf-8");

        if (LooksBinary(bytes))
            return new DetectedText(string.Empty, new UTF8Encoding(false, true), false, true, null);

        var fallback = Encoding.GetEncoding(1252);
        return new DetectedText(fallback.GetString(bytes), fallback, false, false, fallback.WebName);
    }

    private static Encoding? GetBomEncoding(byte[] bytes, out int bomLength)
    {
        bomLength = 0;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        { bomLength = 3; return new UTF8Encoding(true, true); }
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

    private static bool TryDecodeUtf8(byte[] bytes, out string content)
    {
        try
        {
            content = new UTF8Encoding(false, true).GetString(bytes);
            return true;
        }
        catch { content = string.Empty; return false; }
    }

    private static bool TryDetectUtf16(byte[] bytes, out Encoding encoding)
    {
        encoding = new UnicodeEncoding(false, false);
        if (!bytes.Any(b => b == 0)) return false;

        var sample = bytes.Length > 4096 ? bytes.AsSpan(0, 4096) : bytes.AsSpan();
        int evenZero = 0, oddZero = 0, evenCount = 0, oddCount = 0;
        for (int i = 0; i < sample.Length; i++)
        {
            if (i % 2 == 0) { evenCount++; if (sample[i] == 0) evenZero++; }
            else { oddCount++; if (sample[i] == 0) oddZero++; }
        }
        if (evenCount == 0 || oddCount == 0) return false;
        var er = evenZero / (double)evenCount;
        var or2 = oddZero / (double)oddCount;
        if (or2 > 0.6 && er < 0.2) { encoding = new UnicodeEncoding(false, false); return true; }
        if (er > 0.6 && or2 < 0.2) { encoding = new UnicodeEncoding(true, false); return true; }
        return false;
    }

    private static bool TryDetectUtf32(byte[] bytes, out Encoding encoding)
    {
        encoding = new UTF32Encoding(false, false);
        if (bytes.Length < 4 || !bytes.Any(b => b == 0)) return false;

        var sample = bytes.Length > 4096 ? bytes.AsSpan(0, 4096) : bytes.AsSpan();
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
        var sample = bytes.Length > 4096 ? bytes.AsSpan(0, 4096) : bytes.AsSpan();
        if (sample.Length == 0) return false;
        var ctrl = 0;
        foreach (var b in sample)
        {
            if (b == 0) return true;
            if (b < 9 || (b > 13 && b < 32)) ctrl++;
        }
        return ctrl / (double)sample.Length > 0.3;
    }
}
