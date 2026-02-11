namespace DevTools.Organizer.Models;

public sealed record OrganizerRequest(
    string InboxPath,
    string OutputPath,
    string? ConfigPath = null,
    int? MinScore = null,
    bool Apply = false);
