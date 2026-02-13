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
using DevTools.Presentation.Wpf.Services;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Views
{
    public partial class NotesWindow : Window
    {
        private readonly SettingsService _settings;
        private readonly NotesEngine _engine;
        private string? _currentNoteKey; 

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
                Action: NotesAction.ListItems, 
                NotesRootPath: _settings.Settings.NotesStoragePath
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
                
                var request = new NotesRequest(
                    Action: NotesAction.LoadNote, 
                    NotesRootPath: _settings.Settings.NotesStoragePath,
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
                MessageBox.Show("O título é obrigatório.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var action = _currentNoteKey == null ? NotesAction.CreateItem : NotesAction.SaveNote; 
            
            var request = new NotesRequest(
                Action: action,
                NotesRootPath: _settings.Settings.NotesStoragePath,
                Title: NoteTitle.Text,
                Content: NotesContent.Text,
                NoteKey: _currentNoteKey 
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
                    Action: NotesAction.ExportZip, 
                    NotesRootPath: _settings.Settings.NotesStoragePath,
                    OutputPath: dialog.FileName 
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
                    Action: NotesAction.ImportZip, 
                    NotesRootPath: _settings.Settings.NotesStoragePath,
                    ZipPath: dialog.FileName 
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
    }
}