using System.Windows;
using System.Windows.Controls;
using DevTools.Host.Wpf.Components;

namespace DevTools.Host.Wpf.Services;

public static class ToolHistoryViewHelper
{
    public static async Task RecordAsync(string toolSlug, FrameworkElement root, string title)
    {
        try
        {
            var service = App.GetRequiredService<ToolUsageHistoryUiService>();
            await service.RecordAsync(toolSlug, root, title).ConfigureAwait(true);
        }
        catch
        {
            // Histórico não deve bloquear a ferramenta.
        }
    }

    public static async Task ShowAndApplyAsync(
        FrameworkElement root,
        string toolSlug,
        string toolName,
        TextBlock? statusText = null)
    {
        var service = App.GetRequiredService<ToolUsageHistoryUiService>();
        var entries = await service.ListAsync(toolSlug).ConfigureAwait(true);

        if (entries.Count == 0)
        {
            DevToolsMessageBox.Info(
                Window.GetWindow(root),
                "Não ha histórico para esta ferramenta.",
                "Histórico");
            return;
        }

        var dialog = new ToolHistoryDialog(toolName, entries)
        {
            Owner = Window.GetWindow(root)
        };

        if (dialog.ShowDialog() != true || dialog.SelectedEntry is null)
            return;

        var applied = service.TryApply(root, dialog.SelectedEntry);
        if (statusText is not null)
            statusText.Text = applied ? "Histórico aplicado." : "Não foi possivel aplicar este registro.";
    }
}
