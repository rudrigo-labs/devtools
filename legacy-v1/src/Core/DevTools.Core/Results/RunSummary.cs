namespace DevTools.Core.Results;

public sealed record RunSummary(
    string ToolName,
    string Mode, // "DryRun" | "Real"
    string MainInput,
    string? OutputLocation,
    int Processed,
    int Changed,
    int Ignored,
    int Failed,
    TimeSpan Duration
)
{
    public static RunSummary Empty => new(
        ToolName: string.Empty,
        Mode: "Real",
        MainInput: string.Empty,
        OutputLocation: null,
        Processed: 0,
        Changed: 0,
        Ignored: 0,
        Failed: 0,
        Duration: TimeSpan.Zero
    );
}
