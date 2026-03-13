using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using DevTools.Host.Wpf.Facades;
using DevTools.Host.Wpf.Services;
using DevTools.Notes.Models;
using DevTools.Notes.Providers;
using Brush      = System.Windows.Media.Brush;
using Brushes    = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using KeyEventArgs      = System.Windows.Input.KeyEventArgs;
using ModifierKeys      = System.Windows.Input.ModifierKeys;
using Keyboard          = System.Windows.Input.Keyboard;
using Key               = System.Windows.Input.Key;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using OpenFileDialog    = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog    = Microsoft.Win32.SaveFileDialog;

namespace DevTools.Host.Wpf.Views;

public partial class NotesWorkspaceView : System.Windows.Controls.UserControl
{
    private const string NoConfigurationLabel = "Configurar manualmente";

    private enum NotesWorkspaceMode { Execution, Configuration }

    private readonly ObservableCollection<NotesEntity>          _entities             = new();
    private readonly ObservableCollection<NotesSelectionOption> _configurationOptions = new();
    private readonly INotesFacade _facade;

    private NotesEntity?       _currentEntity;
    private NoteListItem?      _currentNote;
    private NotesWorkspaceMode _currentMode = NotesWorkspaceMode.Execution;
    private bool _initialized;
    private bool _suppressSelectionChanged;
    private bool _isBusy;

    private static readonly ObservableCollection<NotesSelectionOption> ExtensionOptions = new()
    {
        new NotesSelectionOption("Markdown (.md)", ".md"),
        new NotesSelectionOption("Texto (.txt)",   ".txt")
    };

    public NotesWorkspaceView(INotesFacade facade)
    {
        _facade = facade;
        InitializeComponent();

        ConfigurationsCombo.ItemsSource  = _configurationOptions;
        ExtensionCombo.ItemsSource       = ExtensionOptions;
        ExtensionCombo.DisplayMemberPath = "Label";
        ExtensionCombo.SelectedIndex     = 0;

        Loaded += View_Loaded;
        ApplyModeState();
    }

