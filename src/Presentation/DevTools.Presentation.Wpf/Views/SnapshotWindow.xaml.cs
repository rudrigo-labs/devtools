using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using DevTools.Presentation.Wpf.Services;
using DevTools.Snapshot.Engine;
using DevTools.Snapshot.Models;
using DevTools.Core.Models;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Views;

public partial class SnapshotWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;

    public SnapshotWindow(JobManager jobManager, SettingsService settingsService)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;

        ProfileSelector.GetOptionsFunc = GetCurrentOptions;
        ProfileSelector.ProfileLoaded += LoadProfile;

        /* Position handled by TrayService
        if (_settingsService.Settings.SnapshotWindowTop.HasValue)
        {
            Top = _settingsService.Settings.SnapshotWindowTop.Value;
            Left = _settingsService.Settings.SnapshotWindowLeft.Value;
        }
        else
        {
            var screen = SystemParameters.WorkArea;
            Left = screen.Right - Width - 20;
            Top = screen.Bottom - Height - 20;
        }

        var workArea = SystemParameters.WorkArea;
        if (Top < 0 || Top > workArea.Height) Top = workArea.Height - Height - 20;
        if (Left < 0 || Left > workArea.Width) Left = workArea.Width - Width - 20;
        */

        /*
        Closed += (s, e) =>
        {
            _settingsService.Settings.SnapshotWindowTop = Top;
            _settingsService.Settings.SnapshotWindowLeft = Left;
            _settingsService.Save();
        };
        */
    }

    private Dictionary<string, string> GetCurrentOptions()
    {
        var options = new Dictionary<string, string>();
        options["root"] = RootPathSelector.SelectedPath;
        options["text"] = (TextCheck.IsChecked ?? true).ToString().ToLowerInvariant();
        options["html"] = (HtmlCheck.IsChecked ?? false).ToString().ToLowerInvariant();
        options["json-nested"] = (JsonNestedCheck.IsChecked ?? false).ToString().ToLowerInvariant();
        options["json-recursive"] = (JsonRecursiveCheck.IsChecked ?? false).ToString().ToLowerInvariant();
        return options;
    }

    private void LoadProfile(ToolProfile profile)
    {
        if (profile.Options.TryGetValue("root", out var root)) RootPathSelector.SelectedPath = root;
        
        if (profile.Options.TryGetValue("text", out var text)) 
            TextCheck.IsChecked = bool.TryParse(text, out var t) ? t : true;
            
        if (profile.Options.TryGetValue("html", out var html)) 
            HtmlCheck.IsChecked = bool.TryParse(html, out var h) ? h : false;
            
        if (profile.Options.TryGetValue("json-nested", out var jn)) 
            JsonNestedCheck.IsChecked = bool.TryParse(jn, out var n) ? n : false;
            
        if (profile.Options.TryGetValue("json-recursive", out var jr)) 
            JsonRecursiveCheck.IsChecked = bool.TryParse(jr, out var r) ? r : false;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void BrowseRoot_Click(object sender, RoutedEventArgs e) { }

    private void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs(out var errorMessage))
        {
            UiMessageService.ShowError(errorMessage, "Erro de Validação");
            return;
        }

        var root = RootPathSelector.SelectedPath;
        var genText = TextCheck.IsChecked ?? true;
        var genHtml = HtmlCheck.IsChecked ?? false;
        var genJsonNested = JsonNestedCheck.IsChecked ?? false;
        var genJsonRecursive = JsonRecursiveCheck.IsChecked ?? false;

        Close();

        _settingsService.Settings.LastSnapshotRootPath = root;
        _settingsService.Save();

        _jobManager.StartJob("Snapshot", async (reporter, ct) =>
        {
            var engine = new SnapshotEngine();
            var request = new SnapshotRequest(
                RootPath: root,
                GenerateText: genText,
                GenerateHtmlPreview: genHtml,
                GenerateJsonNested: genJsonNested,
                GenerateJsonRecursive: genJsonRecursive
            );

            var result = await engine.ExecuteAsync(request, reporter, ct);

            return result.IsSuccess
                ? $"Snapshot gerado com sucesso na pasta 'Snapshot'!"
                : $"Falha ao gerar Snapshot: {string.Join(", ", result.Errors.Select(x => x.Message))}";
        });
    }

    private bool ValidateInputs(out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(RootPathSelector.SelectedPath))
        {
            errorMessage = "Pasta do Projeto é obrigatória.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
