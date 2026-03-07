namespace DevTools.Organizer.Models;

public sealed record OrganizerPlanItem(
    string Source,
    string Target,
    string Category,
    string Reason,
    OrganizerAction Action);
