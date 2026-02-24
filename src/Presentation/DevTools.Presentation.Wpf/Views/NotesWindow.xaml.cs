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
using DevTools.Presentation.Wpf.Utilities;
using Microsoft.Win32;

namespace DevTools.Presentation.Wpf.Views;

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
            NotesAction.ListItems,
            NotesRootPath: _settings.Settings.NotesStoragePath);

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
                NotesAction.LoadNote,
                NoteKey: item.FileName,
                NotesRootPath: _settings.Settings.NotesStoragePath);

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
            DevToolsMessage.Warning("O título é obrigatório.", "Erro");
            return;
        }

        var action = _currentNoteKey == null ? NotesAction.CreateItem : NotesAction.SaveNote;

        var request = new NotesRequest(
            action,
            NoteKey: _currentNoteKey,
            Content: NotesContent.Text,
            NotesRootPath: _settings.Settings.NotesStoragePath,
            Title: NoteTitle.Text);

        var result = await _engine.ExecuteAsync(request);
        if (result.IsSuccess)
        {
            ShowEditMode(false);
            await LoadList();
        }
        else
        {
            var message = result.Errors.FirstOrDefault()?.Message ?? "Erro desconhecido";
            DevToolsMessage.Error($"Erro ao salvar: {message}", "Erro");
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
            var request = new NotesRequest(
                NotesAction.ExportZip,
                NotesRootPath: _settings.Settings.NotesStoragePath,
                OutputPath: dialog.FileName);

            var result = await _engine.ExecuteAsync(request);
            if (result.IsSuccess)
            {
                DevToolsMessage.Info("Backup exportado com sucesso!", "Exportar");
            }
            else
            {
                var message = result.Errors.FirstOrDefault()?.Message ?? "Erro desconhecido";
                DevToolsMessage.Error($"Erro ao exportar: {message}", "Erro");
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
            var request = new NotesRequest(
                NotesAction.ImportZip,
                NotesRootPath: _settings.Settings.NotesStoragePath,
                ZipPath: dialog.FileName);

            var result = await _engine.ExecuteAsync(request);
            if (result.IsSuccess)
            {
                DevToolsMessage.Info("Backup importado com sucesso!", "Importar");
                await LoadList();
            }
            else
            {
                var message = result.Errors.FirstOrDefault()?.Message ?? "Erro desconhecido";
                DevToolsMessage.Error($"Erro ao importar: {message}", "Erro");
            }
        }
    }

    private void NotesContent_TextChanged(object sender, TextChangedEventArgs e)
    {
    }

    private void ShowEditMode(bool edit)
    {
        ListGrid.Visibility = edit ? Visibility.Collapsed : Visibility.Visible;
        EditGrid.Visibility = edit ? Visibility.Visible : Visibility.Collapsed;
    }
}
