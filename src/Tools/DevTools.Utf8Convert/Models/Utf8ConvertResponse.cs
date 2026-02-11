namespace DevTools.Utf8Convert.Models;

public sealed record Utf8ConvertResponse(
    Utf8ConvertSummary Summary,
    IReadOnlyList<Utf8ConvertItem> Items);
