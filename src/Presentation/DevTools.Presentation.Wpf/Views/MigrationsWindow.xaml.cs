using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    private static readonly string[] BlockedMigrationsArgs =
    {
        "--project", "-p", "project",
        "--startup-project", "-s", "startup-project", "startupproject",
        "--context", "-c", "context", "dbcontext"
    };
    private readonly JobManager _jobManager = null!;
    private readonly SettingsService _settings = null!;
    private readonly ConfigService _config = null!;
    private readonly ToolConfigurationManager _toolConfigurationManager = null!;
    private ToolConfiguration? _currentConfiguration;
    private MigrationsSettings _resolvedSettings = new();

    public MigrationsWindow(JobManager jobManager, SettingsService settings, ConfigService config, ToolConfigurationManager toolConfigurationManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settings = settings;
        _config = config;
        _toolConfigurationManager = toolConfigurationManager;

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
        _resolvedSettings = _config.GetSection<MigrationsSettings>("Migrations");
        NormalizeMigrationsSettings(_resolvedSettings);

        ProjectSelector.SelectedPath = _resolvedSettings.RootPath;
        StartupSelector.SelectedPath = _resolvedSettings.StartupProjectPath;
        DbContextInput.Text = _resolvedSettings.DbContextFullName;
        AdditionalArgsInput.Text = _resolvedSettings.AdditionalArgs ?? string.Empty;

        _currentConfiguration = _toolConfigurationManager?.GetDefaultConfiguration("Migrations");
        if (_currentConfiguration is null)
            return;

        if (_currentConfiguration.Options.TryGetValue("root-path", out var root))
            ProjectSelector.SelectedPath = root;

        if (_currentConfiguration.Options.TryGetValue("startup-path", out var startup))
            StartupSelector.SelectedPath = startup;

        if (_currentConfiguration.Options.TryGetValue("dbcontext", out var context))
            DbContextInput.Text = context;

        if (_currentConfiguration.Options.TryGetValue("additional-args", out var additionalArgs))
            _resolvedSettings.AdditionalArgs = additionalArgs;

        if (_currentConfiguration.Options.TryGetValue("target-sqlserver-path", out var sqlServerTarget))
            SetTargetPath(_resolvedSettings, DatabaseProvider.SqlServer, sqlServerTarget);

        if (_currentConfiguration.Options.TryGetValue("target-sqlite-path", out var sqliteTarget))
            SetTargetPath(_resolvedSettings, DatabaseProvider.Sqlite, sqliteTarget);

        AdditionalArgsInput.Text = _resolvedSettings.AdditionalArgs ?? string.Empty;
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

    private void AdditionalArgChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Content is not string argToken || string.IsNullOrWhiteSpace(argToken))
            return;

        var current = (AdditionalArgsInput.Text ?? string.Empty).Trim();
        if (ContainsArgToken(current, argToken))
            return;

        AdditionalArgsInput.Text = string.IsNullOrWhiteSpace(current) ? argToken : $"{current} {argToken}";
        AdditionalArgsInput.Focus();
        AdditionalArgsInput.CaretIndex = AdditionalArgsInput.Text.Length;
    }

    private void ActionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MigrationNamePanel == null) return;

        var selectedTag = (ActionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        MigrationNamePanel.Visibility = selectedTag == "Add" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Execute_Click(object sender, RoutedEventArgs e)
    {
        _resolvedSettings.AdditionalArgs = string.IsNullOrWhiteSpace(AdditionalArgsInput.Text)
            ? null
            : AdditionalArgsInput.Text.Trim();

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
                DbContextFullName = DbContextInput.Text.Trim(),
                Targets = _resolvedSettings.Targets
                    .Select(target => new MigrationTarget
                    {
                        Provider = target.Provider,
                        MigrationsProjectPath = target.MigrationsProjectPath
                    })
                    .ToList(),
                AdditionalArgs = string.IsNullOrWhiteSpace(_resolvedSettings.AdditionalArgs)
                    ? null
                    : _resolvedSettings.AdditionalArgs.Trim()
            },
            MigrationName: migrationName,
            DryRun: DryRunCheck.IsChecked == true,
            WorkingDirectory: root
        );

        if (_currentConfiguration != null)
        {
            _currentConfiguration.Options["root-path"] = root ?? string.Empty;
            _currentConfiguration.Options["startup-path"] = startup ?? string.Empty;
            _currentConfiguration.Options["dbcontext"] = DbContextInput.Text ?? string.Empty;
            _currentConfiguration.Options["additional-args"] = _resolvedSettings.AdditionalArgs ?? string.Empty;
            _currentConfiguration.Options["target-sqlserver-path"] = GetTargetPath(_resolvedSettings, DatabaseProvider.SqlServer);
            _currentConfiguration.Options["target-sqlite-path"] = GetTargetPath(_resolvedSettings, DatabaseProvider.Sqlite);
            _toolConfigurationManager.SaveConfiguration("Migrations", _currentConfiguration);
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
        var actionTag = (ActionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        action = actionTag switch
        {
            "Add" => MigrationsAction.AddMigration,
            "Update" => MigrationsAction.UpdateDatabase,
            _ => default
        };

        if (actionTag is not ("Add" or "Update"))
        {
            errorMessage = "Acao de migration invalida.";
            provider = default;
            root = string.Empty;
            startup = string.Empty;
            migrationName = string.Empty;
            return false;
        }

        var providerTag = (ProviderCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        provider = providerTag switch
        {
            "Sqlite" => DatabaseProvider.Sqlite,
            "SqlServer" => DatabaseProvider.SqlServer,
            _ => default
        };

        if (providerTag is not ("SqlServer" or "Sqlite"))
        {
            errorMessage = "Provider invalido.";
            root = string.Empty;
            startup = string.Empty;
            migrationName = string.Empty;
            return false;
        }

        root = ProjectSelector.SelectedPath;
        startup = StartupSelector.SelectedPath;
        migrationName = (MigrationNameInput.Text ?? string.Empty).Trim();

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(root))
            missing.Add("Pasta Raiz do Projeto");
        if (string.IsNullOrWhiteSpace(startup))
            missing.Add("Arquivo do Startup Project (.csproj)");
        if (action == MigrationsAction.AddMigration && string.IsNullOrWhiteSpace(migrationName))
            missing.Add("Nome da Migration");
        if (string.IsNullOrWhiteSpace(DbContextInput.Text))
            missing.Add("Nome completo do DbContext");

        if (string.IsNullOrWhiteSpace(GetTargetPath(_resolvedSettings, provider)))
            missing.Add($"Projeto de Migrations para {provider}");

        if (missing.Count > 0)
        {
            errorMessage = "Os campos abaixo nao podem ficar em branco:\n- " + string.Join("\n- ", missing);
            return false;
        }

        if (!TryValidateAdditionalArgs(_resolvedSettings.AdditionalArgs, out var argsError))
        {
            errorMessage = argsError;
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static bool TryValidateAdditionalArgs(string? args, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(args))
            return true;

        var blocked = BlockedMigrationsArgs.Where(token => ContainsArgToken(args, token)).Distinct().ToList();
        if (blocked.Count == 0)
            return true;

        error = "Argumentos adicionais invalidos para Migrations:\n- "
            + string.Join("\n- ", blocked)
            + "\n\nUse os campos proprios para Projeto/Startup/DbContext.";
        return false;
    }

    private static bool ContainsArgToken(string args, string token)
    {
        var pattern = $@"(?<!\S){Regex.Escape(token)}(?=\s|$|=)";
        return Regex.IsMatch(args, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static void NormalizeMigrationsSettings(MigrationsSettings settings)
    {
        settings.Targets ??= new List<MigrationTarget>();
        EnsureTarget(settings, DatabaseProvider.SqlServer);
        EnsureTarget(settings, DatabaseProvider.Sqlite);
    }

    private static MigrationTarget EnsureTarget(MigrationsSettings settings, DatabaseProvider provider)
    {
        var target = settings.Targets.FirstOrDefault(item => item.Provider == provider);
        if (target is not null)
            return target;

        target = new MigrationTarget
        {
            Provider = provider,
            MigrationsProjectPath = string.Empty
        };
        settings.Targets.Add(target);
        return target;
    }

    private static string GetTargetPath(MigrationsSettings settings, DatabaseProvider provider)
    {
        return EnsureTarget(settings, provider).MigrationsProjectPath;
    }

    private static void SetTargetPath(MigrationsSettings settings, DatabaseProvider provider, string path)
    {
        EnsureTarget(settings, provider).MigrationsProjectPath = path ?? string.Empty;
    }
}


