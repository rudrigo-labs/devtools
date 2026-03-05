using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DevTools.Notes.Engine;
using DevTools.Notes.Models;
using DevTools.Notes.Providers;
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

        private string ResolveNotesStoragePath(NotesSettings notesSettings)
        {
            var configuredPath = string.IsNullOrWhiteSpace(notesSettings.StoragePath)
                ? _settings.Settings.NotesStoragePath
                : notesSettings.StoragePath;

            var resolved = string.IsNullOrWhiteSpace(configuredPath)
                ? NotesStorageDefaults.GetDefaultPath()
                : configuredPath;

            var fullPath = Path.GetFullPath(resolved);
            Directory.CreateDirectory(fullPath);
            return fullPath;
        }

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
            var resolvedPath = EnsureNotesDefaultsPersisted();
            UpdateStoragePathHint(resolvedPath);
            CheckGoogleDriveStatus();
            await LoadList();
        }

        private string EnsureNotesDefaultsPersisted()
        {
            var notesSettings = GetNotesSettings();
            var resolvedPath = ResolveNotesStoragePath(notesSettings);
            var normalizedFormat = NormalizeExtension(notesSettings.DefaultFormat);

            var changed = false;
            if (!string.Equals(notesSettings.StoragePath, resolvedPath, StringComparison.OrdinalIgnoreCase))
            {
                notesSettings.StoragePath = resolvedPath;
                changed = true;
            }

            if (!string.Equals(notesSettings.DefaultFormat, normalizedFormat, StringComparison.OrdinalIgnoreCase))
            {
                notesSettings.DefaultFormat = normalizedFormat;
                changed = true;
            }

            if (changed)
            {
                _config.SaveSection("Notes", notesSettings);
            }

            if (!string.Equals(_settings.Settings.NotesStoragePath, resolvedPath, StringComparison.OrdinalIgnoreCase))
            {
                _settings.Settings.NotesStoragePath = resolvedPath;
                _settings.Save();
            }

            return resolvedPath;
        }

        private void UpdateStoragePathHint(string fullPath)
        {
            if (NotesStoragePathHint == null)
                return;

            NotesStoragePathHint.Text = $"Salvando em: {fullPath}";
            NotesStoragePathHint.ToolTip = fullPath;
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
                string storagePath = ResolveNotesStoragePath(notesSettings);

                // Buscar todas as notas
                var request = new NotesRequest(
                    Action: NotesAction.ListItems, 
                    NotesRootPath: storagePath
                );

                var listResult = await _engine.ExecuteAsync(request);
                if (!listResult.IsSuccess || listResult.Value?.ListResult == null)
                {
                    UiMessageService.ShowError("Nao foi possivel carregar a lista de notas para sincronizacao.", "Erro");
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
                
                UiMessageService.ShowInfo($"Sincronizacao concluida. Sucesso: {synced} | Falhas: {failed}", "Google Drive");
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
            string storagePath = ResolveNotesStoragePath(notesSettings);

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
            SetFormatFromSettingsDefault();
            NoteFormatCombo.IsEnabled = true;
            ClearEditValidation();
            ShowEditMode(true);
            UpdateDeleteButtonsState();
            FocusContentAtTop();
        }

        private async void NotesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (NotesList.SelectedItem is NoteListItem item)
            {
                _currentNoteKey = item.FileName; 
                
                var notesSettings = GetNotesSettings();
                string storagePath = ResolveNotesStoragePath(notesSettings);

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
                    SetFormatFromFileName(item.FileName);
                    NoteFormatCombo.IsEnabled = false;
                    ClearEditValidation();
                    ShowEditMode(true);
                    UpdateDeleteButtonsState();
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ClearEditValidation();
            ShowEditMode(false);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NoteTitle.Text))
            {
                SetEditValidation("O titulo da nota e obrigatorio.");
                return;
            }

            if (string.IsNullOrWhiteSpace(NotesContent.Text))
            {
                SetEditValidation("O conteudo da nota e obrigatorio.");
                return;
            }

            ClearEditValidation();
            var notesSettings = GetNotesSettings();
            string storagePath = ResolveNotesStoragePath(notesSettings);
            bool useMarkdown = string.Equals(GetSelectedFormatExtension(), ".md", StringComparison.OrdinalIgnoreCase);

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

                // Backup automatico na nuvem se habilitado
                if (notesSettings.AutoCloudSync)
                {
                    _ = UploadCurrentNoteAsync(showSuccessMessage: false);
                }

                ShowEditMode(false);
                await LoadList();
                UpdateDeleteButtonsState();
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
                string storagePath = ResolveNotesStoragePath(notesSettings);

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
                string storagePath = ResolveNotesStoragePath(notesSettings);

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
            if (!edit)
            {
                ClearEditValidation();
            }

            ListGrid.Visibility = edit ? Visibility.Collapsed : Visibility.Visible;
            EditGrid.Visibility = edit ? Visibility.Visible : Visibility.Collapsed;
            UpdateDeleteButtonsState();
        }

        private void SetEditValidation(string message)
        {
            if (EditValidationText == null)
                return;

            EditValidationText.Text = message;
            EditValidationText.Visibility = Visibility.Visible;
        }

        private void ClearEditValidation()
        {
            if (EditValidationText == null)
                return;

            EditValidationText.Text = string.Empty;
            EditValidationText.Visibility = Visibility.Collapsed;
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

        private void SetFormatFromSettingsDefault()
        {
            var notesSettings = GetNotesSettings();
            var ext = NormalizeExtension(notesSettings.DefaultFormat);
            NoteFormatCombo.SelectedIndex = string.Equals(ext, ".md", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }

        private void SetFormatFromFileName(string? fileName)
        {
            var ext = NormalizeExtension(Path.GetExtension(fileName));
            NoteFormatCombo.SelectedIndex = string.Equals(ext, ".md", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }

        private string GetSelectedFormatExtension()
        {
            if (NoteFormatCombo.SelectedItem is ComboBoxItem item && item.Content is string content)
            {
                return NormalizeExtension(content);
            }

            return ".txt";
        }

        private void FocusContentAtTop()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                NotesContent.Focus();
                NotesContent.CaretIndex = 0;
                NotesContent.ScrollToHome();
                NotesContent.ScrollToLine(0);
            }), DispatcherPriority.Input);
        }

        private void UpdateDeleteButtonsState()
        {
            if (DeleteEditButton != null)
            {
                DeleteEditButton.Visibility = !string.IsNullOrWhiteSpace(_currentNoteKey) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void DeleteListButton_Click(object sender, RoutedEventArgs e)
        {
            if (NotesList.SelectedItem is not NoteListItem selected)
            {
                UiMessageService.ShowWarning("Selecione uma nota para excluir.", "Notas");
                return;
            }

            await DeleteNoteByKeyAsync(selected.FileName, selected.Title);
        }

        private async void DeleteEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentNoteKey))
                return;

            await DeleteNoteByKeyAsync(_currentNoteKey, NoteTitle.Text);
        }

        private async Task DeleteNoteByKeyAsync(string noteKey, string? title)
        {
            var noteTitle = string.IsNullOrWhiteSpace(title) ? noteKey : title;
            if (!UiMessageService.Confirm($"Deseja excluir a nota '{noteTitle}'?", "Excluir nota"))
                return;

            try
            {
                var notesSettings = GetNotesSettings();
                string storagePath = ResolveNotesStoragePath(notesSettings);
                string root = NotesPaths.ResolveRoot(storagePath);
                string itemsRoot = NotesPaths.ItemsDir(root);

                string normalized = noteKey
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .TrimStart(Path.DirectorySeparatorChar);

                string fullPath = Path.GetFullPath(Path.Combine(itemsRoot, normalized));
                string rootWithSeparator = itemsRoot.EndsWith(Path.DirectorySeparatorChar)
                    ? itemsRoot
                    : itemsRoot + Path.DirectorySeparatorChar;

                if (fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                var indexStore = new NotesIndexStore();
                var load = await indexStore.LoadAsync(root);
                if (load.IsSuccess && load.Value != null)
                {
                    load.Value.Items.RemoveAll(x =>
                        string.Equals(x.FileName, noteKey, StringComparison.OrdinalIgnoreCase));
                    await indexStore.SaveAsync(root, load.Value);
                }

                _currentNoteKey = null;
                ShowEditMode(false);
                await LoadList();
                UiMessageService.ShowInfo("Nota excluida com sucesso.", "Notas");
            }
            catch (Exception ex)
            {
                UiMessageService.ShowError($"Erro ao excluir nota: {ex.Message}", "Erro ao excluir");
            }
        }
    }
}
