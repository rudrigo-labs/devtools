namespace DevTools.Organizer.Models;

public sealed record OrganizerStats(
    int TotalFiles,
    int EligibleFiles,
    int WouldMove,
    int Duplicates,
    int Ignored,
    int Errors);
