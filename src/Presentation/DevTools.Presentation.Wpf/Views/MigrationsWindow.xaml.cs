using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using DevTools.Core.Models;
using DevTools.Migrations.Engine;
using DevTools.Migrations.Models;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Views;

public partial class MigrationsWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settings;
    private readonly ConfigService _config;

    public MigrationsWindow(JobManager jobManager, SettingsService settings, ConfigService config)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settings = settings;
        _config = config;

        ProfileSelector.GetOptionsFunc = GetCurrentOptions;
        ProfileSelector.ProfileLoaded += LoadProfile;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Restore Position disabled to enforce TrayService placement
        /*
        if (_settings.Settings.MigrationsWindowTop.HasValue)
        {
            Top = _settings.Settings.MigrationsWindowTop.Value;
            Left = _settings.Settings.MigrationsWindowLeft.Value;
        }
        */

        // Restore Inputs
        if (!string.IsNullOrEmpty(_settings.Settings.LastMigrationsRootPath))
            ProjectSelector.SelectedPath = _settings.Settings.LastMigrationsRootPath;

        if (!string.IsNullOrEmpty(_settings.Settings.LastMigrationsStartupPath))
            StartupSelector.SelectedPath = _settings.Settings.LastMigrationsStartupPath;

        if (!string.IsNullOrEmpty(_settings.Settings.LastMigrationsDbContext))
            DbContextInput.Text = _settings.Settings.LastMigrationsDbContext;
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

    private Dictionary<string, string> GetCurrentOptions()
    {
        var options = new Dictionary<string, string>();
        options["root"] = ProjectSelector.SelectedPath;
        options["startup"] = StartupSelector.SelectedPath;
        options["action"] = (ActionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Add";
        options["provider"] = (ProviderCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "SqlServer";
        options["name"] = MigrationNameInput.Text;
        options["context"] = DbContextInput.Text;
        options["dry-run"] = (DryRunCheck.IsChecked ?? false).ToString().ToLowerInvariant();
        return options;
    }

    private void LoadProfile(ToolProfile profile)
    {
        if (profile.Options.TryGetValue("root", out var root)) ProjectSelector.SelectedPath = root;
        if (profile.Options.TryGetValue("startup", out var startup)) StartupSelector.SelectedPath = startup;
        
        if (profile.Options.TryGetValue("action", out var action))
        {
            foreach (ComboBoxItem item in ActionCombo.Items)
            {
                if (item.Tag?.ToString() == action)
                {
                    ActionCombo.SelectedItem = item;
                    break;
                }
            }
        }

        if (profile.Options.TryGetValue("provider", out var provider))
        {
             foreach (ComboBoxItem item in ProviderCombo.Items)
            {
                if (item.Tag?.ToString() == provider)
                {
                    ProviderCombo.SelectedItem = item;
                    break;
                }
            }
        }

        if (profile.Options.TryGetValue("name", out var name)) MigrationNameInput.Text = name;
        if (profile.Options.TryGetValue("context", out var context)) DbContextInput.Text = context;
        
        if (profile.Options.TryGetValue("dry-run", out var dryRun))
             DryRunCheck.IsChecked = bool.TryParse(dryRun, out var d) ? d : false;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // if (e.ButtonState == MouseButtonState.Pressed) DragMove();
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
        var root = ProjectSelector.SelectedPath;
        var startup = StartupSelector.SelectedPath;
        
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(startup))
        {
            System.Windows.MessageBox.Show("Selecione os diretórios do Projeto e do Startup Project.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var actionTag = (ActionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        var action = actionTag == "Add" ? MigrationsAction.AddMigration : MigrationsAction.UpdateDatabase;

        var providerTag = (ProviderCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        var provider = providerTag switch
        {
            "Sqlite" => DatabaseProvider.Sqlite,
            _ => DatabaseProvider.SqlServer
        };


        var migrationName = MigrationNameInput.Text;
        if (action == MigrationsAction.AddMigration && string.IsNullOrWhiteSpace(migrationName))
        {
            System.Windows.MessageBox.Show("Informe o nome da Migration.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
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
}