    private async void View_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;
        await ReloadEntitiesAsync().ConfigureAwait(true);
    }

    // ── Atalho Ctrl+S ────────────────────────────────────────────────────────

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control
            && EditGrid.Visibility == Visibility.Visible)
        {
            e.Handled = true;
            _ = SaveNoteAsync();
        }
    }

    // ── Navegação de modo ─────────────────────────────────────────────────────

    private void SwitchToExecution_Click(object sender, RoutedEventArgs e) =>
        SetMode(NotesWorkspaceMode.Execution);

    private void SwitchToConfiguration_Click(object sender, RoutedEventArgs e) =>
        SetMode(NotesWorkspaceMode.Configuration);

    private void SetMode(NotesWorkspaceMode mode)
    {
        if (_isBusy) return;
        _currentMode = mode;
        ExecutionPanel.Visibility     = mode == NotesWorkspaceMode.Execution     ? Visibility.Visible : Visibility.Collapsed;
        ConfigurationPanel.Visibility = mode == NotesWorkspaceMode.Configuration ? Visibility.Visible : Visibility.Collapsed;
        ApplyModeState();
    }

    // ── Configurações ─────────────────────────────────────────────────────────

    private async Task ReloadEntitiesAsync()
    {
        var selectedId = _currentEntity?.Id;
        var list = await _facade.LoadConfigurationsAsync();

        _suppressSelectionChanged = true;
        _entities.Clear();
        _configurationOptions.Clear();
        _configurationOptions.Add(new NotesSelectionOption(NoConfigurationLabel, string.Empty));

        foreach (var item in list)
        {
            _entities.Add(item);
            _configurationOptions.Add(new NotesSelectionOption(item.Name, item.Id));
        }
        _suppressSelectionChanged = false;

        if (_entities.Count == 0)
        {
            SetSelectedConfiguration(null);
            CreateNewEntity();
            await ReloadNotesListAsync().ConfigureAwait(true);
            return;
        }

        var toSelect = _entities.FirstOrDefault(x => x.Id == selectedId)
            ?? _entities.FirstOrDefault(x => x.IsDefault)
            ?? _entities.First();

        SetSelectedConfiguration(toSelect);
        BindEntityToForm(toSelect);
        await ReloadNotesListAsync().ConfigureAwait(true);
    }

    private void SetSelectedConfiguration(NotesEntity? entity)
    {
        _suppressSelectionChanged = true;
        ConfigurationsCombo.SelectedItem = entity is null
            ? _configurationOptions.FirstOrDefault()
            : _configurationOptions.FirstOrDefault(o => o.Value == entity.Id);
        _suppressSelectionChanged = false;
    }

    private async void ConfigurationsCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged || _isBusy) return;
        if (ConfigurationsCombo.SelectedItem is not NotesSelectionOption opt) return;

        if (string.IsNullOrWhiteSpace(opt.Value))
        {
            CreateNewEntity();
            await ReloadNotesListAsync().ConfigureAwait(true);
            return;
        }

        _currentEntity = _entities.FirstOrDefault(x => x.Id == opt.Value);
        if (_currentEntity is not null)
        {
            BindEntityToForm(_currentEntity);
            NotesStoragePathHint.Text = _currentEntity.LocalRootPath;
            NotesStoragePathHint.ToolTip = _currentEntity.LocalRootPath;
            await ReloadNotesListAsync().ConfigureAwait(true);
        }
    }

    // ── Lista de notas ────────────────────────────────────────────────────────

    private async Task ReloadNotesListAsync()
    {
        var request = new NotesRequest { Action = NotesAction.ListItems };
        var result  = await _facade.ExecuteAsync(request, _currentEntity).ConfigureAwait(true);

        NotesList.ItemsSource = null;
        if (result.IsSuccess && result.Value?.ListResult is not null)
            NotesList.ItemsSource = result.Value.ListResult.Items;

        ShowListView();
    }

    private void NotesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (NotesList.SelectedItem is NoteListItem item)
            _ = OpenNoteAsync(item);
    }

    private async Task OpenNoteAsync(NoteListItem item)
    {
        var request = new NotesRequest { Action = NotesAction.LoadNote, NoteKey = item.FileName };
        var result  = await _facade.ExecuteAsync(request, _currentEntity).ConfigureAwait(true);

        if (!result.IsSuccess || result.Value?.ReadResult is null)
        {
            ExecutionStatusText.Text = string.Join(" | ", result.Errors.Select(x => x.Message));
            return;
        }

        _currentNote          = item;
        NoteTitleInput.Text   = item.Title;
        NoteContentInput.Text = result.Value.ReadResult.Content ?? string.Empty;

        var ext = Path.GetExtension(item.FileName)?.ToLowerInvariant();
        ExtensionCombo.SelectedItem = ExtensionOptions.FirstOrDefault(o => o.Value == ext)
            ?? ExtensionOptions[0];

        DriveStatusBanner.Visibility = Visibility.Collapsed;
        ExecutionStatusText.Text     = string.Empty;
        ShowEditView();
    }

    // ── AppBar lista ──────────────────────────────────────────────────────────

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        _currentNote          = null;
        NoteTitleInput.Text   = string.Empty;
        NoteContentInput.Text = string.Empty;
        ExtensionCombo.SelectedIndex  = 0;
        DriveStatusBanner.Visibility  = Visibility.Collapsed;
        ExecutionStatusText.Text      = string.Empty;
        ShowEditView();
        NoteTitleInput.Focus();
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title    = "Exportar notas como ZIP",
            Filter   = "ZIP|*.zip",
            FileName = $"notes-export-{DateTime.Now:yyyyMMdd}.zip"
        };
        if (dlg.ShowDialog() != true) return;

        _isBusy = true;
        ApplyModeState();
        ExecutionStatusText.Text = "Exportando...";

        try
        {
            var request = new NotesRequest
            {
                Action     = NotesAction.ExportZip,
                OutputPath = Path.GetDirectoryName(dlg.FileName)
            };
            var result = await _facade.ExecuteAsync(request, _currentEntity).ConfigureAwait(true);
            ExecutionStatusText.Text = result.IsSuccess
                ? $"Exportado: {result.Value?.ExportedZipPath}"
                : string.Join(" | ", result.Errors.Select(x => x.Message));
        }
        finally { _isBusy = false; ApplyModeState(); }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Title = "Importar ZIP de notas", Filter = "ZIP|*.zip" };
        if (dlg.ShowDialog() != true) return;

        _isBusy = true;
        ApplyModeState();
        ExecutionStatusText.Text = "Importando...";

        try
        {
            var request = new NotesRequest { Action = NotesAction.ImportZip, ZipPath = dlg.FileName };
            var result  = await _facade.ExecuteAsync(request, _currentEntity).ConfigureAwait(true);

            if (!result.IsSuccess)
            {
                ExecutionStatusText.Text = string.Join(" | ", result.Errors.Select(x => x.Message));
                return;
            }

            var r = result.Value?.BackupReport;
            ExecutionStatusText.Text = r is null ? "Importado." :
                $"Importado: {r.ImportedCount} novo(s), {r.SkippedCount} igual(is), {r.ConflictCount} conflito(s).";
            await ReloadNotesListAsync().ConfigureAwait(true);
        }
        finally { _isBusy = false; ApplyModeState(); }
    }

    // ── Deletar nota da lista ─────────────────────────────────────────────────

    private async void DeleteItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (e.Source is not System.Windows.Controls.Button btn) return;
        var fileName = btn.Tag as string;
        var title    = btn.CommandParameter as string ?? fileName ?? "?";
        if (string.IsNullOrWhiteSpace(fileName)) return;

        var confirm = Components.DevToolsMessageBox.Confirm(
            Window.GetWindow(this), $"Deletar a nota \"{title}\"?", "Deletar");
        if (confirm != Components.DevToolsMessageBoxResult.Yes) return;

        var request = new NotesRequest { Action = NotesAction.DeleteItem, NoteKey = fileName };
        var result  = await _facade.ExecuteAsync(request, _currentEntity).ConfigureAwait(true);

        ExecutionStatusText.Text = result.IsSuccess
            ? "Nota excluída."
            : string.Join(" | ", result.Errors.Select(x => x.Message));

        await ReloadNotesListAsync().ConfigureAwait(true);
    }

    // ── AppBar editor ─────────────────────────────────────────────────────────

    private void BackButton_Click(object sender, RoutedEventArgs e) =>
        _ = ReloadNotesListAsync();

    private void SaveNote_Click(object sender, RoutedEventArgs e) =>
        _ = SaveNoteAsync();

    private async void DeleteCurrentNote_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNote is null) return;

        var confirm = Components.DevToolsMessageBox.Confirm(
            Window.GetWindow(this), $"Deletar a nota \"{_currentNote.Title}\"?", "Deletar");
        if (confirm != Components.DevToolsMessageBoxResult.Yes) return;

        var request = new NotesRequest { Action = NotesAction.DeleteItem, NoteKey = _currentNote.FileName };
        var result  = await _facade.ExecuteAsync(request, _currentEntity).ConfigureAwait(true);

        ExecutionStatusText.Text = result.IsSuccess ? "Nota excluída." :
            string.Join(" | ", result.Errors.Select(x => x.Message));

        await ReloadNotesListAsync().ConfigureAwait(true);
    }

    private async Task SaveNoteAsync()
    {
        var title   = NoteTitleInput.Text.Trim();
        var content = NoteContentInput.Text;
        var ext     = (ExtensionCombo.SelectedItem as NotesSelectionOption)?.Value ?? ".md";

        if (string.IsNullOrWhiteSpace(title))
        {
            ExecutionStatusText.Text = "Título é obrigatório.";
            return;
        }

        _isBusy = true;
        ApplyModeState();
        ExecutionStatusText.Text = "Salvando...";

        try
        {
            NotesRequest request = _currentNote is not null
                ? new NotesRequest
                  {
                      Action    = NotesAction.SaveNote,
                      NoteKey   = _currentNote.FileName,
                      Content   = content,
                      Overwrite = true
                  }
                : new NotesRequest
                  {
                      Action           = NotesAction.CreateItem,
                      Title            = title,
                      Content          = content,
                      Extension        = ext,
                      CreateDateFolder = true
                  };

            var result = await _facade.ExecuteAsync(request, _currentEntity).ConfigureAwait(true);

            if (!result.IsSuccess)
            {
                ExecutionStatusText.Text = string.Join(" | ", result.Errors.Select(x => x.Message));
                return;
            }

            // Atualizar _currentNote se era nova
            if (_currentNote is null && result.Value?.CreateResult is not null)
            {
                var cr = result.Value.CreateResult;
                _currentNote = new NoteListItem(cr.Id, cr.Title, cr.FileName, cr.CreatedUtc, cr.UpdatedUtc);
            }

            var driveOk = result.Value?.DriveSkipped == false;
            ExecutionStatusText.Text = driveOk ? "Salvo e sincronizado com o Drive." : "Salvo.";
            ShowDriveBanner(result.Value?.DriveSkipped, result.Value?.DriveSkipReason);
        }
        finally { _isBusy = false; ApplyModeState(); }
    }

    // ── Action Bar (configuração) ─────────────────────────────────────────────

    private void ActionNew_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;
        CreateNewEntity();
        SetMode(NotesWorkspaceMode.Configuration);
    }

    private async void ActionSave_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;

        if (_currentMode == NotesWorkspaceMode.Execution)
        {
            await SaveNoteAsync().ConfigureAwait(true);
            return;
        }

        ReadFormIntoEntity();

        if (!ValidationUiService.ValidateRequiredFields(out var err,
            ValidationUiService.RequiredControl("Nome", NameInput, NameInput.Text)))
        {
            ExecutionStatusText.Text = err;
            return;
        }

        var validation = await _facade.SaveConfigurationAsync(_currentEntity!).ConfigureAwait(true);
        if (!validation.IsValid)
        {
            ExecutionStatusText.Text = string.Join(" | ", validation.Errors.Select(x => x.Message));
            return;
        }

        ExecutionStatusText.Text = string.Empty;
        await ReloadEntitiesAsync().ConfigureAwait(true);
        ExecutionStatusText.Text = "Configuração salva.";
    }

    private async void ActionDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || _currentEntity is null || string.IsNullOrWhiteSpace(_currentEntity.Id)) return;

        var confirm = Components.DevToolsMessageBox.Confirm(
            Window.GetWindow(this), $"Excluir configuração \"{_currentEntity.Name}\"?", "Excluir");
        if (confirm != Components.DevToolsMessageBoxResult.Yes) return;

        await _facade.DeleteConfigurationAsync(_currentEntity.Id).ConfigureAwait(true);
        _currentEntity = null;
        await ReloadEntitiesAsync().ConfigureAwait(true);
        ExecutionStatusText.Text = "Configuração excluída.";
    }

    private void ActionCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_currentEntity is not null) BindEntityToForm(_currentEntity);
        ExecutionStatusText.Text = string.Empty;
    }

    // ── Google Drive ──────────────────────────────────────────────────────────

    private void GoogleDriveEnabledCheck_Changed(object sender, RoutedEventArgs e) =>
        GoogleDriveFieldsPanel.Visibility = GoogleDriveEnabledCheck.IsChecked == true
            ? Visibility.Visible : Visibility.Collapsed;

    private async void ConnectDrive_Click(object sender, RoutedEventArgs e)
    {
        ReadFormIntoEntity();
        if (string.IsNullOrWhiteSpace(_currentEntity?.GoogleDriveCredentialsPath)
            || string.IsNullOrWhiteSpace(_currentEntity?.OAuthTokenCachePath))
        {
            ExecutionStatusText.Text = "Preencha o credentials.json e a pasta do token antes de conectar.";
            return;
        }
        DriveConnectionStatusText.Text = "Abrindo browser...";
        _isBusy = true; ApplyModeState();
        try
        {
            var result = await _facade.ConnectGoogleDriveAsync(_currentEntity!).ConfigureAwait(true);
            DriveConnectionStatusText.Text = result.IsSuccess ? "✓ Conectado." :
                $"Erro: {result.Errors.FirstOrDefault()?.Message}";
        }
        finally { _isBusy = false; ApplyModeState(); }
    }

    private async void DisconnectDrive_Click(object sender, RoutedEventArgs e)
    {
        if (_currentEntity is null) return;
        _isBusy = true; ApplyModeState();
        DriveConnectionStatusText.Text = "Desconectando...";
        try
        {
            var result = await _facade.DisconnectGoogleDriveAsync(_currentEntity).ConfigureAwait(true);
            DriveConnectionStatusText.Text = result.IsSuccess ? "Desconectado." :
                $"Erro: {result.Errors.FirstOrDefault()?.Message}";
        }
        finally { _isBusy = false; ApplyModeState(); }
    }

    private void ShowSetupGuide_Click(object sender, RoutedEventArgs e)
    {
        var win = new Window
        {
            Title = "Guia de Configuração — Google Drive",
            Width = 680, Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            Background = (Brush)FindResource("DevToolsWindowBackground")
        };
        var scroll = new System.Windows.Controls.ScrollViewer { Margin = new Thickness(16) };
        var tb = new System.Windows.Controls.TextBox
        {
            Text            = NotesGoogleDriveSetupGuide.GetGuideText(),
            IsReadOnly      = true,
            FontFamily      = new FontFamily("Consolas"),
            FontSize        = 12,
            Background      = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            TextWrapping    = TextWrapping.Wrap,
            Foreground      = (Brush)FindResource("DevToolsTextPrimary")
        };
        scroll.Content = tb;
        win.Content    = scroll;
        win.ShowDialog();
    }

    // ── Alternância ListGrid / EditGrid ───────────────────────────────────────

    private void ShowListView()
    {
        ListGrid.Visibility = Visibility.Visible;
        EditGrid.Visibility = Visibility.Collapsed;
        _currentNote = null;
    }

    private void ShowEditView()
    {
        ListGrid.Visibility = Visibility.Collapsed;
        EditGrid.Visibility = Visibility.Visible;
    }

    // ── Drive banner ──────────────────────────────────────────────────────────

    private void ShowDriveBanner(bool? skipped, string? reason)
    {
        if (skipped is null) { DriveStatusBanner.Visibility = Visibility.Collapsed; return; }
        DriveStatusBanner.Visibility = Visibility.Visible;
        DriveStatusText.Text = skipped == false ? "✓ Nota sincronizada com o Google Drive." :
            reason ?? "Drive não sincronizado.";
    }

    // ── Binding de configuração ───────────────────────────────────────────────

    private void BindEntityToForm(NotesEntity entity)
    {
        _currentEntity = entity;
        NameInput.Text                           = entity.Name;
        DescriptionInput.Text                    = entity.Description ?? string.Empty;
        LocalRootPathSelector.SelectedPath       = entity.LocalRootPath;
        IsDefaultCheck.IsChecked                 = entity.IsDefault;
        GoogleDriveEnabledCheck.IsChecked        = entity.GoogleDriveEnabled;
        CredentialsPathSelector.SelectedPath     = entity.GoogleDriveCredentialsPath;
        FolderIdInput.Text                       = entity.GoogleDriveFolderId;
        OAuthTokenCachePathSelector.SelectedPath = entity.OAuthTokenCachePath;
        GoogleDriveFieldsPanel.Visibility        = entity.GoogleDriveEnabled
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ReadFormIntoEntity()
    {
        if (_currentEntity is null) return;
        _currentEntity.Name                       = NameInput.Text.Trim();
        _currentEntity.Description                = DescriptionInput.Text.Trim();
        _currentEntity.LocalRootPath              = LocalRootPathSelector.SelectedPath?.Trim() ?? string.Empty;
        _currentEntity.IsDefault                  = IsDefaultCheck.IsChecked ?? false;
        _currentEntity.GoogleDriveEnabled         = GoogleDriveEnabledCheck.IsChecked ?? false;
        _currentEntity.GoogleDriveCredentialsPath = CredentialsPathSelector.SelectedPath?.Trim() ?? string.Empty;
        _currentEntity.GoogleDriveFolderId        = FolderIdInput.Text.Trim();
        _currentEntity.OAuthTokenCachePath        = OAuthTokenCachePathSelector.SelectedPath?.Trim() ?? string.Empty;
    }

    private void CreateNewEntity()
    {
        _currentEntity = new NotesEntity { DefaultExtension = ".md" };
        BindEntityToForm(_currentEntity);
        SetSelectedConfiguration(null);
    }

    // ── Estado geral ──────────────────────────────────────────────────────────

    private void ApplyModeState()
    {
        Actions.CanSave    = !_isBusy;
        Actions.CanCancel  = _isBusy;
        Actions.ShowCancel = _isBusy;
        Actions.SaveText   = _currentMode == NotesWorkspaceMode.Execution ? "Salvar nota" : "Salvar";
    }
}
