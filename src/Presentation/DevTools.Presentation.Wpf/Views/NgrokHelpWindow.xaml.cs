using DevTools.Presentation.Wpf.Services;
using DevTools.Ngrok.Services;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace DevTools.Presentation.Wpf.Views;

public partial class NgrokHelpWindow : Window
{
    private const string PdfFileName = "Guia_Configuracao_Ngrok.pdf";
    private readonly NgrokOnboardingService _onboardingService;

    public NgrokHelpWindow()
    {
        InitializeComponent();
        _onboardingService = new NgrokOnboardingService();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        HelpContentViewer.Document = NgrokHelpContentProvider.GetHelp();
        HelpContentViewer.ScrollToHome();
    }

    private void OpenTokenPage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(_onboardingService.GetTokenPageUrl()) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError($"Nao foi possivel abrir a pagina do token.\n{ex.Message}", "Erro ao abrir link");
        }
    }

    private void DownloadPdfButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var sourcePdfPath = ResolvePdfPath();
            if (sourcePdfPath == null)
            {
                UiMessageService.ShowError($"O arquivo de ajuda '{PdfFileName}' nao foi encontrado.", "Erro ao localizar PDF");
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = PdfFileName,
                DefaultExt = ".pdf",
                Filter = "Arquivos PDF (*.pdf)|*.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                File.Copy(sourcePdfPath, saveFileDialog.FileName, true);
                UiMessageService.ShowInfo($"Guia salvo com sucesso em: {saveFileDialog.FileName}", "Download concluido");
            }
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError($"Erro ao baixar o guia: {ex.Message}", "Erro ao baixar PDF");
        }
    }

    private static string? ResolvePdfPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs", PdfFileName),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "docs", PdfFileName),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "src", "Presentation", "DevTools.Presentation.Wpf", "docs", PdfFileName)
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
