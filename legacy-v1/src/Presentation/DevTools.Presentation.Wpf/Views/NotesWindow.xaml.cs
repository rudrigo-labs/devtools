using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DevTools.Notes.Engine;
using DevTools.Notes.Models;
using DevTools.Presentation.Wpf.Models;
using DevTools.Presentation.Wpf.Services;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Views
{
    public partial class NotesWindow : Window
    {
        private const int MinVisibleNotes = 8;
        private readonly SettingsService _settings;
        private readonly ConfigService _config;
        private readonly GoogleDriveService _googleDriveService;
        private readonly NotesEngine _engine;
        private string? _currentNoteKey; 
        private List<NoteListItem> _allNotes = new();
        private int _currentVisibleNotes = MinVisibleNotes;

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
            var normalizedDisplay = NormalizeInitialListDisplay(notesSettings.InitialListDisplay);

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

            if (!string.Equals(notesSettings.InitialListDisplay, normalizedDisplay, StringComparison.OrdinalIgnoreCase))
            {
                notesSettings.InitialListDisplay = normalizedDisplay;
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
                _allNotes = result.Value.ListResult.Items
                    .OrderByDescending(x => x.UpdatedUtc)
                    .ToList();
                ApplyVisibleNotes(resetToBase: true);
            }
            else
            {
                _allNotes = new List<NoteListItem>();
                NotesList.ItemsSource = new List<NoteListItem>();
                if (LoadMoreNotesButton != null)
                {
                    LoadMoreNotesButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ApplyVisibleNotes(bool resetToBase)
        {
            if (_allNotes.Count == 0)
            {
                NotesList.ItemsSource = new List<NoteListItem>();
                if (LoadMoreNotesButton != null)
                    LoadMoreNotesButton.Visibility = Visibility.Collapsed;
                return;
            }

            int baseCount = CalculateBaseVisibleNotes();
            if (resetToBase)
            {
                _currentVisibleNotes = baseCount;
            }
            else if (_currentVisibleNotes < baseCount)
            {
                _currentVisibleNotes = baseCount;
            }

            NotesList.ItemsSource = _allNotes.Take(_currentVisibleNotes).ToList();
            UpdateLoadMoreState(baseCount);
        }

        private int CalculateBaseVisibleNotes()
        {
            var configuredFixedCount = GetConfiguredFixedVisibleCount(GetNotesSettings().InitialListDisplay);
            if (configuredFixedCount.HasValue)
            {
                return Math.Max(MinVisibleNotes, configuredFixedCount.Value);
            }

            if (NotesList == null || NotesList.ActualHeight <= 0)
                return MinVisibleNotes;

            const double estimatedItemHeight = 72d;
            int fit = (int)Math.Floor(NotesList.ActualHeight / estimatedItemHeight);
            return Math.Max(MinVisibleNotes, fit);
        }

        private void UpdateLoadMoreState(int baseCount)
        {
            if (LoadMoreNotesButton == null)
                return;

            bool hasMore = _allNotes.Count > _currentVisibleNotes;
            LoadMoreNotesButton.Visibility = hasMore ? Visibility.Visible : Visibility.Collapsed;

            if (!hasMore)
            {
                LoadMoreNotesButton.ToolTip = "Todas as notas já estão visíveis";
                return;
            }

            int nextCount = GetNextVisibleStep(_currentVisibleNotes);
            int delta = Math.Max(1, nextCount - _currentVisibleNotes);
            LoadMoreNotesButton.ToolTip = $"Mostrar mais notas (+{delta})";
        }

        private static int GetNextVisibleStep(int current)
        {
            if (current < 15)
                return 15;

            return current + 5;
        }

        private static int? GetConfiguredFixedVisibleCount(string? displayMode)
        {
            if (int.TryParse(displayMode, out var count) && count > 0)
            {
                return count;
            }

            return null;
        }

        private static string NormalizeInitialListDisplay(string? value)
        {
            return value switch
            {
                "8" => "8",
                "15" => "15",
                "20" => "20",
                _ => "Auto"
            };
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

        private void LoadMoreNotesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_allNotes.Count == 0)
                return;

            int nextCount = GetNextVisibleStep(_currentVisibleNotes);
            _currentVisibleNotes = Math.Min(nextCount, _allNotes.Count);
            ApplyVisibleNotes(resetToBase: false);
        }

        private async void DeleteItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button)
                return;

            if (button.Tag is not string noteKey || string.IsNullOrWhiteSpace(noteKey))
                return;

            var title = button.CommandParameter as string;
            await DeleteNoteByKeyAsync(noteKey, title);
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string storagePath = ResolveNotesStoragePath(GetNotesSettings());
                Process.Start(new ProcessStartInfo
                {
                    FileName = storagePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                UiMessageService.ShowError($"Erro ao abrir pasta de notas: {ex.Message}", "Notas");
            }
        }

        private void NotesList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_allNotes.Count == 0)
                return;

            ApplyVisibleNotes(resetToBase: false);
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
                var request = new NotesRequest(
                    Action: NotesAction.DeleteItem,
                    NotesRootPath: storagePath,
                    NoteKey: noteKey);

                var result = await _engine.ExecuteAsync(request);
                if (!result.IsSuccess)
                {
                    var error = result.Errors.FirstOrDefault()?.Message ?? "Erro desconhecido ao excluir nota.";
                    UiMessageService.ShowError(error, "Erro ao excluir");
                    return;
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
