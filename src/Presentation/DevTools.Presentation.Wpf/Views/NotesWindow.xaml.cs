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
using DevTools.Notes.Cloud;
using DevTools.Presentation.Wpf.Services;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Views
{
    public partial class NotesWindow : Window
    {
        private readonly SettingsService _settings;
        private readonly NotesEngine _engine;
        private string? _currentNoteKey; // Changed from FileName to NoteKey to match API

        public NotesWindow(SettingsService settings)
        {
            InitializeComponent();
            _settings = settings;
            _engine = new NotesEngine();
            
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await LoadList();

            // Auto-connect if provider was selected
            if (!string.IsNullOrEmpty(_settings.Settings.LastCloudProvider) &&
                Enum.TryParse<CloudProviderType>(_settings.Settings.LastCloudProvider, out var provider))
            {
                var action = provider == CloudProviderType.GoogleDrive ? NotesAction.ConnectGoogle : NotesAction.ConnectOneDrive;
                await RunCloudAction(action, provider, silent: true);
            }
            else
            {
                UpdateCloudStatusUI(false, null);
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private async Task LoadList()
        {
            var request = new NotesRequest(
                Action: NotesAction.ListItems, // Fixed Enum
                NotesRootPath: _settings.Settings.NotesStoragePath
            );

            var result = await _engine.ExecuteAsync(request);
            if (result.IsSuccess && result.Value?.ListResult != null)
            {
                NotesList.ItemsSource = result.Value.ListResult.Items;
            }
            else
            {
                // Handle empty or error
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
                _currentNoteKey = item.FileName; // NoteKey is typically the FileName/Path
                
                var request = new NotesRequest(
                    Action: NotesAction.LoadNote, // Fixed Enum
                    NotesRootPath: _settings.Settings.NotesStoragePath,
                    NoteKey: item.FileName // Fixed Param
                );

                var result = await _engine.ExecuteAsync(request);
                if (result.IsSuccess && result.Value?.ReadResult != null) // Fixed Property
                {
                    // ReadResult doesn't return Title, so we use the one from the list item
                    // or we could parse it from content if we wanted to be fancy.
                    // For now, relying on the list item's title is safer/simpler.
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
                MessageBox.Show("O título é obrigatório.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var action = _currentNoteKey == null ? NotesAction.CreateItem : NotesAction.SaveNote; // Fixed Enums
            
            var request = new NotesRequest(
                Action: action,
                NotesRootPath: _settings.Settings.NotesStoragePath,
                Title: NoteTitle.Text,
                Content: NotesContent.Text,
                NoteKey: _currentNoteKey // Fixed Param
            );

            var result = await _engine.ExecuteAsync(request);
            if (result.IsSuccess)
            {
                ShowEditMode(false);
                await LoadList();
            }
            else
            {
                MessageBox.Show($"Erro ao salvar: {result.Errors.FirstOrDefault()?.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "ZIP Files (*.zip)|*.zip",
                FileName = $"Notes_Backup_{DateTime.Now:yyyyMMdd}.zip"
            };

            if (dialog.ShowDialog() == true)
            {
                var request = new NotesRequest(
                    Action: NotesAction.ExportZip, // Fixed Enum
                    NotesRootPath: _settings.Settings.NotesStoragePath,
                    OutputPath: dialog.FileName // Fixed Param (Export uses OutputPath)
                );

                var result = await _engine.ExecuteAsync(request);
                if (result.IsSuccess)
                {
                    MessageBox.Show("Backup exportado com sucesso!", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Erro ao exportar: {result.Errors.FirstOrDefault()?.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "ZIP Files (*.zip)|*.zip"
            };

            if (dialog.ShowDialog() == true)
            {
                var request = new NotesRequest(
                    Action: NotesAction.ImportZip, // Fixed Enum
                    NotesRootPath: _settings.Settings.NotesStoragePath,
                    ZipPath: dialog.FileName // Fixed Param (Import uses ZipPath)
                );

                var result = await _engine.ExecuteAsync(request);
                if (result.IsSuccess)
                {
                    MessageBox.Show("Backup importado com sucesso!", "Importar", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadList();
                }
                else
                {
                    MessageBox.Show($"Erro ao importar: {result.Errors.FirstOrDefault()?.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // Cloud Handlers

        private void CloudButton_Click(object sender, RoutedEventArgs e)
        {
            if (CloudButton.ContextMenu != null)
                CloudButton.ContextMenu.IsOpen = true;
        }

        private async void SyncCloud_Click(object sender, RoutedEventArgs e)
        {
            await RunCloudAction(NotesAction.SyncCloud, CloudProviderType.None);
        }

        private async void ConnectGoogle_Click(object sender, RoutedEventArgs e)
        {
            await RunCloudAction(NotesAction.ConnectGoogle, CloudProviderType.GoogleDrive);
        }

        private async void ConnectOneDrive_Click(object sender, RoutedEventArgs e)
        {
            await RunCloudAction(NotesAction.ConnectOneDrive, CloudProviderType.OneDrive);
        }

        private async void DisconnectCloud_Click(object sender, RoutedEventArgs e)
        {
            await RunCloudAction(NotesAction.DisconnectCloud, CloudProviderType.None);
            _settings.Settings.LastCloudProvider = null;
            _settings.Save();
        }

        private async void StatusCloud_Click(object sender, RoutedEventArgs e)
        {
            await RunCloudAction(NotesAction.GetCloudStatus, CloudProviderType.None);
        }

        private async Task<bool> RunCloudAction(NotesAction action, CloudProviderType provider, bool silent = false)
        {
            var request = new NotesRequest(
                Action: action,
                NotesRootPath: _settings.Settings.NotesStoragePath,
                CloudProvider: provider,
                CloudConfig: GetCloudConfig());

            var result = await _engine.ExecuteAsync(request);
            
            if (result.IsSuccess)
            {
                if (action == NotesAction.ConnectGoogle || action == NotesAction.ConnectOneDrive)
                {
                    if (result.Value?.IsConnected ?? false)
                    {
                        _settings.Settings.LastCloudProvider = provider.ToString();
                        _settings.Save();
                        UpdateCloudStatusUI(true, provider.ToString());
                        if (!silent)
                            MessageBox.Show($"Conectado com sucesso ao {provider}!", "Cloud", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else if (action == NotesAction.SyncCloud && result.Value?.SyncResult != null)
                {
                    _settings.Settings.LastSyncTime = DateTime.Now;
                    _settings.Save();
                    
                    var sync = result.Value.SyncResult;
                    var msg = $"Sincronização concluída!\nEnviados: {sync.Uploaded}\nBaixados: {sync.Downloaded}\nConflitos: {sync.Conflicts}\nErros: {sync.Errors}";
                    
                    UpdateCloudStatusUI(true, _settings.Settings.LastCloudProvider);

                    if (sync.Messages.Count > 0)
                    {
                        msg += "\n\nDetalhes:\n" + string.Join("\n", sync.Messages.Take(5));
                        if (sync.Messages.Count > 5) msg += "\n...";
                    }
                    if (!silent)
                        MessageBox.Show(msg, "Sync", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (action == NotesAction.GetCloudStatus)
                {
                    var status = result.Value?.IsConnected ?? false;
                    var user = result.Value?.CloudUser ?? "Desconhecido";
                    var lastSync = _settings.Settings.LastSyncTime?.ToString("g") ?? "Nunca";
                    MessageBox.Show($"Status: {(status ? "Conectado" : "Desconectado")}\nUsuário: {user}\nÚltima Sync: {lastSync}", "Cloud Status", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (action == NotesAction.DisconnectCloud)
                {
                     UpdateCloudStatusUI(false, null);
                     if (!silent)
                        MessageBox.Show("Desconectado.", "Cloud", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                return true;
            }
            else
            {
                if (!silent)
                    MessageBox.Show($"Erro na operação Cloud: {result.Errors.FirstOrDefault()?.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void UpdateCloudStatusUI(bool connected, string? provider)
        {
            if (connected)
            {
                var lastSync = _settings.Settings.LastSyncTime?.ToString("HH:mm") ?? "N/A";
                CloudButton.ToolTip = $"Conectado ({provider})\nÚltima Sync: {lastSync}";
                // Optional: Change icon color or opacity to indicate connection
                CloudButton.Opacity = 1.0;
            }
            else
            {
                CloudButton.ToolTip = "Nuvem / Sync (Desconectado)";
                CloudButton.Opacity = 0.7;
            }
        }

        private CloudConfiguration GetCloudConfig()
        {
            return new CloudConfiguration
            {
                GoogleClientId = _settings.Settings.GoogleClientId,
                GoogleClientSecret = _settings.Settings.GoogleClientSecret,
                OneDriveClientId = _settings.Settings.OneDriveClientId
            };
        }
    }
}