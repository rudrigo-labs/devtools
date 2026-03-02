using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DevTools.Core.Models;
using DevTools.Organizer.Engine;
using DevTools.Organizer.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Utilities;

namespace DevTools.Presentation.Wpf.Views;

public partial class OrganizerWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;

    public OrganizerWindow(JobManager jobManager, SettingsService settingsService)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastOrganizerInputPath))
            InputPathSelector.SelectedPath = _settingsService.Settings.LastOrganizerInputPath;
        else
            InputPathSelector.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        Loaded += (s, e) =>
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - ActualWidth - 20;
            Top = desktopWorkingArea.Bottom - ActualHeight - 20;
            Activate();
        };

        Deactivated += (s, e) =>
        {
        };
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
        var inputPath = InputPathSelector.SelectedPath;
        var outputPath = OutputPathSelector.SelectedPath;
        var simulate = SimulateCheck.IsChecked ?? false;

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            DevToolsMessage.Warning("Por favor, selecione uma pasta de entrada.", "Erro");
            return;
        }

        if (!System.IO.Directory.Exists(inputPath))
        {
            DevToolsMessage.Error("O diretório de entrada especificado não existe.", "Diretório Inválido");
            return;
        }

        if (!string.IsNullOrWhiteSpace(outputPath) && !System.IO.Directory.Exists(outputPath))
        {
            DevToolsMessage.Error("O diretório de saída especificado não existe.", "Diretório Inválido");
            return;
        }

        _settingsService.Settings.LastOrganizerInputPath = inputPath;
        _settingsService.Save();

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = inputPath;
        }

        Close();

        _jobManager.StartJob("Organizer", async (reporter, ct) =>
        {
            try
            {
                var engine = new OrganizerEngine();
                var request = new OrganizerRequest(inputPath, outputPath, null, null, !simulate);

                var result = await engine.ExecuteAsync(request, reporter, ct);

                if (!result.IsSuccess)
                {
                    return $"Erro ao organizar: {string.Join(", ", result.Errors.Select(e => e.Message))}";
                }

                var stats = result.Value.Stats;
                return $"Organização concluída! Movidos: {stats.WouldMove}, Falhas: {stats.Errors}, Duplicados: {stats.Duplicates}";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Erro crítico ao executar Organizer", ex);
                return $"Erro crítico: {ex.Message}";
            }
        });
    }
}

