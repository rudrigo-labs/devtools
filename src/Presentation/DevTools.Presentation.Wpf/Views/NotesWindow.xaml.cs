using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevTools.Notes.Engine;
using DevTools.Notes.Models;
using DevTools.Presentation.Wpf.Models;
using DevTools.Presentation.Wpf.Services;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Views
{
    public partial class NotesWindow : Window
    {
        private readonly SettingsService _settings;
        private readonly ConfigService _config;
        private readonly GoogleDriveService _googleDriveService;
        private readonly NotesEngine _engine;
        private string? _currentNoteKey; 

        private NotesSettings GetNotesSettings() => _config.GetSection<NotesSettings>("Notes") ?? new();
        private GoogleDriveSettings GetGDriveSettings() => _config.GetSection<GoogleDriveSettings>("GoogleDrive") ?? new();

        public NotesWindow(SettingsService settings, GoogleDriveService googleDriveService, ConfigService config)
        {
            InitializeComponent();
            _settings = settings;
            _config = config;
            _googleDriveService = googleDriveService;
            _engine = new NotesEngine();
            
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            CheckGoogleDriveStatus();
            await LoadList();
        }

        private void CheckGoogleDriveStatus()
        {
            var gdrive = GetGDriveSettings();
            bool driveEnabled = gdrive != null && gdrive.IsEnabled && !string.IsNullOrEmpty(gdrive.ClientId);
            
            CloudSyncButton.Visibility = driveEnabled ? Visibility.Visible : Visibility.Collapsed;
            CloudUploadNoteButton.Visibility = driveEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void CloudUploadNoteButton_Click(object sender, RoutedEventArgs e)
        {
            await UploadCurrentNoteAsync(showSuccessMessage: true);
        }

        private async void CloudSyncButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = GetGDriveSettings();
                if (!settings.IsEnabled) return;

                UiMessageService.ShowInfo("Sincronizando notas com o Google Drive...", "Google Drive");
                
                var notesSettings = GetNotesSettings();
                string storagePath = string.IsNullOrEmpty(notesSettings.StoragePath) ? _settings.Settings.NotesStoragePath : notesSettings.StoragePath;

                // Buscar todas as notas
                var request = new NotesRequest(
                    Action: NotesAction.ListItems, 
                    NotesRootPath: storagePath
                );

                var listResult = await _engine.ExecuteAsync(request);
                if (!listResult.IsSuccess || listResult.Value?.ListResult == null)
                {
                    UiMessageService.ShowError("Não foi possível carregar a lista de notas para sincronização.", "Erro");
                    return;
                }

                int synced = 0;
                int failed = 0;

                foreach (var item in listResult.Value.ListResult.Items)
                {
                    var readRequest = new NotesRequest(
                        Action: NotesAction.LoadNote,
                        NotesRootPath: storagePath,
                        NoteKey: item.FileName
                    );
                    var readResult = await _engine.ExecuteAsync(readRequest);
                    if (readResult.IsSuccess && readResult.Value?.ReadResult != null)
                    {
                        try
                        {
                            await _googleDriveService.UploadNoteAsync(
                                readResult.Value.ReadResult.Content ?? string.Empty,
                                item.FileName,
                                settings);
                            synced++;
                        }
                        catch
                        {
                            failed++;
                        }
                    }
                }
                
                UiMessageService.ShowInfo($"Sincronização concluída. Sucesso: {synced} | Falhas: {failed}", "Google Drive");
            }
            catch (Exception ex)
            {
                UiMessageService.ShowError($"Erro ao sincronizar com Google Drive: {ex.Message}", "Erro Sync");
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async Task LoadList()
        {
            var notesSettings = GetNotesSettings();
            string storagePath = string.IsNullOrEmpty(notesSettings.StoragePath) ? _settings.Settings.NotesStoragePath : notesSettings.StoragePath;

            var request = new NotesRequest(
                Action: NotesAction.ListItems, 
                NotesRootPath: storagePath
            );

            var result = await _engine.ExecuteAsync(request);
            if (result.IsSuccess && result.Value?.ListResult != null)
            {
                NotesList.ItemsSource = result.Value.ListResult.Items;
            }
            else
            {
                NotesList.ItemsSource = new List<NoteListItem>();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _currentNoteKey = null;
            NoteTitle.Text = "";
            NotesContent.Text = "";
            ShowEditMode(true);
        }

        private async void NotesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (NotesList.SelectedItem is NoteListItem item)
            {
                _currentNoteKey = item.FileName; 
                
                var notesSettings = GetNotesSettings();
                string storagePath = string.IsNullOrEmpty(notesSettings.StoragePath) ? _settings.Settings.NotesStoragePath : notesSettings.StoragePath;

                var request = new NotesRequest(
                    Action: NotesAction.LoadNote, 
                    NotesRootPath: storagePath,
                    NoteKey: item.FileName 
                );

                var result = await _engine.ExecuteAsync(request);
                if (result.IsSuccess && result.Value?.ReadResult != null) 
                {
                    NoteTitle.Text = item.Title; 
                    NotesContent.Text = result.Value.ReadResult.Content;
                    ShowEditMode(true);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ShowEditMode(false);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NoteTitle.Text))
            {
                UiMessageService.ShowError("O título é obrigatório.", "Erro");
                return;
            }

            var notesSettings = GetNotesSettings();
            string storagePath = string.IsNullOrEmpty(notesSettings.StoragePath) ? _settings.Settings.NotesStoragePath : notesSettings.StoragePath;
            bool useMarkdown = string.Equals(NormalizeExtension(notesSettings.DefaultFormat), ".md", StringComparison.OrdinalIgnoreCase);

            var action = _currentNoteKey == null ? NotesAction.CreateItem : NotesAction.SaveNote; 
            
            var request = new NotesRequest(
                Action: action,
                NotesRootPath: storagePath,
                Title: NoteTitle.Text,
                Content: NotesContent.Text,
                NoteKey: _currentNoteKey,
                UseMarkdown: useMarkdown
            );

            var result = await _engine.ExecuteAsync(request);
            if (result.IsSuccess)
            {
                // Atualiza key local para uploads individuais consistentes
                _currentNoteKey = result.Value?.CreateResult?.FileName ?? result.Value?.WriteResult?.Key ?? _currentNoteKey;

                // Backup automático na nuvem se habilitado
                if (notesSettings.AutoCloudSync)
                {
                    _ = UploadCurrentNoteAsync(showSuccessMessage: false);
                }

                ShowEditMode(false);
                await LoadList();
            }
            else
            {
                var error = result.Errors.FirstOrDefault()?.Message ?? "Erro desconhecido.";
                UiMessageService.ShowError($"Erro ao salvar: {error}", "Erro ao salvar nota");
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "ZIP Files (*.zip)|*.zip",
                FileName = $"Notes_Backup_{DateTime.Now:yyyyMMdd}.zip"
            };

            if (dialog.ShowDialog() == true)
            {
                var notesSettings = GetNotesSettings();
                string storagePath = string.IsNullOrEmpty(notesSettings.StoragePath) ? _settings.Settings.NotesStoragePath : notesSettings.StoragePath;

                var request = new NotesRequest(
                    Action: NotesAction.ExportZip, 
                    NotesRootPath: storagePath,
                    OutputPath: dialog.FileName 
                );

                var result = await _engine.ExecuteAsync(request);
                if (result.IsSuccess)
                {
                    UiMessageService.ShowInfo("Backup exportado com sucesso!", "Exportar");
                }
                else
                {
                    var error = result.Errors.FirstOrDefault()?.Message ?? "Erro desconhecido.";
                    UiMessageService.ShowError($"Erro ao exportar: {error}", "Erro ao exportar backup");
                }
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ZIP Files (*.zip)|*.zip"
            };

            if (dialog.ShowDialog() == true)
            {
                var notesSettings = GetNotesSettings();
                string storagePath = string.IsNullOrEmpty(notesSettings.StoragePath) ? _settings.Settings.NotesStoragePath : notesSettings.StoragePath;

                var request = new NotesRequest(
                    Action: NotesAction.ImportZip, 
                    NotesRootPath: storagePath,
                    ZipPath: dialog.FileName 
                );

                var result = await _engine.ExecuteAsync(request);
                if (result.IsSuccess)
                {
                    UiMessageService.ShowInfo("Backup importado com sucesso!", "Importar");
                    await LoadList();
                }
                else
                {
                    var error = result.Errors.FirstOrDefault()?.Message ?? "Erro desconhecido.";
                    UiMessageService.ShowError($"Erro ao importar: {error}", "Erro ao importar backup");
                }
            }
        }

        private void NotesContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Auto-save logic could go here, but for now explicit save
        }

        private void ShowEditMode(bool edit)
        {
            ListGrid.Visibility = edit ? Visibility.Collapsed : Visibility.Visible;
            EditGrid.Visibility = edit ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task UploadCurrentNoteAsync(bool showSuccessMessage)
        {
            if (string.IsNullOrWhiteSpace(NotesContent.Text))
                return;

            try
            {
                var settings = GetGDriveSettings();
                if (!settings.IsEnabled)
                    return;

                var notesSettings = GetNotesSettings();
                string fileName = ResolveUploadFileName(notesSettings);

                if (showSuccessMessage)
                    UiMessageService.ShowInfo("Subindo nota para o Google Drive...", "Google Drive");

                await _googleDriveService.UploadNoteAsync(NotesContent.Text, fileName, settings);

                if (showSuccessMessage)
                    UiMessageService.ShowInfo($"Nota '{fileName}' sincronizada com sucesso!", "Google Drive");
            }
            catch (Exception ex)
            {
                UiMessageService.ShowError($"Erro ao subir nota: {ex.Message}", "Erro Upload");
            }
        }

        private string ResolveUploadFileName(NotesSettings notesSettings)
        {
            if (!string.IsNullOrWhiteSpace(_currentNoteKey))
            {
                var keyFileName = Path.GetFileName(_currentNoteKey);
                if (!string.IsNullOrWhiteSpace(keyFileName))
                    return keyFileName;
            }

            string safeTitle = string.IsNullOrWhiteSpace(NoteTitle.Text) ? "NotaSemTitulo" : NoteTitle.Text;
            foreach (char c in Path.GetInvalidFileNameChars())
                safeTitle = safeTitle.Replace(c, '_');

            string extension = NormalizeExtension(notesSettings.DefaultFormat);
            return $"{safeTitle}{extension}";
        }

        private static string NormalizeExtension(string? extension)
        {
            if (string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase))
                return ".md";

            return ".txt";
        }
    }
}
