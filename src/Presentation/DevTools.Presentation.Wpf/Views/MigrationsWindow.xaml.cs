using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevTools.Core.Models;
using DevTools.Migrations.Engine;
using DevTools.Migrations.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Utilities;

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

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
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

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
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
            DevToolsMessage.Warning("Selecione os diretórios do Projeto e do Startup Project.", "Atenção");
            return;
        }

        if (!System.IO.Directory.Exists(root))
        {
            DevToolsMessage.Error("O diretório do Projeto não existe.", "Diretório Inválido");
            return;
        }

        if (!System.IO.Directory.Exists(startup))
        {
            DevToolsMessage.Error("O diretório de Startup não existe.", "Diretório Inválido");
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
            DevToolsMessage.Warning("Informe o nome da Migration.", "Atenção");
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
        IsEnabled = false;

        _jobManager.StartJob("EF Core Migration", async (progress, ct) =>
        {
            try
            {
                var engine = new MigrationsEngine();
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
            }
            finally
            {
                Dispatcher.Invoke(() => IsEnabled = true);
            }
        });
    }
}

