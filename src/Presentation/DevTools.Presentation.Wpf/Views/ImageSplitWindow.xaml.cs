using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using DevTools.Image.Engine;
using DevTools.Image.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Utilities;
using DevTools.Core.Models;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Views;

public partial class ImageSplitWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;

    public ImageSplitWindow(JobManager jobManager, SettingsService settingsService)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastImageSplitInputPath))
            InputPathSelector.SelectedPath = _settingsService.Settings.LastImageSplitInputPath;
        
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastImageSplitOutputDir))
            OutputPathSelector.SelectedPath = _settingsService.Settings.LastImageSplitOutputDir;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsTextAllowed(e.Text);
    }

    private static bool IsTextAllowed(string text)
    {
        return Regex.IsMatch(text, "[0-9]+");
    }

    private void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        var inputPath = InputPathSelector.SelectedPath;
        var outputPath = OutputPathSelector.SelectedPath;
        
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            DevToolsMessage.Warning("Por favor, selecione uma imagem de entrada.", "Campo Obrigatório");
            return;
        }

        if (!System.IO.File.Exists(inputPath))
        {
            DevToolsMessage.Error("O arquivo de entrada especificado não existe.", "Arquivo Inválido");
            return;
        }

        if (!string.IsNullOrWhiteSpace(outputPath) && !System.IO.Directory.Exists(outputPath))
        {
            DevToolsMessage.Error("O diretório de saída especificado não existe.", "Diretório Inválido");
            return;
        }

        if (!byte.TryParse(AlphaBox.Text, out var alpha)) alpha = 10;
        if (!int.TryParse(MinSizeBox.Text, out var minSize)) minSize = 3;
        var overwrite = OverwriteCheck.IsChecked ?? false;

        _settingsService.Settings.LastImageSplitInputPath = inputPath;
        _settingsService.Settings.LastImageSplitOutputDir = outputPath;
        _settingsService.Save();

        Close();

        _jobManager.StartJob("ImageSplitter", async (reporter, ct) =>
        {
            try
            {
                var engine = new ImageSplitEngine();
                var request = new ImageSplitRequest(
                    InputPath: inputPath,
                    OutputDirectory: string.IsNullOrWhiteSpace(outputPath) ? null : outputPath,
                    AlphaThreshold: alpha,
                    MinRegionWidth: minSize,
                    MinRegionHeight: minSize,
                    Overwrite: overwrite
                );

                var result = await engine.ExecuteAsync(request, reporter, ct);

                return result.IsSuccess
                    ? $"Recorte concluído! {result.Value?.TotalComponents ?? 0} partes geradas."
                    : $"Falha no recorte: {string.Join(", ", result.Errors.Select(e => e.Message))}";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Erro crítico ao executar ImageSplitter", ex);
                return $"Erro crítico: {ex.Message}";
            }
        });
    }
}
