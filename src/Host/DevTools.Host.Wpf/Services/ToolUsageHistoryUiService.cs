using System.Text.Json;
using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using System.Windows;
using System.Windows.Media;
using WpfControls = System.Windows.Controls;

namespace DevTools.Host.Wpf.Services;

public sealed class ToolUsageHistoryUiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IToolUsageHistoryRepository _repository;

    public ToolUsageHistoryUiService(IToolUsageHistoryRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ToolUsageHistoryEntry>> ListAsync(string toolSlug, CancellationToken ct = default)
        => _repository.ListAsync(toolSlug, ct);

    public async Task RecordAsync(
        string toolSlug,
        FrameworkElement root,
        string title,
        CancellationToken ct = default)
    {
        var snapshot = Capture(root);
        var payload = JsonSerializer.Serialize(snapshot, JsonOptions);

        var entry = new ToolUsageHistoryEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            UsedAtUtc = DateTime.UtcNow,
            Title = string.IsNullOrWhiteSpace(title) ? "Execução" : title.Trim(),
            PayloadJson = payload
        };

        await _repository.AddAsync(toolSlug, entry, maxItems: 10, ct).ConfigureAwait(true);
    }

    public bool TryApply(FrameworkElement root, ToolUsageHistoryEntry entry)
    {
        if (entry is null || string.IsNullOrWhiteSpace(entry.PayloadJson))
            return false;

        ToolFormSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<ToolFormSnapshot>(entry.PayloadJson, JsonOptions);
        }
        catch
        {
            return false;
        }

        if (snapshot is null || snapshot.Controls.Count == 0)
            return false;

        var appliedAny = false;

        foreach (var state in snapshot.Controls)
        {
            var element = root.FindName(state.Name) as FrameworkElement;
            if (element is null)
                continue;

            switch (state.Kind)
            {
                case "TextBox" when element is WpfControls.TextBox textBox:
                    textBox.Text = state.StringValue ?? string.Empty;
                    appliedAny = true;
                    break;
                case "CheckBox" when element is WpfControls.CheckBox checkBox:
                    checkBox.IsChecked = state.BoolValue;
                    appliedAny = true;
                    break;
                case "RadioButton" when element is WpfControls.RadioButton radioButton:
                    radioButton.IsChecked = state.BoolValue;
                    appliedAny = true;
                    break;
                case "ComboBox" when element is WpfControls.ComboBox comboBox:
                    if (state.IntValue.HasValue)
                    {
                        comboBox.SelectedIndex = state.IntValue.Value;
                        appliedAny = true;
                    }
                    break;
                case "PathSelector":
                {
                    var selectedPathProperty = element.GetType().GetProperty("SelectedPath");
                    if (selectedPathProperty?.CanWrite == true && selectedPathProperty.PropertyType == typeof(string))
                    {
                        selectedPathProperty.SetValue(element, state.StringValue ?? string.Empty);
                        appliedAny = true;
                    }

                    break;
                }
            }
        }

        return appliedAny;
    }

    private static ToolFormSnapshot Capture(FrameworkElement root)
    {
        var snapshot = new ToolFormSnapshot();

        foreach (var element in EnumerateVisualTree(root))
        {
            if (element is not FrameworkElement frameworkElement)
                continue;

            var name = frameworkElement.Name;
            if (!ShouldTrack(name))
                continue;

            switch (frameworkElement)
            {
                case WpfControls.TextBox textBox when !textBox.IsReadOnly:
                    snapshot.Controls.Add(new ToolFormState
                    {
                        Name = name,
                        Kind = "TextBox",
                        StringValue = textBox.Text
                    });
                    break;

                case WpfControls.CheckBox checkBox:
                    snapshot.Controls.Add(new ToolFormState
                    {
                        Name = name,
                        Kind = "CheckBox",
                        BoolValue = checkBox.IsChecked
                    });
                    break;

                case WpfControls.RadioButton radioButton:
                    snapshot.Controls.Add(new ToolFormState
                    {
                        Name = name,
                        Kind = "RadioButton",
                        BoolValue = radioButton.IsChecked
                    });
                    break;

                case WpfControls.ComboBox comboBox:
                    snapshot.Controls.Add(new ToolFormState
                    {
                        Name = name,
                        Kind = "ComboBox",
                        IntValue = comboBox.SelectedIndex
                    });
                    break;

                default:
                {
                    var selectedPathProperty = frameworkElement.GetType().GetProperty("SelectedPath");
                    if (selectedPathProperty?.CanRead == true && selectedPathProperty.PropertyType == typeof(string))
                    {
                        var value = selectedPathProperty.GetValue(frameworkElement) as string;
                        snapshot.Controls.Add(new ToolFormState
                        {
                            Name = name,
                            Kind = "PathSelector",
                            StringValue = value ?? string.Empty
                        });
                    }

                    break;
                }
            }
        }

        return snapshot;
    }

    private static bool ShouldTrack(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (name.Contains("Status", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Result", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Summary", StringComparison.OrdinalIgnoreCase))
            return false;

        if (name.EndsWith("Button", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static IEnumerable<DependencyObject> EnumerateVisualTree(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;

            foreach (var descendant in EnumerateVisualTree(child))
                yield return descendant;
        }
    }

    private sealed class ToolFormSnapshot
    {
        public List<ToolFormState> Controls { get; set; } = [];
    }

    private sealed class ToolFormState
    {
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string? StringValue { get; set; }
        public bool? BoolValue { get; set; }
        public int? IntValue { get; set; }
    }
}
