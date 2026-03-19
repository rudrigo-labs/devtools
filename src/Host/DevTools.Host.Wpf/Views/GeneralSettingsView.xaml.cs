using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using DevTools.Host.Wpf.Components;
using DevTools.Host.Wpf.Configuration;
using WpfControls = System.Windows.Controls;

namespace DevTools.Host.Wpf.Views;

public partial class GeneralSettingsView : System.Windows.Controls.UserControl
{
    private readonly AppSettingsFileService _appSettingsFileService;
    private readonly Dictionary<string, WpfControls.CheckBox> _toolVisibilityChecks;
    private readonly Dictionary<string, string> _lastKnownConfigValues;
    private bool _isLoading;
    private bool _isRevertingConfigChange;

    public GeneralSettingsView(AppSettingsFileService appSettingsFileService)
    {
        _appSettingsFileService = appSettingsFileService;
        InitializeComponent();

        _toolVisibilityChecks = new Dictionary<string, WpfControls.CheckBox>(StringComparer.OrdinalIgnoreCase)
        {
            ["Snapshot"] = SnapshotEnabledCheckBox,
            ["Rename"] = RenameEnabledCheckBox,
            ["Harvest"] = HarvestEnabledCheckBox,
            ["ImageSplit"] = ImageSplitEnabledCheckBox,
            ["SearchText"] = SearchTextEnabledCheckBox,
            ["Organizer"] = OrganizerEnabledCheckBox,
            ["Utf8Convert"] = Utf8ConvertEnabledCheckBox,
            ["Migrations"] = MigrationsEnabledCheckBox,
            ["SshTunnel"] = SshTunnelEnabledCheckBox,
            ["Ngrok"] = NgrokEnabledCheckBox,
            ["Notes"] = NotesEnabledCheckBox
        };

        _lastKnownConfigValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        WireConfigImpactAlerts();

        Loaded += GeneralSettingsView_Loaded;
    }

