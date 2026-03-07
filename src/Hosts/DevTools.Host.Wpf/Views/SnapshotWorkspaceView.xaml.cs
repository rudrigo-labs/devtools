using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DevTools.Host.Wpf.Services;
using DevTools.Snapshot.Engine;
using DevTools.Snapshot.Models;
using DevTools.Snapshot.Services;

namespace DevTools.Host.Wpf.Views;

public partial class SnapshotWorkspaceView : System.Windows.Controls.UserControl
{
    private readonly ObservableCollection<SnapshotEntity> _entities = new();
    private SnapshotEntityService? _entityService;
    private SnapshotEngine? _snapshotEngine;
    private SnapshotEntity? _currentEntity;

    public SnapshotWorkspaceView()
    {
        InitializeComponent();
        ConfigurationsCombo.ItemsSource = _entities;
        SetActionState();
    }

    public async void Initialize(SnapshotEntityService entityService, SnapshotEngine snapshotEngine)
    {
        _entityService = entityService;
        _snapshotEngine = snapshotEngine;
        await ReloadEntitiesAsync().ConfigureAwait(true);
    }

    private async Task ReloadEntitiesAsync()
    {
        if (_entityService is null)
            return;

        var selectedId = _currentEntity?.Id;
        var list = await _entityService.ListAsync();
        _entities.Clear();
        foreach (var item in list)
            _entities.Add(item);

        var selected = _entities.FirstOrDefault(x => x.Id == selectedId)
            ?? _entities.FirstOrDefault(x => x.IsDefault)
            ?? _entities.FirstOrDefault();

        ConfigurationsCombo.SelectedItem = selected;
        if (selected is null)
        {
            CreateNewEntity();
        }
    }

