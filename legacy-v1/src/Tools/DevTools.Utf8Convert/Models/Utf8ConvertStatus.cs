namespace DevTools.Utf8Convert.Models;

public enum Utf8ConvertStatus
{
    Converted = 0,
    AlreadyUtf8 = 1,
    SkippedBinary = 2,
    SkippedExcluded = 3,
    Error = 4
}