    private void GeneralSettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        CategoryListBox.SelectedIndex = 0;
        LoadSettingsFromFile();
    }

    private void CategoryListBox_SelectionChanged(object sender, WpfControls.SelectionChangedEventArgs e)
    {
        var sectionTag = (CategoryListBox.SelectedItem as WpfControls.ListBoxItem)?.Tag?.ToString();
        SetActiveSection(sectionTag);
    }

    private void SetActiveSection(string? sectionTag)
    {
        TextFamilyPanel.Visibility = Visibility.Collapsed;
        HistoryPanel.Visibility = Visibility.Collapsed;
        ToolsPanel.Visibility = Visibility.Collapsed;

        switch (sectionTag)
        {
            case "History":
                HistoryPanel.Visibility = Visibility.Visible;
                break;
            case "Tools":
                ToolsPanel.Visibility = Visibility.Visible;
                break;
            case "TextFamily":
            default:
                TextFamilyPanel.Visibility = Visibility.Visible;
                break;
        }
    }

    private void MaxFileSizeHelpButton_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        DevToolsMessageBox.Info(
            owner,
            "O que é:\n" +
            "Valor padrão de tamanho máximo por arquivo (em KB) para ferramentas da família de texto.\n\n" +
            "Como funciona:\n" +
            "Se a ferramenta não receber um valor específico, ela usa este número automaticamente.\n\n" +
            "Exemplo:\n" +
            "500 = arquivos até 500 KB por padrão.\n\n" +
            "Quando aumentar:\n" +
            "Quando precisar processar arquivos grandes.\n\n" +
            "Impacto:\n" +
            "Valores maiores podem aumentar tempo de processamento e uso de memória.",
            "Ajuda - Tamanho máximo padrão");
    }

    private void AbsoluteMaxHelpButton_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        DevToolsMessageBox.Info(
            owner,
            "O que é:\n" +
            "Limite máximo global (em KB) que o sistema permite.\n\n" +
            "Como funciona:\n" +
            "Mesmo que alguém informe um valor maior em uma ferramenta, o sistema bloqueia.\n\n" +
            "Regra obrigatória:\n" +
            "Este valor deve ser maior ou igual ao 'Tamanho máximo padrão (KB)'.\n\n" +
            "Exemplo:\n" +
            "Padrão = 500 e Teto = 10000.\n\n" +
            "Impacto:\n" +
            "Se reduzir demais, execuções válidas podem ser impedidas.",
            "Ajuda - Teto absoluto");
    }

    private void DefaultIncludeGlobsHelpButton_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        DevToolsMessageBox.Info(
            owner,
            "O que é:\n" +
            "Lista de padrões (1 por linha) para dizer quais arquivos entram por padrão.\n\n" +
            "Onde é usado:\n" +
            "Search Text e UTF-8 Convert carregam estes valores automaticamente.\n\n" +
            "Exemplos úteis:\n" +
            "**/*\n" +
            "**/*.cs\n" +
            "**/*.json\n\n" +
            "Dica prática:\n" +
            "Se quiser incluir tudo, use **/* e refine no campo de exclusão.",
            "Ajuda - Globs padrão de inclusão");
    }

    private void DefaultExcludeGlobsHelpButton_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        DevToolsMessageBox.Info(
            owner,
            "O que é:\n" +
            "Lista de padrões (1 por linha) para remover arquivos/pastas da varredura padrão.\n\n" +
            "Prioridade:\n" +
            "A exclusão vence a inclusão quando um caminho bate nos dois filtros.\n\n" +
            "Exemplos recomendados:\n" +
            "**/bin/**\n" +
            "**/obj/**\n" +
            "**/.git/**\n" +
            "**/node_modules/**\n\n" +
            "Impacto:\n" +
            "Exclusões corretas deixam a execução mais rápida e evitam ruído.",
            "Ajuda - Globs padrão de exclusão");
    }

    private void HistoryEnabledHelpButton_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        DevToolsMessageBox.Info(
            owner,
            "O que é:\n" +
            "Liga ou desliga o histórico global das ferramentas.\n\n" +
            "Quando ligado:\n" +
            "As execuções ficam registradas e o botão de histórico aparece nas telas.\n\n" +
            "Quando desligado:\n" +
            "Novas execuções não são registradas e o histórico deixa de aparecer.\n\n" +
            "Use desligado quando:\n" +
            "Você não quiser armazenar rastros locais de uso.",
            "Ajuda - Usar histórico");
    }

    private void ToolEnabledHelpButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfControls.Button button)
            return;

        var toolTag = button.Tag?.ToString() ?? string.Empty;
        var owner = Window.GetWindow(this);

        var toolDisplayName = GetToolDisplayName(toolTag);
        var message =
            $"O que este campo faz:\n" +
            $"Liga/desliga a exibição da ferramenta '{toolDisplayName}' no aplicativo.\n\n" +
            "Quando desmarcado:\n" +
            "- some da navegação de execução\n" +
            "- some da navegação de configuração\n\n" +
            "Quando marcado:\n" +
            "- volta a aparecer normalmente\n\n" +
            "Aplicação da mudança:\n" +
            "Salve e recarregue o aplicativo para refletir em toda a interface.";

        DevToolsMessageBox.Info(owner, message, $"Ajuda - {toolDisplayName}");
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        RestartApplication();
    }

    private void WireConfigImpactAlerts()
    {
        MaxFileSizeKbTextBox.LostFocus += (_, _) =>
            ConfirmTextConfigImpact(
                "FileTools.MaxFileSizeKb",
                MaxFileSizeKbTextBox,
                "Tamanho máximo padrão alterado",
                "Impacto: altera o limite padrão de leitura de arquivos para as ferramentas de varredura. " +
                "Valores maiores podem aumentar consumo de memória e tempo de processamento.");

        AbsoluteMaxFileSizeKbTextBox.LostFocus += (_, _) =>
            ConfirmTextConfigImpact(
                "FileTools.AbsoluteMaxFileSizeKb",
                AbsoluteMaxFileSizeKbTextBox,
                "Teto absoluto alterado",
                "Impacto: muda o teto global aceito pelo sistema. " +
                "Valores menores podem bloquear execuções que antes eram permitidas.");

        DefaultIncludeGlobsTextBox.LostFocus += (_, _) =>
            ConfirmTextConfigImpact(
                "FileTools.DefaultIncludeGlobs",
                DefaultIncludeGlobsTextBox,
                "Globs padrão de inclusão alterados",
                "Impacto: muda o filtro padrão de inclusão da família de texto em ferramentas como Search Text e UTF-8 Convert.");

        DefaultExcludeGlobsTextBox.LostFocus += (_, _) =>
            ConfirmTextConfigImpact(
                "FileTools.DefaultExcludeGlobs",
                DefaultExcludeGlobsTextBox,
                "Globs padrão de exclusão alterados",
                "Impacto: muda o filtro padrão de exclusão da família de texto em ferramentas como Search Text e UTF-8 Convert.");

        HistoryEnabledCheckBox.Checked += (_, _) =>
            ConfirmBooleanConfigImpact(
                "History.Enabled",
                HistoryEnabledCheckBox,
                "Histórico ativado",
                "Impacto: as ferramentas voltam a exibir histórico e registrar novas execuções.");

        HistoryEnabledCheckBox.Unchecked += (_, _) =>
            ConfirmBooleanConfigImpact(
                "History.Enabled",
                HistoryEnabledCheckBox,
                "Histórico desativado",
                "Impacto: o histórico deixa de aparecer nas ferramentas e novas execuções não serão registradas.");

        foreach (var (toolTag, checkBox) in _toolVisibilityChecks)
        {
            checkBox.Checked += (_, _) =>
                ConfirmBooleanConfigImpact(
                    $"ToolVisibility.{toolTag}",
                    checkBox,
                    $"Ferramenta '{GetToolDisplayName(toolTag)}' habilitada",
                    "Impacto: a ferramenta volta a aparecer na navegação após recarregar a aplicação.");

            checkBox.Unchecked += (_, _) =>
                ConfirmBooleanConfigImpact(
                    $"ToolVisibility.{toolTag}",
                    checkBox,
                    $"Ferramenta '{GetToolDisplayName(toolTag)}' desabilitada",
                    "Impacto: a ferramenta deixará de aparecer na navegação após recarregar a aplicação.");
        }
    }

    private void ConfirmTextConfigImpact(
        string configKey,
        WpfControls.TextBox textBox,
        string title,
        string impactMessage)
    {
        if (_isLoading || _isRevertingConfigChange)
            return;

        var currentValue = NormalizeMultilineText(textBox.Text);
        if (!_lastKnownConfigValues.TryGetValue(configKey, out var previousValue))
        {
            _lastKnownConfigValues[configKey] = currentValue;
            return;
        }

        if (string.Equals(previousValue, currentValue, StringComparison.Ordinal))
            return;

        if (ConfirmConfigImpact(title, impactMessage))
        {
            _lastKnownConfigValues[configKey] = currentValue;
            return;
        }

        _isRevertingConfigChange = true;
        try
        {
            textBox.Text = previousValue;
        }
        finally
        {
            _isRevertingConfigChange = false;
        }
    }

    private void ConfirmBooleanConfigImpact(
        string configKey,
        WpfControls.CheckBox checkBox,
        string title,
        string impactMessage)
    {
        if (_isLoading || _isRevertingConfigChange)
            return;

        var currentValue = (checkBox.IsChecked == true).ToString();
        if (!_lastKnownConfigValues.TryGetValue(configKey, out var previousValue))
        {
            _lastKnownConfigValues[configKey] = currentValue;
            return;
        }

        if (string.Equals(previousValue, currentValue, StringComparison.Ordinal))
            return;

        if (ConfirmConfigImpact(title, impactMessage))
        {
            _lastKnownConfigValues[configKey] = currentValue;
            return;
        }

        var previousCheckedValue = bool.TryParse(previousValue, out var parsedPrevious) && parsedPrevious;
        _isRevertingConfigChange = true;
        try
        {
            checkBox.IsChecked = previousCheckedValue;
        }
        finally
        {
            _isRevertingConfigChange = false;
        }
    }

    private bool ConfirmConfigImpact(string title, string impactMessage)
    {
        var owner = Window.GetWindow(this);
        var result = DevToolsMessageBox.Confirm(
            owner,
            $"{impactMessage}\n\nDeseja manter esta alteração?\n\nEla será persistida ao clicar em 'Salvar' e aplicada completamente após 'Recarregar'.",
            title);

        return result == DevToolsMessageBoxResult.Yes;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadAndValidateInputs(
                out var maxFileSizeKb,
                out var absoluteMaxFileSizeKb,
                out var defaultIncludeGlobs,
                out var defaultExcludeGlobs,
                out var historyEnabled,
                out var disabledTools,
                out var error))
        {
            SetStatus(error, isError: true);
            return;
        }

        try
        {
            _appSettingsFileService.SaveGeneralSettings(
                maxFileSizeKb,
                absoluteMaxFileSizeKb,
                defaultIncludeGlobs,
                defaultExcludeGlobs,
                historyEnabled,
                disabledTools);

            SetStatus("Configurações salvas com sucesso. Use 'Recarregar' para reiniciar e aplicar tudo.");
        }
        catch (Exception ex)
        {
            SetStatus($"Falha ao salvar appsettings.json: {ex.Message}", isError: true);
        }
    }

    private void LoadSettingsFromFile()
    {
        _isLoading = true;
        try
        {
            var snapshot = _appSettingsFileService.Load();
            MaxFileSizeKbTextBox.Text = snapshot.Settings.FileTools.MaxFileSizeKb.ToString(CultureInfo.InvariantCulture);
            AbsoluteMaxFileSizeKbTextBox.Text = snapshot.Settings.FileTools.AbsoluteMaxFileSizeKb.ToString(CultureInfo.InvariantCulture);
            DefaultIncludeGlobsTextBox.Text = string.Join(Environment.NewLine, snapshot.Settings.FileTools.DefaultIncludeGlobs);
            DefaultExcludeGlobsTextBox.Text = string.Join(Environment.NewLine, snapshot.Settings.FileTools.DefaultExcludeGlobs);
            HistoryEnabledCheckBox.IsChecked = snapshot.Settings.History.Enabled;

            foreach (var check in _toolVisibilityChecks.Values)
                check.IsChecked = true;

            foreach (var disabledTool in snapshot.Settings.ToolVisibility.DisabledTools)
            {
                if (_toolVisibilityChecks.TryGetValue(disabledTool, out var checkBox))
                    checkBox.IsChecked = false;
            }

            CaptureCurrentConfigValues();

            SettingsPathTextBlock.Text = snapshot.FilePath;

            if (snapshot.ParseError)
            {
                SetStatus("O appsettings.json estava inválido. Os valores exibidos foram restaurados para o padrão.", isError: true);
                return;
            }

            if (!snapshot.FileExists)
            {
                SetStatus("appsettings.json não encontrado na pasta do executável. Ele será criado ao salvar.");
                return;
            }

            SetStatus("Configurações carregadas.");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private bool TryReadAndValidateInputs(
        out int maxFileSizeKb,
        out int absoluteMaxFileSizeKb,
        out IReadOnlyCollection<string> defaultIncludeGlobs,
        out IReadOnlyCollection<string> defaultExcludeGlobs,
        out bool historyEnabled,
        out IReadOnlyCollection<string> disabledTools,
        out string error)
    {
        maxFileSizeKb = 0;
        absoluteMaxFileSizeKb = 0;
        defaultIncludeGlobs = Array.Empty<string>();
        defaultExcludeGlobs = Array.Empty<string>();
        historyEnabled = true;
        disabledTools = Array.Empty<string>();
        error = string.Empty;

        if (_isLoading)
        {
            error = "Aguarde o carregamento das configurações.";
            return false;
        }

        if (!int.TryParse(MaxFileSizeKbTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxFileSizeKb))
        {
            error = "Informe um número inteiro válido em 'Tamanho máximo padrão (KB)'.";
            return false;
        }

        if (!int.TryParse(AbsoluteMaxFileSizeKbTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out absoluteMaxFileSizeKb))
        {
            error = "Informe um número inteiro válido em 'Teto absoluto (KB)'.";
            return false;
        }

        if (maxFileSizeKb <= 0)
        {
            error = "'Tamanho máximo padrão (KB)' deve ser maior que zero.";
            return false;
        }

        if (absoluteMaxFileSizeKb <= 0)
        {
            error = "'Teto absoluto (KB)' deve ser maior que zero.";
            return false;
        }

        if (absoluteMaxFileSizeKb < maxFileSizeKb)
        {
            error = "'Teto absoluto (KB)' deve ser maior ou igual ao 'Tamanho máximo padrão (KB)'.";
            return false;
        }

        defaultIncludeGlobs = ParseGlobLines(DefaultIncludeGlobsTextBox.Text);
        if (defaultIncludeGlobs.Count == 0)
            defaultIncludeGlobs = ["**/*"];

        defaultExcludeGlobs = ParseGlobLines(DefaultExcludeGlobsTextBox.Text);

        historyEnabled = HistoryEnabledCheckBox.IsChecked == true;

        var disabledToolTags = _toolVisibilityChecks
            .Where(x => x.Value.IsChecked != true)
            .Select(x => x.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (disabledToolTags.Length == _toolVisibilityChecks.Count)
        {
            error = "Mantenha ao menos uma ferramenta habilitada.";
            return false;
        }

        disabledTools = disabledToolTags;
        return true;
    }

    private void CaptureCurrentConfigValues()
    {
        _lastKnownConfigValues["FileTools.MaxFileSizeKb"] = NormalizeMultilineText(MaxFileSizeKbTextBox.Text);
        _lastKnownConfigValues["FileTools.AbsoluteMaxFileSizeKb"] = NormalizeMultilineText(AbsoluteMaxFileSizeKbTextBox.Text);
        _lastKnownConfigValues["FileTools.DefaultIncludeGlobs"] = NormalizeMultilineText(DefaultIncludeGlobsTextBox.Text);
        _lastKnownConfigValues["FileTools.DefaultExcludeGlobs"] = NormalizeMultilineText(DefaultExcludeGlobsTextBox.Text);
        _lastKnownConfigValues["History.Enabled"] = (HistoryEnabledCheckBox.IsChecked == true).ToString();

        foreach (var (toolTag, checkBox) in _toolVisibilityChecks)
            _lastKnownConfigValues[$"ToolVisibility.{toolTag}"] = (checkBox.IsChecked == true).ToString();
    }

    private static IReadOnlyCollection<string> ParseGlobLines(string rawText)
        => rawText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string NormalizeMultilineText(string rawText)
    {
        var lines = rawText
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None)
            .Select(x => x.TrimEnd())
            .ToArray();

        return string.Join(Environment.NewLine, lines).Trim();
    }

    private static string GetToolDisplayName(string toolTag)
        => toolTag switch
        {
            "ImageSplit" => "Image Split",
            "SearchText" => "Search Text",
            "Utf8Convert" => "UTF8 Convert",
            "SshTunnel" => "SSH Tunnel",
            _ => toolTag
        };

    private void RestartApplication()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath))
                throw new InvalidOperationException("Não foi possível identificar o executável atual.");

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory
            };

            Process.Start(startInfo);
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            SetStatus($"Falha ao reiniciar a aplicação: {ex.Message}", isError: true);
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Foreground = isError
            ? (System.Windows.Media.Brush)FindResource("DevToolsStatusError")
            : (System.Windows.Media.Brush)FindResource("DevToolsTextSecondary");
    }
}