    private void ConfigurationsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ConfigurationsCombo.SelectedItem is SnapshotEntity entity)
        {
            _currentEntity = entity;
            BindEntityToForm(entity);
            ValidationUiService.ClearInline(MainFrame);
            SetActionState();
        }
    }

    private async void ActionSave_Click(object sender, RoutedEventArgs e)
    {
        if (_entityService is null)
            return;

        if (!TryBindFormToEntity(out var entity, out var validationError))
        {
            ValidationUiService.ShowInline(MainFrame, validationError);
            return;
        }

        var result = await _entityService.UpsertAsync(entity);
        if (!result.IsValid)
        {
            ValidationUiService.ShowInline(MainFrame, JoinValidationErrors(result));
            return;
        }

        ValidationUiService.ClearInline(MainFrame);
        _currentEntity = entity;
        await ReloadEntitiesAsync().ConfigureAwait(true);
        ExecutionStatusText.Text = $"Configuracao '{entity.Name}' salva.";
        SetActionState();
    }

    private async void ActionDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_entityService is null || _currentEntity is null)
            return;

        if (string.IsNullOrWhiteSpace(_currentEntity.Id))
        {
            CreateNewEntity();
            return;
        }

        await _entityService.DeleteAsync(_currentEntity.Id);
        ValidationUiService.ClearInline(MainFrame);
        await ReloadEntitiesAsync().ConfigureAwait(true);
        ExecutionStatusText.Text = "Configuracao removida.";
        SetActionState();
    }

    private void ActionNew_Click(object sender, RoutedEventArgs e)
    {
        CreateNewEntity();
        ValidationUiService.ClearInline(MainFrame);
        ExecutionStatusText.Text = "Nova configuracao criada (nao salva).";
        SetActionState();
    }

    private void ActionCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_currentEntity is not null)
        {
            BindEntityToForm(_currentEntity);
        }

        ValidationUiService.ClearInline(MainFrame);
        ExecutionStatusText.Text = "Edicao cancelada.";
        SetActionState();
    }

    private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_snapshotEngine is null)
            return;

        if (!TryBindFormToEntity(out var entity, out var validationError))
        {
            ValidationUiService.ShowInline(MainFrame, validationError);
            return;
        }

        var request = new SnapshotExecutionRequest
        {
            RootPath = entity.RootPath,
            OutputBasePath = entity.OutputBasePath,
            GenerateText = entity.GenerateText,
            GenerateHtmlPreview = entity.GenerateHtmlPreview,
            GenerateJsonNested = entity.GenerateJsonNested,
            GenerateJsonRecursive = entity.GenerateJsonRecursive,
            IgnoredDirectories = entity.IgnoredDirectories,
            MaxFileSizeKb = entity.MaxFileSizeKb
        };

        var result = await _snapshotEngine.ExecuteAsync(request);
        if (!result.IsSuccess)
        {
            ValidationUiService.ShowInline(MainFrame, string.Join(" | ", result.Errors.Select(x => x.Message)));
            ExecutionStatusText.Text = "Falha na execucao do snapshot.";
            return;
        }

        ValidationUiService.ClearInline(MainFrame);
        var data = result.Value!;
        ExecutionStatusText.Text = $"Snapshot executado. Arquivos analisados: {data.TotalFilesScanned}.";
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ActionCancel_Click(sender, e);
    }

    private void CreateNewEntity()
    {
        var nextIndex = _entities.Count + 1;
        _currentEntity = new SnapshotEntity
        {
            Name = $"Snapshot {nextIndex}",
            Description = "Nova configuracao",
            IsActive = true,
            GenerateText = true
        };

        ConfigurationsCombo.SelectedItem = null;
        BindEntityToForm(_currentEntity);
        SetActionState();
    }

    private void BindEntityToForm(SnapshotEntity entity)
    {
        NameInput.Text = entity.Name;
        DescriptionInput.Text = entity.Description;
        RootPathSelector.SelectedPath = entity.RootPath;
        OutputBasePathSelector.SelectedPath = entity.OutputBasePath;
        IgnoredDirectoriesInput.Text = string.Join(", ", entity.IgnoredDirectories);
        MaxFileSizeKbInput.Text = entity.MaxFileSizeKb?.ToString() ?? string.Empty;
        TextCheck.IsChecked = entity.GenerateText;
        HtmlCheck.IsChecked = entity.GenerateHtmlPreview;
        JsonNestedCheck.IsChecked = entity.GenerateJsonNested;
        JsonRecursiveCheck.IsChecked = entity.GenerateJsonRecursive;
    }

    private bool TryBindFormToEntity(out SnapshotEntity entity, out string errorMessage)
    {
        entity = _currentEntity is null ? new SnapshotEntity() : CloneEntity(_currentEntity);

        var maxFileSizeKb = ParseOptionalInt(MaxFileSizeKbInput.Text);
        var maxFileSizeInvalid = !string.IsNullOrWhiteSpace(MaxFileSizeKbInput.Text) && maxFileSizeKb is null;

        if (!ValidationUiService.ValidateRequiredFields(
                out errorMessage,
                ValidationUiService.RequiredControl("Nome", NameInput, NameInput.Text),
                ValidationUiService.RequiredPath("Pasta do projeto", RootPathSelector, RootPathSelector.SelectedPath)))
        {
            ValidationUiService.SetControlInvalid(MaxFileSizeKbInput, maxFileSizeInvalid);
            return false;
        }

        ValidationUiService.SetControlInvalid(MaxFileSizeKbInput, maxFileSizeInvalid);

        if (maxFileSizeInvalid)
        {
            errorMessage = "Tamanho maximo por arquivo (KB) deve ser um numero inteiro valido.";
            return false;
        }

        entity.Name = NameInput.Text.Trim();
        entity.Description = DescriptionInput.Text.Trim();
        entity.RootPath = RootPathSelector.SelectedPath?.Trim() ?? string.Empty;
        entity.OutputBasePath = OutputBasePathSelector.SelectedPath?.Trim() ?? string.Empty;
        entity.MaxFileSizeKb = maxFileSizeKb;
        entity.IgnoredDirectories = ParseIgnoredDirectories(IgnoredDirectoriesInput.Text);
        entity.GenerateText = TextCheck.IsChecked ?? false;
        entity.GenerateHtmlPreview = HtmlCheck.IsChecked ?? false;
        entity.GenerateJsonNested = JsonNestedCheck.IsChecked ?? false;
        entity.GenerateJsonRecursive = JsonRecursiveCheck.IsChecked ?? false;
        entity.IsActive = true;

        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            entity.Id = entity.Name;
        }

        _currentEntity = entity;
        errorMessage = string.Empty;
        return true;
    }

    private void SetActionState()
    {
        var hasSelected = _currentEntity is not null;
        var hasPersisted = hasSelected && !string.IsNullOrWhiteSpace(_currentEntity!.Id) && _entities.Any(x => x.Id == _currentEntity.Id);

        Actions.CanSave = hasSelected;
        Actions.CanDelete = hasPersisted;
        Actions.CanCancel = hasSelected;
    }

    private static SnapshotEntity CloneEntity(SnapshotEntity source)
    {
        return new SnapshotEntity
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            IsActive = source.IsActive,
            IsDefault = source.IsDefault,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,
            RootPath = source.RootPath,
            OutputBasePath = source.OutputBasePath,
            GenerateText = source.GenerateText,
            GenerateHtmlPreview = source.GenerateHtmlPreview,
            GenerateJsonNested = source.GenerateJsonNested,
            GenerateJsonRecursive = source.GenerateJsonRecursive,
            IgnoredDirectories = source.IgnoredDirectories.ToArray(),
            MaxFileSizeKb = source.MaxFileSizeKb
        };
    }

    private static IReadOnlyList<string> ParseIgnoredDirectories(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Array.Empty<string>();

        return input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static int? ParseOptionalInt(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        return int.TryParse(input, out var parsed) ? parsed : null;
    }

    private static string JoinValidationErrors(DevTools.Core.Validation.ValidationResult validationResult)
    {
        if (validationResult.IsValid)
            return string.Empty;

        return string.Join(" | ", validationResult.Errors.Select(x => x.Message));
    }
}
