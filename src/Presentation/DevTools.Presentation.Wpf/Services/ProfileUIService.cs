using DevTools.Core.Configuration;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Components;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;
using MaterialDesignThemes.Wpf; // Adicionado para usar componentes do Material Design

namespace DevTools.Presentation.Wpf.Services;

public class ProfileUIService
{
    private readonly ProfileManager _profileManager;

    public ProfileUIService(ProfileManager profileManager)
    {
        _profileManager = profileManager;
    }

    public List<ToolProfile> LoadProfiles(string toolName)
    {
        return _profileManager.LoadProfiles(toolName);
    }

    public void SaveProfile(string toolName, ToolProfile profile)
    {
        var profiles = _profileManager.LoadProfiles(toolName);

        if (profile.IsDefault)
        {
            foreach (var p in profiles.Where(p => !p.Name.Equals(profile.Name, System.StringComparison.OrdinalIgnoreCase)))
            {
                p.IsDefault = false;
            }
        }

        var existing = profiles.FirstOrDefault(p => p.Name.Equals(profile.Name, System.StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            profiles.Remove(existing);
        }

        profile.UpdatedUtc = System.DateTime.UtcNow;
        profiles.Add(profile);

        _profileManager.SaveProfiles(toolName, profiles);
    }

    public void DeleteProfile(string toolName, string profileName)
    {
        _profileManager.DeleteProfile(toolName, profileName);
    }

    public ToolProfile? GetDefaultProfile(string toolName)
    {
        return _profileManager.GetDefaultProfile(toolName);
    }

    public void GenerateUIForProfile(string toolName, StackPanel container, ToolProfile profile)
    {
        container.Children.Clear();
        var options = profile.Options;

        switch (toolName)
        {
            case "Rename":
                // Grupo: Substituição
                var renameReplaceCard = CreateMaterialCard();
                var renameReplaceStack = new StackPanel();
                renameReplaceStack.Children.Add(CreateCardHeader("FormTextbox", "Substituição de Texto"));
                renameReplaceStack.Children.Add(CreateGridWithTwoLabeledTextBoxes("Texto Antigo", "old-text", options, "Texto Novo", "new-text", options));
                renameReplaceCard.Child = renameReplaceStack;
                container.Children.Add(renameReplaceCard);

                // Grupo: Filtros
                var renameFilterCard = CreateMaterialCard();
                var renameFilterStack = new StackPanel();
                renameFilterStack.Children.Add(CreateCardHeader("FilterVariant", "Filtros (Glob)"));
                renameFilterStack.Children.Add(CreateGridWithTwoLabeledTextBoxes("Include", "include", options, "Exclude", "exclude", options));
                renameFilterCard.Child = renameFilterStack;
                container.Children.Add(renameFilterCard);
                break;

            case "Migrations":
                // Grupo: Caminhos
                var migPathCard = CreateMaterialCard();
                var migPathStack = new StackPanel();
                migPathStack.Children.Add(CreateCardHeader("Folder", "Caminhos do Projeto"));
                migPathStack.Children.Add(CreatePathSelector("Pasta Raiz", "root-path", options, isFolderPicker: true));
                migPathStack.Children.Add(CreatePathSelector("Startup Project", "startup-path", options, isFolderPicker: true, new Thickness(0, 16, 0, 0)));
                migPathCard.Child = migPathStack;
                container.Children.Add(migPathCard);

                // Grupo: Contexto
                var migContextCard = CreateMaterialCard();
                var migContextStack = new StackPanel();
                migContextStack.Children.Add(CreateCardHeader("DatabaseSettings", "Configuração do Contexto"));
                migContextStack.Children.Add(CreateLabeledTextBox("DbContext Full Name", "dbcontext", options));
                migContextCard.Child = migContextStack;
                container.Children.Add(migContextCard);
                break;

            case "Harvest":
                // Grupo: Caminhos
                var harvPathCard = CreateMaterialCard();
                var harvPathStack = new StackPanel();
                harvPathStack.Children.Add(CreateCardHeader("FolderArrowRight", "Caminhos"));
                harvPathStack.Children.Add(CreatePathSelector("Origem", "source-path", options, isFolderPicker: true));
                harvPathStack.Children.Add(CreatePathSelector("Destino", "output-path", options, isFolderPicker: true, new Thickness(0, 16, 0, 0)));
                harvPathCard.Child = harvPathStack;
                container.Children.Add(harvPathCard);

                // Grupo: Limites
                var harvLimitCard = CreateMaterialCard();
                var harvLimitStack = new StackPanel();
                harvLimitStack.Children.Add(CreateCardHeader("ChartLine", "Limites de Coleta"));
                harvLimitStack.Children.Add(CreateLabeledTextBox("Score Mínimo (0-100)", "min-score", options));
                harvLimitCard.Child = harvLimitStack;
                container.Children.Add(harvLimitCard);
                break;

            case "SearchText":
                // Grupo: Caminhos
                var searchPathCard = CreateMaterialCard();
                var searchPathStack = new StackPanel();
                searchPathStack.Children.Add(CreateCardHeader("FolderSearch", "Diretório de Busca"));
                searchPathStack.Children.Add(CreatePathSelector("Pasta Raiz", "root-path", options, isFolderPicker: true));
                searchPathCard.Child = searchPathStack;
                container.Children.Add(searchPathCard);

                // Grupo: Busca
                var searchConfigCard = CreateMaterialCard();
                var searchConfigStack = new StackPanel();
                searchConfigStack.Children.Add(CreateCardHeader("Magnify", "Configuração da Busca"));
                searchConfigStack.Children.Add(CreateLabeledTextBox("Padrão de Busca", "search-pattern", options));
                searchConfigStack.Children.Add(CreateGridWithTwoLabeledTextBoxes("Include (Glob)", "include", options, "Exclude (Glob)", "exclude", options, new Thickness(0, 16, 0, 0)));
                searchConfigCard.Child = searchConfigStack;
                container.Children.Add(searchConfigCard);
                break;

            case "Snapshot":
                // Grupo: Projeto
                var snapCard = CreateMaterialCard();
                var snapStack = new StackPanel();
                snapStack.Children.Add(CreateCardHeader("PackageVariant", "Snapshot do Projeto"));
                snapStack.Children.Add(CreatePathSelector("Pasta do Projeto", "project-path", options, isFolderPicker: true));
                snapCard.Child = snapStack;
                container.Children.Add(snapCard);
                break;

            case "SSHTunnel":
                // Grupo: Dados de Conexão
                var connectionCard = CreateMaterialCard();
                var connectionStack = new StackPanel();
                connectionStack.Children.Add(CreateCardHeader("ServerNetwork", "Dados de Conexão (SSH)"));
                connectionStack.Children.Add(CreateLabeledTextBox("Nome do Perfil", "ssh-profile-name", options));
                connectionStack.Children.Add(CreateGridWithTwoLabeledTextBoxes("Host SSH (Servidor)", "ssh-host", options, "Porta", "ssh-port", options, new Thickness(0, 16, 0, 0)));
                connectionStack.Children.Add(CreateLabeledTextBox("Usuário SSH", "ssh-user", options, new Thickness(0, 16, 0, 0)));
                connectionStack.Children.Add(CreatePathSelector("Caminho da Chave Privada (.pem/.ppk)", "identity-file", options, false, new Thickness(0, 16, 0, 0)));
                connectionCard.Child = connectionStack;
                container.Children.Add(connectionCard);

                // Grupo: Mapeamento do Túnel
                var tunnelCard = CreateMaterialCard();
                var tunnelStack = new StackPanel();
                tunnelStack.Children.Add(CreateCardHeader("TransitTransfer", "Mapeamento do Túnel"));
                tunnelStack.Children.Add(CreateTunnelMappingGrid(options));
                tunnelCard.Child = tunnelStack;
                container.Children.Add(tunnelCard);
                break;
        }
    }

    private void AddProfileField(StackPanel container, string labelText, string key, Dictionary<string, string> options, bool isPath = false)
    {
        string value = options.TryGetValue(key, out var val) ? val : "";

        if (isPath)
        {
            var textBlock = new TextBlock { Text = labelText, Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("SecondaryTextBrush"), FontSize = 12, Margin = new Thickness(0, 0, 0, 6) };
            container.Children.Add(textBlock);
            var selector = new PathSelector 
            { 
                Title = labelText, 
                IsFolderPicker = true,
                SelectedPath = value
            };
            selector.Tag = key;
            container.Children.Add(selector);
        }
        else
        {
            var textBlock = new TextBlock { Text = labelText, Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("SecondaryTextBrush"), FontSize = 12, Margin = new Thickness(0, 0, 0, 6) };
            container.Children.Add(textBlock);
            var textBox = new System.Windows.Controls.TextBox 
            { 
                Text = value,
                Tag = key,
                Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("PrimaryTextBrush")
            };
            container.Children.Add(textBox);
        }
    }

    // Métodos auxiliares para criar componentes visuais (estilo Dashboard)
    private Border CreateMaterialCard()
    {
        return new Border
        {
            Padding = new Thickness(20),
            Margin = new Thickness(0, 0, 0, 24),
            Background = (System.Windows.Media.Brush)Application.Current.FindResource("MaterialDesignCardBackground"),
            CornerRadius = new CornerRadius(8),
            BorderBrush = (System.Windows.Media.Brush)Application.Current.FindResource("DevToolsBrushBorder"),
            BorderThickness = new Thickness(1)
        };
    }

    private StackPanel CreateCardHeader(string iconKind, string title)
    {
        var headerStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 20) };
        headerStack.Children.Add(new PackIcon { Kind = (PackIconKind)System.Enum.Parse(typeof(PackIconKind), iconKind), Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("AccentBrush"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0), Width = 20, Height = 20 });
        headerStack.Children.Add(new TextBlock { Text = title, FontSize = 16, Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("PrimaryTextBrush"), FontWeight = FontWeights.SemiBold });
        return headerStack;
    }

    private StackPanel CreateLabeledTextBox(string labelText, string key, Dictionary<string, string> options, Thickness? margin = null)
    {
        var stack = new StackPanel();
        if (margin.HasValue) stack.Margin = margin.Value;

        stack.Children.Add(new TextBlock { Text = labelText, Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("SecondaryTextBrush"), FontSize = 11, Margin = new Thickness(0, 0, 0, 6) });
        
        var textBox = new System.Windows.Controls.TextBox
        {
            Text = options.TryGetValue(key, out var val) ? val : "",
            Tag = key,
            Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("PrimaryTextBrush")
        };
        stack.Children.Add(textBox);
        return stack;
    }

    private UIElement CreatePathSelector(string title, string key, Dictionary<string, string> options, bool isFolderPicker, Thickness? margin = null)
    {
        var textBlock = new TextBlock { Text = title, Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("SecondaryTextBrush"), FontSize = 12, Margin = new Thickness(0, 0, 0, 6) };
        var selector = new PathSelector
        {
            Title = title,
            IsFolderPicker = isFolderPicker,
            SelectedPath = options.TryGetValue(key, out var val) ? val : ""
        };
        selector.Tag = key;
        var stack = new StackPanel();
        if (margin.HasValue) stack.Margin = margin.Value;
        stack.Children.Add(textBlock);
        stack.Children.Add(selector);
        return stack; 
    }

    private Grid CreateGridWithTwoLabeledTextBoxes(string label1, string key1, Dictionary<string, string> options, string label2, string key2, Dictionary<string, string> options2, Thickness? margin = null)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) }); // Spacer
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var stack1 = CreateLabeledTextBox(label1, key1, options);
        stack1.Margin = new Thickness(0, 0, 10, 0);
        Grid.SetColumn(stack1, 0);
        grid.Children.Add(stack1);

        var stack2 = CreateLabeledTextBox(label2, key2, options2);
        Grid.SetColumn(stack2, 2);
        grid.Children.Add(stack2);

        if (margin.HasValue) grid.Margin = margin.Value;
        return grid;
    }

    private Grid CreateTunnelMappingGrid(Dictionary<string, string> options)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) }); // Spacer
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Local
        var localBind = CreateLabeledTextBox("Bind Local (IP)", "local-bind", options);
        Grid.SetColumn(localBind, 0);
        grid.Children.Add(localBind);

        var localPort = CreateLabeledTextBox("Porta Local", "local-port", options);
        localPort.Width = 80;
        localPort.Margin = new Thickness(5, 0, 0, 0);
        Grid.SetColumn(localPort, 1);
        grid.Children.Add(localPort);

        // Arrow
        var arrowIcon = new PackIcon { Kind = PackIconKind.ArrowRight, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("SecondaryTextBrush"), Margin = new Thickness(0, 0, 0, 10) };
        Grid.SetColumn(arrowIcon, 2);
        grid.Children.Add(arrowIcon);

        // Remote
        var remoteHost = CreateLabeledTextBox("Host Remoto", "remote-host", options);
        Grid.SetColumn(remoteHost, 3);
        grid.Children.Add(remoteHost);

        var remotePort = CreateLabeledTextBox("Porta Remota", "remote-port", options);
        remotePort.Width = 80;
        remotePort.Margin = new Thickness(5, 0, 0, 0);
        Grid.SetColumn(remotePort, 4);
        grid.Children.Add(remotePort);

        return grid;
    }
}