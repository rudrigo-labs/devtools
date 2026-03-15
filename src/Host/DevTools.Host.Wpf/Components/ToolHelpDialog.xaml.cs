using System.Windows;
using DevTools.Host.Wpf.Services;

namespace DevTools.Host.Wpf.Components;

public partial class ToolHelpDialog : Window
{
    private ToolHelpDialog(Window? owner, ToolHelpContent content)
    {
        InitializeComponent();

        if (owner is not null)
            Owner = owner;
        else
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

        DialogTitleText.Text = content.Title;
        ObjetivoText.Text = content.Objetivo;
        ComoUsarText.Text = content.ComoUsar;
        ExemploText.Text = content.Exemplo;
        ObservacoesText.Text = content.Observacoes;
    }

    public static void Show(Window? owner, ToolHelpContent content)
    {
        var dialog = new ToolHelpDialog(owner, content);
        dialog.ShowDialog();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();
}
