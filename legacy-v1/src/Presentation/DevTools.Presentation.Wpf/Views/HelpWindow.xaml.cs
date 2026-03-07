using DevTools.Presentation.Wpf.Services;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Views
{
    public partial class HelpWindow : Window
    {
        private const string PdfFileName = "Guia_Configuracao_Backup_Google_Drive.pdf";

        public HelpWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                HelpContentViewer.Document = HelpContentProvider.GetGoogleDriveHelp();
                HelpContentViewer.ScrollToHome();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar conteúdo de ajuda: {ex.Message}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DownloadPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Tenta no diretório de execução (onde o .exe está)
                string sourcePdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs", PdfFileName);

                // 2. Se não encontrar, tenta subir um nível (comum em debug de alguns ambientes)
                if (!File.Exists(sourcePdfPath))
                {
                    sourcePdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Docs", PdfFileName);
                }

                // 3. Se ainda não encontrar, tenta o caminho relativo ao projeto (fallback para dev)
                if (!File.Exists(sourcePdfPath))
                {
                    // Tenta encontrar a pasta Docs subindo até encontrar 'src'
                    string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                    while (!string.IsNullOrEmpty(currentDir) && !Directory.Exists(Path.Combine(currentDir, "Presentation")))
                    {
                        var parent = Directory.GetParent(currentDir);
                        if (parent == null) break;
                        currentDir = parent.FullName;
                    }
                    
                    if (!string.IsNullOrEmpty(currentDir))
                    {
                        sourcePdfPath = Path.Combine(currentDir, "Presentation", "DevTools.Presentation.Wpf", "Docs", PdfFileName);
                    }
                }

                if (!File.Exists(sourcePdfPath))
                {
                    UiMessageService.ShowError($"O arquivo de ajuda '{PdfFileName}' não foi encontrado.\nCertifique-se de que ele existe na pasta 'Docs' do projeto.", "Erro ao Localizar PDF");
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
                    UiMessageService.ShowInfo($"Guia salvo com sucesso em: {saveFileDialog.FileName}", "Download Concluído");
                }
            }
            catch (Exception ex)
            {
                UiMessageService.ShowError($"Erro ao tentar baixar o guia: {ex.Message}", "Erro ao Baixar PDF");
            }
        }
    }
}
