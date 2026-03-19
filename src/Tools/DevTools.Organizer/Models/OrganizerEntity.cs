using DevTools.Core.Models;

namespace DevTools.Organizer.Models;

/// <summary>
/// Configuração nomeada do Organizer.
/// Herda NamedConfiguration com metadados de persistência.
/// </summary>
public sealed class OrganizerEntity : NamedConfiguration
{
    public string InboxPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public int MinScore { get; set; } = 3;
    public bool Apply { get; set; }

    public string[] AllowedExtensions { get; set; } = OrganizerDefaults.DefaultAllowedExtensions();
    public double FileNameWeight { get; set; } = OrganizerDefaults.DefaultFileNameWeight;
    public bool DeduplicateByHash { get; set; } = OrganizerDefaults.DefaultDeduplicateByHash;
    public bool DeduplicateByName { get; set; } = OrganizerDefaults.DefaultDeduplicateByName;
    public int DeduplicateFirstLines { get; set; } = OrganizerDefaults.DefaultDeduplicateFirstLines;
    public string DuplicatesFolderName { get; set; } = OrganizerDefaults.DefaultDuplicatesFolderName;
    public string OthersFolderName { get; set; } = OrganizerDefaults.DefaultOthersFolderName;

    public List<OrganizerCategory> Categories { get; set; } = OrganizerDefaults.DefaultCategories();
}
