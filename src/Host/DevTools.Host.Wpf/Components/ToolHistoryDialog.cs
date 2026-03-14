using DevTools.Core.Models;
using System.Windows;
using System.Linq;
using WpfControls = System.Windows.Controls;

namespace DevTools.Host.Wpf.Components;

public sealed class ToolHistoryDialog : Window
{
    private readonly WpfControls.ListBox _listBox;
    private readonly IReadOnlyList<ToolUsageHistoryEntry> _entries;
    public ToolUsageHistoryEntry? SelectedEntry { get; private set; }

    public ToolHistoryDialog(string toolName, IReadOnlyList<ToolUsageHistoryEntry> entries)
    {
        _entries = entries;

        Title = $"Histórico — {toolName}";
        Width = 640;
        Height = 420;
        MinWidth = 560;
        MinHeight = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;

        var grid = new WpfControls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new WpfControls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new WpfControls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.RowDefinitions.Add(new WpfControls.RowDefinition { Height = System.Windows.GridLength.Auto });

        var info = new WpfControls.TextBlock
        {
            Text = "Selecione um registro para preencher automaticamente os campos da tela.",
            Margin = new Thickness(0, 0, 0, 10),
            TextWrapping = TextWrapping.Wrap
        };
        WpfControls.Grid.SetRow(info, 0);
        grid.Children.Add(info);

        _listBox = new WpfControls.ListBox
        {
            DisplayMemberPath = nameof(HistoryListItem.Label),
            ItemsSource = entries
                .OrderByDescending(x => x.UsedAtUtc)
                .Select(x => new HistoryListItem(x))
                .ToList()
        };
        _listBox.MouseDoubleClick += (_, _) => ApplySelection();
        WpfControls.Grid.SetRow(_listBox, 1);
        grid.Children.Add(_listBox);

        var buttonPanel = new WpfControls.StackPanel
        {
            Orientation = WpfControls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };

        var cancelButton = new WpfControls.Button
        {
            Content = "Fechar",
            Width = 100,
            Margin = new Thickness(0, 0, 8, 0)
        };
        cancelButton.Click += (_, _) => Close();

        var applyButton = new WpfControls.Button
        {
            Content = "Aplicar",
            Width = 100
        };
        applyButton.Click += (_, _) => ApplySelection();

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(applyButton);

        WpfControls.Grid.SetRow(buttonPanel, 2);
        grid.Children.Add(buttonPanel);

        Content = grid;
    }

    private void ApplySelection()
    {
        if (_listBox.SelectedItem is not HistoryListItem item)
            return;

        SelectedEntry = _entries.FirstOrDefault(x => x.Id == item.Id);
        if (SelectedEntry is null)
            return;

        DialogResult = true;
        Close();
    }

    private sealed class HistoryListItem
    {
        public HistoryListItem(ToolUsageHistoryEntry entry)
        {
            Id = entry.Id;
            Label = $"{entry.UsedAtUtc:dd/MM/yyyy HH:mm} — {entry.Title}";
        }

        public string Id { get; }
        public string Label { get; }
    }
}
