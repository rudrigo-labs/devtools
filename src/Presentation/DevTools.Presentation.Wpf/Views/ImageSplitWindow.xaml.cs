using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using DevTools.Image.Engine;
using DevTools.Image.Models;
using DevTools.Presentation.Wpf.Services;
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

        ProfileSelector.GetOptionsFunc = GetCurrentOptions;
        ProfileSelector.ProfileLoaded += LoadProfile;

        // Restore Settings
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastImageSplitInputPath))
            InputPathBox.Text = _settingsService.Settings.LastImageSplitInputPath;
        
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastImageSplitOutputDir))
            OutputPathBox.Text = _settingsService.Settings.LastImageSplitOutputDir;

        // Restore Position
        /* Position handled by TrayService
        if (_settingsService.Settings.ImageSplitWindowTop.HasValue)
        {
            Top = _settingsService.Settings.ImageSplitWindowTop.Value;
            Left = _settingsService.Settings.ImageSplitWindowLeft.Value;
        }
        else
        {
            // Default: Bottom-Right
            var screen = SystemParameters.WorkArea;
            Left = screen.Right - Width - 20;
            Top = screen.Bottom - Height - 20;
        }

        // Safety check
        var workArea = SystemParameters.WorkArea;
        if (Top < 0 || Top > workArea.Height) Top = workArea.Height - Height - 20;
        if (Left < 0 || Left > workArea.Width) Left = workArea.Width - Width - 20;
        */

        // Auto-Save Position on Close
        /*
        Closed += (s, e) =>
        {
            _settingsService.Settings.ImageSplitWindowTop = Top;
            _settingsService.Settings.ImageSplitWindowLeft = Left;
            _settingsService.Save();
        };
        */
    }

    private Dictionary<string, string> GetCurrentOptions()
    {
        var options = new Dictionary<string, string>();
        options["input"] = InputPathBox.Text;
        options["output"] = OutputPathBox.Text;
        options["alpha"] = AlphaBox.Text;
        options["min-w"] = MinSizeBox.Text;
        options["min-h"] = MinSizeBox.Text; // UI uses single size box for both
        options["overwrite"] = (OverwriteCheck.IsChecked ?? false).ToString().ToLowerInvariant();
        return options;
    }

    private void LoadProfile(ToolProfile profile)
    {
        if (profile.Options.TryGetValue("input", out var input)) InputPathBox.Text = input;
        else if (profile.Options.TryGetValue("file", out var file)) InputPathBox.Text = file;
        
        if (profile.Options.TryGetValue("output", out var output)) OutputPathBox.Text = output;
        
        if (profile.Options.TryGetValue("alpha", out var alpha)) AlphaBox.Text = alpha;
        else if (profile.Options.TryGetValue("threshold", out var threshold)) AlphaBox.Text = threshold;
        
        if (profile.Options.TryGetValue("min-w", out var minW)) MinSizeBox.Text = minW;
        else if (profile.Options.TryGetValue("width", out var width)) MinSizeBox.Text = width;
        
        if (profile.Options.TryGetValue("overwrite", out var overwrite))
             OverwriteCheck.IsChecked = bool.TryParse(overwrite, out var o) ? o : false;
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

    private void BrowseInput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Imagens (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Todos os Arquivos (*.*)|*.*",
            Title = "Selecione a Imagem para Recortar"
        };

        if (dlg.ShowDialog() == true)
        {
            InputPathBox.Text = dlg.FileName;
            _settingsService.Settings.LastImageSplitInputPath = dlg.FileName;
            _settingsService.Save();
        }
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Selecione a Pasta de Saída"
        };

        if (dlg.ShowDialog() == true)
        {
            OutputPathBox.Text = dlg.FolderName;
            _settingsService.Settings.LastImageSplitOutputDir = dlg.FolderName;
            _settingsService.Save();
        }
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
        var inputPath = InputPathBox.Text;
        var outputPath = OutputPathBox.Text;
        
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            MessageBox.Show("Por favor, selecione uma imagem de entrada.", "Campo Obrigatório", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!byte.TryParse(AlphaBox.Text, out var alpha)) alpha = 10;
        if (!int.TryParse(MinSizeBox.Text, out var minSize)) minSize = 3;
        var overwrite = OverwriteCheck.IsChecked ?? false;

        // Close window to start job
        Close();

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

            return result.IsSuccess
                ? $"Recorte concluído! {result.Value?.TotalComponents ?? 0} partes geradas."
                : $"Falha no recorte: {string.Join(", ", result.Errors.Select(e => e.Message))}";
        });
    }
}
