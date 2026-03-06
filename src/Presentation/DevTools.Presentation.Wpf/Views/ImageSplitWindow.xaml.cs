using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using DevTools.Core.Configuration;
using DevTools.Core.Models;
using DevTools.Image.Engine;
using DevTools.Image.Models;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Views;

public partial class ImageSplitWindow : Window
{
    private readonly JobManager _jobManager;
    private readonly SettingsService _settingsService;
    private readonly ToolConfigurationManager _toolConfigurationManager;

    public ImageSplitWindow(JobManager jobManager, SettingsService settingsService, ToolConfigurationManager toolConfigurationManager)
    {
        InitializeComponent();
        _jobManager = jobManager;
        _settingsService = settingsService;
        _toolConfigurationManager = toolConfigurationManager;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyDefaultConfiguration();

        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastImageSplitInputPath))
            InputPathSelector.SelectedPath = _settingsService.Settings.LastImageSplitInputPath;

        if (!string.IsNullOrWhiteSpace(_settingsService.Settings.LastImageSplitOutputDir))
            OutputPathSelector.SelectedPath = _settingsService.Settings.LastImageSplitOutputDir;
    }

    private void ApplyDefaultConfiguration()
    {
        var configuration = _toolConfigurationManager.GetDefaultConfiguration("ImageSplitter");
        if (configuration == null)
            return;

        if (configuration.Options.TryGetValue("input-path", out var inputPath) && !string.IsNullOrWhiteSpace(inputPath))
            InputPathSelector.SelectedPath = inputPath;

        if (configuration.Options.TryGetValue("output-path", out var outputPath) && !string.IsNullOrWhiteSpace(outputPath))
            OutputPathSelector.SelectedPath = outputPath;

        if (configuration.Options.TryGetValue("alpha-threshold", out var alphaThreshold) && !string.IsNullOrWhiteSpace(alphaThreshold))
            AlphaBox.Text = alphaThreshold;

        if (configuration.Options.TryGetValue("min-size", out var minSize) && !string.IsNullOrWhiteSpace(minSize))
            MinSizeBox.Text = minSize;

        if (configuration.Options.TryGetValue("overwrite", out var overwrite) && bool.TryParse(overwrite, out var parsedOverwrite))
            OverwriteCheck.IsChecked = parsedOverwrite;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // if (e.ButtonState == MouseButtonState.Pressed)
        //    DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BrowseInput_Click(object sender, RoutedEventArgs e) { }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e) { }

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
        if (!ValidateInputs(out var errorMessage))
        {
            ValidationUiService.ShowInline(MainFrame, errorMessage);
            return;
        }

        ValidationUiService.ClearInline(MainFrame);

        var inputPath = InputPathSelector.SelectedPath;
        var outputPath = OutputPathSelector.SelectedPath;

        _ = byte.TryParse(AlphaBox.Text, out var alpha);
        _ = int.TryParse(MinSizeBox.Text, out var minSize);
        var overwrite = OverwriteCheck.IsChecked ?? false;

        Close();

        _settingsService.Settings.LastImageSplitInputPath = inputPath;
        _settingsService.Settings.LastImageSplitOutputDir = outputPath;
        _settingsService.Save();

        _jobManager.StartJob("ImageSplitter", async (reporter, ct) =>
        {
            var engine = new ImageSplitEngine();
            var request = new ImageSplitRequest(
                InputPath: inputPath,
                OutputDirectory: outputPath,
                AlphaThreshold: alpha,
                MinRegionWidth: minSize,
                MinRegionHeight: minSize,
                Overwrite: overwrite
            );

            var result = await engine.ExecuteAsync(request, reporter, ct);

            if (result.IsSuccess && result.Value is not null)
            {
                var total = result.Value.TotalComponents;
                var saved = result.Value.Outputs.Count;
                var dir = result.Value.OutputDirectory;

                if (saved > 0)
                    return $"Recorte concluido! {saved} parte(s) salva(s) em: {dir}";

                return $"Nenhuma parte salva. Detectadas {total}. Dica: habilite 'Sobrescrever' ou ajuste Alpha/Tamanho/StartIndex. Pasta: {dir}";
            }

            return $"Falha no recorte: {string.Join(", ", result.Errors.Select(e => e.Message))}";
        });
    }

    private bool ValidateInputs(out string errorMessage)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(InputPathSelector.SelectedPath))
            missing.Add("Imagem de Entrada");

        if (string.IsNullOrWhiteSpace(MinSizeBox.Text))
            missing.Add("Tamanho Minimo");

        if (string.IsNullOrWhiteSpace(AlphaBox.Text))
            missing.Add("Alpha");

        if (missing.Count > 0)
        {
            errorMessage = "Os campos abaixo nao podem ficar em branco:\n- " + string.Join("\n- ", missing);
            return false;
        }

        if (!int.TryParse(MinSizeBox.Text, out _))
        {
            errorMessage = "Tamanho minimo deve ser um numero inteiro valido.";
            return false;
        }

        if (!byte.TryParse(AlphaBox.Text, out _))
        {
            errorMessage = "Alpha deve ser um numero entre 0 e 255.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
