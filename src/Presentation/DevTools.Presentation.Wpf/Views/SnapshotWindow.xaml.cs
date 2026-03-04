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

using DevTools.Core.Configuration;

namespace DevTools.Presentation.Wpf.Views;

public partial class SnapshotWindow : Window
{
    private readonly JobManager _jobManager = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly ProfileManager _profileManager = null!;
    private ToolProfile? _currentProfile;

    public SnapshotWindow(JobManager jobManager, SettingsService settingsService, ProfileManager profileManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _profileManager = profileManager;

        Loaded += OnLoaded;
    }

    // Construtor para o Designer
    public SnapshotWindow()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Tentar carregar perfil padrão
        _currentProfile = _profileManager?.GetDefaultProfile("Snapshot");
        if (_currentProfile != null)
        {
            if (_currentProfile.Options.TryGetValue("project-path", out var proj)) RootPathSelector.SelectedPath = proj;
        }
        else
        {
            // Fallback para configurações salvas anteriormente
            if (!string.IsNullOrEmpty(_settingsService?.Settings.LastSnapshotRootPath))
                RootPathSelector.SelectedPath = _settingsService.Settings.LastSnapshotRootPath;
        }
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

        // Sincronizar com o perfil padrão se estiver em uso
        if (_currentProfile != null)
        {
            _currentProfile.Options["project-path"] = root ?? "";
            _profileManager.SaveProfile("Snapshot", _currentProfile);
        }

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
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(RootPathSelector.SelectedPath))
            missing.Add("Pasta do Projeto");

        if (missing.Count > 0)
        {
            errorMessage = "Os campos abaixo não podem ficar em branco:\n- " + string.Join("\n- ", missing);
            return false;
        }

        if (string.IsNullOrWhiteSpace(RootPathSelector.SelectedPath))
        {
            errorMessage = "Pasta do Projeto é obrigatória.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
