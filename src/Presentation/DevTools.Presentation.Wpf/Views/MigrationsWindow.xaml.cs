using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using DevTools.Core.Models;
using DevTools.Migrations.Engine;
using DevTools.Migrations.Models;
using DevTools.Presentation.Wpf.Services;

using DevTools.Core.Configuration;

namespace DevTools.Presentation.Wpf.Views;

public partial class MigrationsWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settings;
    private readonly ConfigService _config;
    private readonly ProfileManager _profileManager;
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
        // Tentar carregar perfil padrão
        _currentProfile = _profileManager?.GetDefaultProfile("Migrations");
        if (_currentProfile != null)
        {
            if (_currentProfile.Options.TryGetValue("root-path", out var root)) ProjectSelector.SelectedPath = root;
            if (_currentProfile.Options.TryGetValue("startup-path", out var startup)) StartupSelector.SelectedPath = startup;
            if (_currentProfile.Options.TryGetValue("dbcontext", out var context)) DbContextInput.Text = context;
        }
        else
        {
            // Fallback para configurações salvas anteriormente (comportamento original)
            if (!string.IsNullOrEmpty(_settings?.Settings.LastMigrationsRootPath))
                ProjectSelector.SelectedPath = _settings.Settings.LastMigrationsRootPath;

            if (!string.IsNullOrEmpty(_settings?.Settings.LastMigrationsStartupPath))
                StartupSelector.SelectedPath = _settings.Settings.LastMigrationsStartupPath;

            if (!string.IsNullOrEmpty(_settings?.Settings.LastMigrationsDbContext))
                DbContextInput.Text = _settings.Settings.LastMigrationsDbContext;
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
        if (selectedTag == "Add")
        {
            MigrationNamePanel.Visibility = Visibility.Visible;
        }
        else
        {
            MigrationNamePanel.Visibility = Visibility.Collapsed;
        }
    }

    private void Execute_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs(out var validationError, out var action, out var provider, out var root, out var startup, out var migrationName))
        {
            UiMessageService.ShowError(validationError, "Erro de Validação");
            return;
        }

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

        // Sincronizar com o perfil padrão se estiver em uso
        if (_currentProfile != null)
        {
            _currentProfile.Options["root-path"] = root ?? "";
            _currentProfile.Options["startup-path"] = startup ?? "";
            _currentProfile.Options["dbcontext"] = DbContextInput.Text ?? "";
            _profileManager.SaveProfile("Migrations", _currentProfile);
        }

        OutputText.Text = "Iniciando...";

        _jobManager.StartJob("EF Core Migration", async (progress, ct) =>
        {
            var engine = new MigrationsEngine();
            // Engine execution
            var result = await engine.ExecuteAsync(request, progress, ct);

            if (result.IsSuccess)
            {
                Dispatcher.Invoke(() =>
                {
                    OutputText.Text = result.Value?.StdOut ?? "Comando executado com sucesso (sem saída).";
                });
                return "Comando EF Core finalizado com sucesso.";
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    OutputText.Text = $"ERRO:\n{string.Join("\n", result.Errors.Select(e => e.Message))}\n\nDetalhes:\n{result.Value?.StdOut}\n{result.Value?.StdErr}";
                });
                return "Falha ao executar comando EF Core.";
            }
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
            missing.Add("Diretório do Projeto");
        if (string.IsNullOrWhiteSpace(startup))
            missing.Add("Diretório do Startup Project");
        if (action == MigrationsAction.AddMigration && string.IsNullOrWhiteSpace(migrationName))
            missing.Add("Nome da Migration");
        if (string.IsNullOrWhiteSpace(DbContextInput.Text))
            missing.Add("Nome completo do DbContext");

        if (missing.Count > 0)
        {
            errorMessage = "Os campos abaixo não podem ficar em branco:\n- " + string.Join("\n- ", missing);
            return false;
        }

        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(startup))
        {
            errorMessage = "Selecione os diretórios do Projeto e do Startup Project.";
            return false;
        }

        if (action == MigrationsAction.AddMigration && string.IsNullOrWhiteSpace(migrationName))
        {
            errorMessage = "Informe o nome da Migration.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(DbContextInput.Text))
        {
            errorMessage = "Informe o nome completo do DbContext.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
