namespace DevTools.Notes.Cloud;

/// <summary>
/// Holds the application's OAuth keys.
/// These are the "Master Keys" registered by the developer (You) once.
/// End-users do NOT need to configure this; they just log in.
/// </summary>
internal static class CloudSecrets
{
    // TODO: Replace with your actual Application Client ID from Google Cloud Console
    // Type: Desktop App / Native
    public const string GoogleClientId = "YOUR_GOOGLE_CLIENT_ID_HERE.apps.googleusercontent.com";
    
    // TODO: Replace with your actual Application Client Secret from Google Cloud Console
    public const string GoogleClientSecret = "YOUR_GOOGLE_CLIENT_SECRET_HERE";

    // TODO: Replace with your actual Application Client ID from Azure Portal
    // Type: Public Client (Mobile & Desktop)
    public const string OneDriveClientId = "YOUR_ONEDRIVE_CLIENT_ID_HERE";
}
