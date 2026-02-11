namespace DevTools.Notes.Cloud;

public class CloudConfiguration
{
    // These should ideally be loaded from a secure config or injected at build time
    // For this implementation, we define the structure. 
    // Users might need to provide their own keys if this is an open source tool without embedded secrets.
    
    public string GoogleClientId { get; set; } = string.Empty;
    public string GoogleClientSecret { get; set; } = string.Empty;
    
    public string OneDriveClientId { get; set; } = string.Empty;
}
