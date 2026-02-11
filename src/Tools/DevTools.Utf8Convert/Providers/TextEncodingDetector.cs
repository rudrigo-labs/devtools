using System.Text;

namespace DevTools.Utf8Convert.Providers;

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

        if (TryDetectUtf32(bytes, out var utf32Encoding))
        {
            var content = utf32Encoding.GetString(bytes);
            return new DetectedText(content, utf32Encoding, false, false, utf32Encoding.WebName);
        }

        if (TryDetectUtf16(bytes, out var utf16Encoding))
        {
            var content = utf16Encoding.GetString(bytes);
            return new DetectedText(content, utf16Encoding, false, false, utf16Encoding.WebName);
        }

        if (TryDecodeUtf8(bytes, out var utf8))
        {
            return new DetectedText(utf8, new UTF8Encoding(false, true), false, false, "utf-8");
        }

        if (LooksBinary(bytes))
            return new DetectedText(string.Empty, new UTF8Encoding(false, true), false, true, null);

        var fallback = Encoding.GetEncoding(1252);
        var fallbackText = fallback.GetString(bytes);
        return new DetectedText(fallbackText, fallback, false, false, fallback.WebName);
    }

    private static Encoding? GetBomEncoding(byte[] bytes, out int bomLength)
    {
        bomLength = 0;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            bomLength = 3;
            return new UTF8Encoding(true, true);
        }

        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
        {
            bomLength = 4;
            return new UTF32Encoding(false, true);
        }

        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
        {
            bomLength = 4;
            return new UTF32Encoding(true, true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            bomLength = 2;
            return new UnicodeEncoding(false, true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            bomLength = 2;
            return new UnicodeEncoding(true, true);
        }

        return null;
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

    private static bool TryDetectUtf16(byte[] bytes, out Encoding encoding)
    {
        encoding = new UnicodeEncoding(false, false);

        if (!bytes.Any(b => b == 0))
            return false;

        var sample = bytes.Length > 4096 ? bytes.AsSpan(0, 4096) : bytes.AsSpan();
        int evenZero = 0, oddZero = 0;
        int evenCount = 0, oddCount = 0;

        for (int i = 0; i < sample.Length; i++)
        {
            if (i % 2 == 0)
            {
                evenCount++;
                if (sample[i] == 0) evenZero++;
            }
            else
            {
                oddCount++;
                if (sample[i] == 0) oddZero++;
            }
        }

        if (evenCount == 0 || oddCount == 0)
            return false;

        var evenRatio = evenZero / (double)evenCount;
        var oddRatio = oddZero / (double)oddCount;

        if (oddRatio > 0.6 && evenRatio < 0.2)
        {
            encoding = new UnicodeEncoding(false, false);
            return true;
        }

        if (evenRatio > 0.6 && oddRatio < 0.2)
        {
            encoding = new UnicodeEncoding(true, false);
            return true;
        }

        return false;
    }

    private static bool TryDetectUtf32(byte[] bytes, out Encoding encoding)
    {
        encoding = new UTF32Encoding(false, false);

        if (bytes.Length < 4 || !bytes.Any(b => b == 0))
            return false;

        var sample = bytes.Length > 4096 ? bytes.AsSpan(0, 4096) : bytes.AsSpan();
        int mod0 = 0, mod1 = 0, mod2 = 0, mod3 = 0;
        int mod0Count = 0, mod1Count = 0, mod2Count = 0, mod3Count = 0;

        for (int i = 0; i < sample.Length; i++)
        {
            var mod = i % 4;
            switch (mod)
            {
                case 0:
                    mod0Count++;
                    if (sample[i] == 0) mod0++;
                    break;
                case 1:
                    mod1Count++;
                    if (sample[i] == 0) mod1++;
                    break;
                case 2:
                    mod2Count++;
                    if (sample[i] == 0) mod2++;
                    break;
                case 3:
                    mod3Count++;
                    if (sample[i] == 0) mod3++;
                    break;
            }
        }

        if (mod0Count == 0 || mod1Count == 0 || mod2Count == 0 || mod3Count == 0)
            return false;

        var non0ZeroRatio = (mod1 + mod2 + mod3) / (double)(mod1Count + mod2Count + mod3Count);
        var mod0Ratio = mod0 / (double)mod0Count;

        if (non0ZeroRatio > 0.7 && mod0Ratio < 0.2)
        {
            encoding = new UTF32Encoding(false, false);
            return true;
        }

        var mod3Ratio = mod3 / (double)mod3Count;
        var non3ZeroRatio = (mod0 + mod1 + mod2) / (double)(mod0Count + mod1Count + mod2Count);

        if (non3ZeroRatio > 0.7 && mod3Ratio < 0.2)
        {
            encoding = new UTF32Encoding(true, false);
            return true;
        }

        return false;
    }

    private static bool LooksBinary(byte[] bytes)
    {
        var sample = bytes.Length > 4096 ? bytes.AsSpan(0, 4096) : bytes.AsSpan();
        if (sample.Length == 0) return false;

        var controlCount = 0;
        foreach (var b in sample)
        {
            if (b == 0) return true;

            if (b < 9 || (b > 13 && b < 32))
                controlCount++;
        }

        var ratio = controlCount / (double)sample.Length;
        return ratio > 0.3;
    }
}

public sealed record DetectedText(
    string Content,
    Encoding Encoding,
    bool HasBom,
    bool IsBinary,
    string? DetectedName);
