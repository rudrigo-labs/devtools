namespace DevTools.Organizer.Models;

public sealed record OrganizerResponse(
    string InboxPath,
    string OutputPath,
    OrganizerStats Stats,
    IReadOnlyList<OrganizerPlanItem> Plan);
