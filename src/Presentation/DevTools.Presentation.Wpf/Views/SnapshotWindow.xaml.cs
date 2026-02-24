using System.Windows;
using DevTools.Snapshot.Engine;
using DevTools.Snapshot.Models;
using DevTools.Presentation.Wpf.Services;
using DevTools.Presentation.Wpf.Utilities;
using DevTools.Core.Models;

namespace DevTools.Presentation.Wpf.Views;

public partial class SnapshotWindow : Window
{
    private readonly SettingsService _settingsService;

    public SnapshotRequest? Result { get; private set; }

    public SnapshotWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;

        ProfileSelector.GetOptionsFunc = GetCurrentOptions;
        ProfileSelector.ProfileLoaded += LoadProfile;

        // Load Settings
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastSnapshotRootPath))
            RootPathSelector.SelectedPath = _settingsService.Settings.LastSnapshotRootPath;
        
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastSnapshotOutputPath))
            OutputPathSelector.SelectedPath = _settingsService.Settings.LastSnapshotOutputPath;

        if (!string.IsNullOrEmpty(_settingsService.Settings.LastSnapshotIgnored))
            IgnoredBox.Text = _settingsService.Settings.LastSnapshotIgnored;

        if (_settingsService.Settings.LastSnapshotMaxKb.HasValue)
            MaxKbBox.Text = _settingsService.Settings.LastSnapshotMaxKb.Value.ToString();

        if (_settingsService.Settings.LastSnapshotText.HasValue)
            TextCheck.IsChecked = _settingsService.Settings.LastSnapshotText.Value;

        if (_settingsService.Settings.LastSnapshotJsonNested.HasValue)
            JsonNestedCheck.IsChecked = _settingsService.Settings.LastSnapshotJsonNested.Value;

        if (_settingsService.Settings.LastSnapshotJsonRecursive.HasValue)
            JsonRecursiveCheck.IsChecked = _settingsService.Settings.LastSnapshotJsonRecursive.Value;

        if (_settingsService.Settings.LastSnapshotHtml.HasValue)
            HtmlCheck.IsChecked = _settingsService.Settings.LastSnapshotHtml.Value;
    }

    private Dictionary<string, string> GetCurrentOptions()
    {
        return new Dictionary<string, string>
        {
            ["root"] = RootPathSelector.SelectedPath,
            ["output"] = OutputPathSelector.SelectedPath,
            ["ignore"] = IgnoredBox.Text,
            ["max-kb"] = MaxKbBox.Text,
            ["text"] = (TextCheck.IsChecked ?? true).ToString().ToLowerInvariant(),
            ["json-nested"] = (JsonNestedCheck.IsChecked ?? false).ToString().ToLowerInvariant(),
            ["json-recursive"] = (JsonRecursiveCheck.IsChecked ?? false).ToString().ToLowerInvariant(),
            ["html"] = (HtmlCheck.IsChecked ?? false).ToString().ToLowerInvariant()
        };
    }

    private void LoadProfile(ToolProfile profile)
    {
        if (profile.Options.TryGetValue("root", out var root)) RootPathSelector.SelectedPath = root;
        if (profile.Options.TryGetValue("output", out var output)) OutputPathSelector.SelectedPath = output;
        if (profile.Options.TryGetValue("ignore", out var ignore)) IgnoredBox.Text = ignore;
        if (profile.Options.TryGetValue("max-kb", out var maxKb)) MaxKbBox.Text = maxKb;
        
        if (profile.Options.TryGetValue("text", out var text)) 
            TextCheck.IsChecked = bool.TryParse(text, out var t) ? t : true;
        
        if (profile.Options.TryGetValue("json-nested", out var nested)) 
            JsonNestedCheck.IsChecked = bool.TryParse(nested, out var n) ? n : false;
        
        if (profile.Options.TryGetValue("json-recursive", out var recursive)) 
            JsonRecursiveCheck.IsChecked = bool.TryParse(recursive, out var r) ? r : false;
        
        if (profile.Options.TryGetValue("html", out var html)) 
            HtmlCheck.IsChecked = bool.TryParse(html, out var h) ? h : false;
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RootPathSelector.SelectedPath))
        {
            DevToolsMessage.Warning("Por favor, selecione um diretório raiz (Source).", "Erro de Validação");
            return;
        }

        if (!System.IO.Directory.Exists(RootPathSelector.SelectedPath))
        {
            DevToolsMessage.Error("O diretório raiz especificado não existe.", "Diretório Inválido");
            return;
        }

        if (RememberSettingsCheck.IsChecked == true)
        {
            _settingsService.Settings.LastSnapshotRootPath = RootPathSelector.SelectedPath;
            _settingsService.Settings.LastSnapshotOutputPath = OutputPathSelector.SelectedPath;
            _settingsService.Settings.LastSnapshotIgnored = IgnoredBox.Text;
            
            if (int.TryParse(MaxKbBox.Text, out var maxKb))
                _settingsService.Settings.LastSnapshotMaxKb = maxKb;

            _settingsService.Settings.LastSnapshotText = TextCheck.IsChecked;
            _settingsService.Settings.LastSnapshotJsonNested = JsonNestedCheck.IsChecked;
            _settingsService.Settings.LastSnapshotJsonRecursive = JsonRecursiveCheck.IsChecked;
            _settingsService.Settings.LastSnapshotHtml = HtmlCheck.IsChecked;
            _settingsService.Save();
        }

        var ignored = string.IsNullOrWhiteSpace(IgnoredBox.Text) 
            ? null 
            : IgnoredBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        int? maxKbVal = int.TryParse(MaxKbBox.Text, out var mk) ? mk : null;

        Result = new SnapshotRequest(
            RootPath: RootPathSelector.SelectedPath,
            OutputBasePath: OutputPathSelector.SelectedPath,
            GenerateText: TextCheck.IsChecked ?? true,
            GenerateJsonNested: JsonNestedCheck.IsChecked ?? false,
            GenerateJsonRecursive: JsonRecursiveCheck.IsChecked ?? false,
            GenerateHtmlPreview: HtmlCheck.IsChecked ?? false,
            IgnoredDirectories: ignored,
            MaxFileSizeKb: maxKbVal
        );

        var engine = new SnapshotEngine();
        
        IsEnabled = false;
        RunSummary.Clear();

        try
        {
            var result = await Task.Run(() => engine.ExecuteAsync(Result));
            RunSummary.BindResult(result);
            
            if (result.IsSuccess && HtmlCheck.IsChecked == true && result.Value.Artifacts.Any(a => a.Kind == SnapshotArtifactKind.HtmlPreview))
            {
                var htmlArtifact = result.Value.Artifacts.FirstOrDefault(a => a.Kind == SnapshotArtifactKind.HtmlPreview);
                if (htmlArtifact != null)
                {
                    // Could open preview here if we wanted
                    // Process.Start(new ProcessStartInfo { FileName = Path.Combine(htmlArtifact.Path, "index.html"), UseShellExecute = true });
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error("Erro crítico ao executar Snapshot", ex);
            DevToolsMessage.Error($"Erro crítico: {ex.Message}", "Erro");
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
