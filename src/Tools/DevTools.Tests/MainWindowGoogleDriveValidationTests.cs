using System.Reflection;
using DevTools.Presentation.Wpf.Models;
using DevTools.Presentation.Wpf.Views;

namespace DevTools.Tests;

public class MainWindowGoogleDriveValidationTests
{
    [Fact]
    public void ValidateGoogleDriveSettings_WithMissingFields_ReturnsFalseAndListsFields()
    {
        var settings = new GoogleDriveSettings
        {
            IsEnabled = true,
            ClientId = "",
            ClientSecret = "   ",
            ProjectId = "",
            FolderName = ""
        };

        var (isValid, message) = InvokeValidation(settings);

        Assert.False(isValid);
        Assert.Contains("Client ID", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Client Secret", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Project ID", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Nome da Pasta", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateGoogleDriveSettings_WithAllFields_ReturnsTrue()
    {
        var settings = new GoogleDriveSettings
        {
            IsEnabled = true,
            ClientId = "client-id",
            ClientSecret = "client-secret",
            ProjectId = "project-id",
            FolderName = "DevToolsNotes"
        };

        var (isValid, message) = InvokeValidation(settings);

        Assert.True(isValid);
        Assert.True(string.IsNullOrWhiteSpace(message));
    }

    private static (bool IsValid, string Message) InvokeValidation(GoogleDriveSettings settings)
    {
        var method = typeof(MainWindow).GetMethod(
            "ValidateGoogleDriveSettings",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);

        object?[] args = { settings, null };
        var result = method!.Invoke(null, args);

        Assert.NotNull(result);
        return ((bool)result!, args[1] as string ?? string.Empty);
    }
}
