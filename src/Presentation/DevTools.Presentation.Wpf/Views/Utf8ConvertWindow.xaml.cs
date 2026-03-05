using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Utf8Convert.Engine;
using DevTools.Utf8Convert.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class Utf8ConvertWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;

    public Utf8ConvertWindow(JobManager jobManager, SettingsService settingsService)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
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
            ValidationUiService.ShowInline(MainFrame, errorMessage);
            return;
        }

        ValidationUiService.ClearInline(MainFrame);

        var root = RootPathSelector.SelectedPath;
        var recursive = RecursiveCheck.IsChecked ?? true;
        var backup = BackupCheck.IsChecked ?? true;
        var bom = BomCheck.IsChecked ?? true;

        _settingsService.Settings.LastUtf8RootPath = root;
        _settingsService.Save();

        Close();

        _jobManager.StartJob("Utf8Convert", async (reporter, ct) =>
        {
            var engine = new Utf8ConvertEngine();
            var request = new Utf8ConvertRequest(
                RootPath: root,
                Recursive: recursive,
                CreateBackup: backup,
                OutputBom: bom
            );

            var result = await engine.ExecuteAsync(request, reporter, ct);

            return result.IsSuccess
                ? $"Conversao concluida! {result.Value?.Summary.Converted ?? 0} arquivos convertidos."
                : $"Falha na conversao: {string.Join(", ", result.Errors.Select(x => x.Message))}";
        });
    }

    private bool ValidateInputs(out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(RootPathSelector.SelectedPath))
        {
            errorMessage = "Os campos abaixo nao podem ficar em branco:\n- Pasta Raiz";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
