using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DevTools.Core.Configuration;
using DevTools.Core.Models;
using DevTools.Migrations.Engine;
using DevTools.Migrations.Models;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Views;

public partial class MigrationsWindow : Window
{
    private readonly JobManager _jobManager = null!;
    private readonly SettingsService _settings = null!;
    private readonly ConfigService _config = null!;
    private readonly ProfileManager _profileManager = null!;
    private ToolProfile? _currentProfile;

    public MigrationsWindow(JobManager jobManager, SettingsService settings, ConfigService config, ProfileManager profileManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settings = settings;
        _config = config;
        _profileManager = profileManager;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    // Construtor para o Designer
    public MigrationsWindow()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _currentProfile = _profileManager?.GetDefaultProfile("Migrations");
        if (_currentProfile != null)
        {
            if (_currentProfile.Options.TryGetValue("root-path", out var root)) ProjectSelector.SelectedPath = root;
            if (_currentProfile.Options.TryGetValue("startup-path", out var startup)) StartupSelector.SelectedPath = startup;
            if (_currentProfile.Options.TryGetValue("dbcontext", out var context)) DbContextInput.Text = context;
            return;
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _settings.Settings.MigrationsWindowTop = Top;
        _settings.Settings.MigrationsWindowLeft = Left;
        _settings.Settings.LastMigrationsRootPath = ProjectSelector.SelectedPath;
        _settings.Settings.LastMigrationsStartupPath = StartupSelector.SelectedPath;
        _settings.Settings.LastMigrationsDbContext = DbContextInput.Text;
        _settings.Save();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ActionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MigrationNamePanel == null) return;

        var selectedTag = (ActionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        MigrationNamePanel.Visibility = selectedTag == "Add" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Execute_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs(out var validationError, out var action, out var provider, out var root, out var startup, out var migrationName))
        {
            ValidationUiService.ShowInline(MainFrame, validationError);
            return;
        }

        ValidationUiService.ClearInline(MainFrame);

        var request = new MigrationsRequest(
            Action: action,
            Provider: provider,
            Settings: new MigrationsSettings
            {
                RootPath = root,
                StartupProjectPath = startup,
                DbContextFullName = DbContextInput.Text
            },
            MigrationName: migrationName,
            DryRun: DryRunCheck.IsChecked == true,
            WorkingDirectory: root
        );

        if (_currentProfile != null)
        {
            _currentProfile.Options["root-path"] = root ?? string.Empty;
            _currentProfile.Options["startup-path"] = startup ?? string.Empty;
            _currentProfile.Options["dbcontext"] = DbContextInput.Text ?? string.Empty;
            _profileManager.SaveProfile("Migrations", _currentProfile);
        }

        OutputText.Text = "Iniciando...";

        _jobManager.StartJob("EF Core Migration", async (progress, ct) =>
        {
            var engine = new MigrationsEngine();
            var result = await engine.ExecuteAsync(request, progress, ct);

            if (result.IsSuccess)
            {
                Dispatcher.Invoke(() =>
                {
                    OutputText.Text = result.Value?.StdOut ?? "Comando executado com sucesso (sem saida).";
                });
                return "Comando EF Core finalizado com sucesso.";
            }

            Dispatcher.Invoke(() =>
            {
                OutputText.Text = $"ERRO:\n{string.Join("\n", result.Errors.Select(e => e.Message))}\n\nDetalhes:\n{result.Value?.StdOut}\n{result.Value?.StdErr}";
            });
            return "Falha ao executar comando EF Core.";
        });
    }

    private bool ValidateInputs(
        out string errorMessage,
        out MigrationsAction action,
        out DatabaseProvider provider,
        out string root,
        out string startup,
        out string migrationName)
    {
        root = ProjectSelector.SelectedPath;
        startup = StartupSelector.SelectedPath;
        migrationName = MigrationNameInput.Text;

        var actionTag = (ActionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        action = actionTag == "Add" ? MigrationsAction.AddMigration : MigrationsAction.UpdateDatabase;

        var providerTag = (ProviderCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        provider = providerTag switch
        {
            "Sqlite" => DatabaseProvider.Sqlite,
            _ => DatabaseProvider.SqlServer
        };

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(root))
            missing.Add("Diretorio do Projeto");
        if (string.IsNullOrWhiteSpace(startup))
            missing.Add("Diretorio do Startup Project");
        if (action == MigrationsAction.AddMigration && string.IsNullOrWhiteSpace(migrationName))
            missing.Add("Nome da Migration");
        if (string.IsNullOrWhiteSpace(DbContextInput.Text))
            missing.Add("Nome completo do DbContext");

        if (missing.Count > 0)
        {
            errorMessage = "Os campos abaixo nao podem ficar em branco:\n- " + string.Join("\n- ", missing);
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
