using DevTools.Core.Configuration;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Components;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;

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
            {
                var replaceCard = CreateMaterialCard();
                var replaceStack = new StackPanel();
                replaceStack.Children.Add(CreateCardHeader("FormTextbox", "Substituição de Texto"));
                replaceStack.Children.Add(CreateGridWithTwoLabeledTextBoxes("Texto Antigo", "old-text", options, "Texto Novo", "new-text", options));
                replaceCard.Child = replaceStack;
                container.Children.Add(replaceCard);

                var filterCard = CreateMaterialCard();
                var filterStack = new StackPanel();
                filterStack.Children.Add(CreateCardHeader("FilterVariant", "Filtros (Glob)"));
                filterStack.Children.Add(CreateGridWithTwoLabeledTextBoxes("Include", "include", options, "Exclude", "exclude", options));
                filterCard.Child = filterStack;
                container.Children.Add(filterCard);
                break;
            }

            case "Migrations":
            {
                var pathCard = CreateMaterialCard();
                var pathStack = new StackPanel();
                pathStack.Children.Add(CreateCardHeader("Folder", "Caminhos do Projeto"));
                pathStack.Children.Add(CreateGridWithTwoPathSelectors(
                    "Pasta Raiz",
                    "root-path",
                    true,
                    "Startup Project (.csproj)",
                    "startup-path",
                    false,
                    options));
                pathCard.Child = pathStack;
                container.Children.Add(pathCard);

                var targetsCard = CreateMaterialCard();
                var targetsStack = new StackPanel();
                targetsStack.Children.Add(CreateCardHeader("Database", "Projetos de Migrations por Provider"));
                targetsStack.Children.Add(CreateGridWithTwoPathSelectors(
                    "Projeto SQL Server (.csproj)",
                    "target-sqlserver-path",
                    false,
                    "Projeto SQLite (.csproj)",
                    "target-sqlite-path",
                    false,
                    options));
                targetsCard.Child = targetsStack;
                container.Children.Add(targetsCard);

                var contextCard = CreateMaterialCard();
                var contextStack = new StackPanel();
                contextStack.Children.Add(CreateCardHeader("DatabaseSettings", "Configuração do Contexto"));
                contextStack.Children.Add(CreateMigrationsContextGrid(options));
                contextCard.Child = contextStack;
                container.Children.Add(contextCard);
                break;
            }

            case "Harvest":
            {
                var pathCard = CreateMaterialCard();
                var pathStack = new StackPanel();
                pathStack.Children.Add(CreateCardHeader("FolderArrowRight", "Caminhos"));
                pathStack.Children.Add(CreatePathSelector("Origem", "source-path", options, isFolderPicker: true));
                pathStack.Children.Add(CreatePathSelector("Destino", "output-path", options, isFolderPicker: true, new Thickness(0, 16, 0, 0)));
                pathCard.Child = pathStack;
                container.Children.Add(pathCard);

                var limitCard = CreateMaterialCard();
                var limitStack = new StackPanel();
                limitStack.Children.Add(CreateCardHeader("ChartLine", "Limites de Coleta"));
                limitStack.Children.Add(CreateLabeledTextBox("Score Mínimo (0-100)", "min-score", options));
                limitCard.Child = limitStack;
                container.Children.Add(limitCard);
                break;
            }

            case "SearchText":
            {
                var pathCard = CreateMaterialCard();
                var pathStack = new StackPanel();
                pathStack.Children.Add(CreateCardHeader("FolderSearch", "Diretório de Busca"));
                pathStack.Children.Add(CreatePathSelector("Pasta Raiz", "root-path", options, isFolderPicker: true));
                pathCard.Child = pathStack;
                container.Children.Add(pathCard);

                var configCard = CreateMaterialCard();
                var configStack = new StackPanel();
                configStack.Children.Add(CreateCardHeader("Magnify", "Configuração da Busca"));
                configStack.Children.Add(CreateLabeledTextBox("Padrão de Busca", "search-pattern", options));
                configStack.Children.Add(CreateGridWithTwoLabeledTextBoxes("Include (Glob)", "include", options, "Exclude (Glob)", "exclude", options, new Thickness(0, 16, 0, 0)));
                configCard.Child = configStack;
                container.Children.Add(configCard);
                break;
            }

            case "Snapshot":
            {
                var card = CreateMaterialCard();
                var stack = new StackPanel();
                stack.Children.Add(CreateCardHeader("PackageVariant", "Snapshot do Projeto"));
                stack.Children.Add(CreatePathSelector("Pasta do Projeto", "project-path", options, isFolderPicker: true));
                stack.Children.Add(CreatePathSelector("Pasta de Saida (Opcional)", "output-base-path", options, isFolderPicker: true, new Thickness(0, 16, 0, 0)));
                stack.Children.Add(CreateLabeledTextBox("Diretorios Ignorados (separados por virgula)", "ignored-directories", options, new Thickness(0, 16, 0, 0)));
                stack.Children.Add(CreateLabeledTextBox("Tamanho Maximo por Arquivo (KB) (Opcional)", "max-file-size-kb", options, new Thickness(0, 16, 0, 0)));
                card.Child = stack;
                container.Children.Add(card);
                break;
            }

            case "SSHTunnel":
            {
                var connectionCard = CreateMaterialCard();
                var connectionStack = new StackPanel();
                connectionStack.Children.Add(CreateCardHeader("ServerNetwork", "Dados de Conexão (SSH)"));
                connectionStack.Children.Add(CreateLabeledTextBox("Nome do Perfil", "ssh-profile-name", options));
                connectionStack.Children.Add(CreateSshConnectionGrid(options, new Thickness(0, 16, 0, 0)));
                connectionStack.Children.Add(CreateLabeledTextBox("Usuário SSH", "ssh-user", options, new Thickness(0, 16, 0, 0)));
                connectionStack.Children.Add(CreatePathSelector("Caminho da Chave Privada (.pem/.ppk)", "identity-file", options, false, new Thickness(0, 16, 0, 0)));
                connectionCard.Child = connectionStack;
                container.Children.Add(connectionCard);

                var tunnelCard = CreateMaterialCard();
                var tunnelStack = new StackPanel();
                tunnelStack.Children.Add(CreateCardHeader("TransitTransfer", "Mapeamento do Túnel"));
                tunnelStack.Children.Add(CreateTunnelMappingGrid(options));
                tunnelCard.Child = tunnelStack;
                container.Children.Add(tunnelCard);
                break;
            }
        }
    }

    private void AddProfileField(StackPanel container, string labelText, string key, Dictionary<string, string> options, bool isPath = false)
    {
        var value = options.TryGetValue(key, out var val) ? val : "";

        if (isPath)
        {
            container.Children.Add(new TextBlock
            {
                Text = labelText,
                Style = (Style)Application.Current.FindResource("DevToolsInputLabel")
            });

            var selector = new PathSelector
            {
                Title = labelText,
                IsFolderPicker = true,
                SelectedPath = value,
                Tag = key
            };

            container.Children.Add(selector);
            return;
        }

        container.Children.Add(new TextBlock
        {
            Text = labelText,
            Style = (Style)Application.Current.FindResource("DevToolsInputLabel")
        });

        container.Children.Add(new System.Windows.Controls.TextBox
        {
            Text = value,
            Tag = key,
            Style = (Style)Application.Current.FindResource("DevToolsTextInput")
        });
    }

    private Border CreateMaterialCard()
    {
        return new Border
        {
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 24),
            Background = System.Windows.Media.Brushes.Transparent,
            CornerRadius = new CornerRadius(0),
            BorderThickness = new Thickness(0)
        };
    }

    private StackPanel CreateCardHeader(string iconKind, string title)
    {
        var headerStack = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 20)
        };

        headerStack.Children.Add(new PackIcon
        {
            Kind = (PackIconKind)System.Enum.Parse(typeof(PackIconKind), iconKind),
            Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("DevToolsAccent"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            Width = 20,
            Height = 20
        });

        headerStack.Children.Add(new TextBlock
        {
            Text = title,
            Style = (Style)Application.Current.FindResource("DevToolsCardTitle")
        });

        return headerStack;
    }

    private StackPanel CreateLabeledTextBox(string labelText, string key, Dictionary<string, string> options, Thickness? margin = null)
    {
        var stack = new StackPanel();
        if (margin.HasValue)
        {
            stack.Margin = margin.Value;
        }

        stack.Children.Add(new TextBlock
        {
            Text = labelText,
            Style = (Style)Application.Current.FindResource("DevToolsInputLabel")
        });

        stack.Children.Add(new System.Windows.Controls.TextBox
        {
            Text = options.TryGetValue(key, out var val) ? val : "",
            Tag = key,
            Style = (Style)Application.Current.FindResource("DevToolsTextInput")
        });

        return stack;
    }

    private UIElement CreatePathSelector(string title, string key, Dictionary<string, string> options, bool isFolderPicker, Thickness? margin = null)
    {
        var selector = new PathSelector
        {
            Title = title,
            IsFolderPicker = isFolderPicker,
            SelectedPath = options.TryGetValue(key, out var val) ? val : "",
            Tag = key
        };

        var stack = new StackPanel();
        if (margin.HasValue)
        {
            stack.Margin = margin.Value;
        }

        stack.Children.Add(selector);
        return stack;
    }

    private Grid CreateGridWithTwoLabeledTextBoxes(string label1, string key1, Dictionary<string, string> options, string label2, string key2, Dictionary<string, string> options2, Thickness? margin = null)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var stack1 = CreateLabeledTextBox(label1, key1, options);
        Grid.SetColumn(stack1, 0);
        grid.Children.Add(stack1);

        var stack2 = CreateLabeledTextBox(label2, key2, options2);
        Grid.SetColumn(stack2, 2);
        grid.Children.Add(stack2);

        if (margin.HasValue)
        {
            grid.Margin = margin.Value;
        }

        return grid;
    }

    private Grid CreateGridWithTwoPathSelectors(
        string title1,
        string key1,
        bool isFolderPicker1,
        string title2,
        string key2,
        bool isFolderPicker2,
        Dictionary<string, string> options,
        Thickness? margin = null)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = CreatePathSelector(title1, key1, options, isFolderPicker1);
        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var right = CreatePathSelector(title2, key2, options, isFolderPicker2);
        Grid.SetColumn(right, 2);
        grid.Children.Add(right);

        if (margin.HasValue)
        {
            grid.Margin = margin.Value;
        }

        return grid;
    }

    private Grid CreateMigrationsContextGrid(Dictionary<string, string> options)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var dbContext = CreateLabeledTextBox("DbContext Full Name", "dbcontext", options);
        Grid.SetColumn(dbContext, 0);
        grid.Children.Add(dbContext);

        var right = new StackPanel();
        right.Children.Add(new TextBlock
        {
            Text = "Argumentos Adicionais",
            Style = (Style)Application.Current.FindResource("DevToolsInputLabel")
        });

        var argsInput = new System.Windows.Controls.TextBox
        {
            Text = options.TryGetValue("additional-args", out var val) ? val : "",
            Tag = "additional-args",
            Style = (Style)Application.Current.FindResource("DevToolsTextInput")
        };

        right.Children.Add(argsInput);
        right.Children.Add(new TextBlock
        {
            Text = "Atalhos comuns (clique para adicionar):",
            Style = (Style)Application.Current.FindResource("DevToolsInputLabel"),
            Margin = new Thickness(0, 10, 0, 6)
        });

        var chips = new WrapPanel();
        chips.Children.Add(CreateArgChipButton("--verbose", argsInput));
        chips.Children.Add(CreateArgChipButton("--no-build", argsInput));
        chips.Children.Add(CreateArgChipButton("--no-color", argsInput));
        chips.Children.Add(CreateArgChipButton("--prefix-output", argsInput));
        right.Children.Add(chips);

        Grid.SetColumn(right, 2);
        grid.Children.Add(right);

        return grid;
    }

    private static System.Windows.Controls.Button CreateArgChipButton(string argToken, System.Windows.Controls.TextBox targetInput)
    {
        var button = new System.Windows.Controls.Button
        {
            Content = argToken,
            MinWidth = 0,
            MinHeight = 30,
            Width = double.NaN,
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 0, 6, 6),
            Style = (Style)Application.Current.FindResource("DevToolsBrowseButton")
        };

        button.Click += (_, _) =>
        {
            var current = (targetInput.Text ?? string.Empty).Trim();
            if (ContainsArgToken(current, argToken))
                return;

            targetInput.Text = string.IsNullOrWhiteSpace(current) ? argToken : $"{current} {argToken}";
            targetInput.Focus();
            targetInput.CaretIndex = targetInput.Text.Length;
        };

        return button;
    }

    private static bool ContainsArgToken(string args, string token)
    {
        if (string.IsNullOrWhiteSpace(args) || string.IsNullOrWhiteSpace(token))
            return false;

        var pattern = $@"(?<!\S){Regex.Escape(token)}(?=\s|$|=)";
        return Regex.IsMatch(args, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private Grid CreateTunnelMappingGrid(Dictionary<string, string> options)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var localBind = CreateLabeledTextBox("Bind Local (IP)", "local-bind", options);
        Grid.SetColumn(localBind, 0);
        grid.Children.Add(localBind);

        var localPort = CreateLabeledTextBox("Porta Local", "local-port", options);
        localPort.Width = 80;
        localPort.Margin = new Thickness(5, 0, 0, 0);
        Grid.SetColumn(localPort, 1);
        grid.Children.Add(localPort);

        var arrowIcon = new PackIcon
        {
            Kind = PackIconKind.ArrowRight,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("DevToolsTextSecondary"),
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetColumn(arrowIcon, 2);
        grid.Children.Add(arrowIcon);

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

    private Grid CreateSshConnectionGrid(Dictionary<string, string> options, Thickness? margin = null)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var hostStack = CreateLabeledTextBox("Host SSH (Servidor)", "ssh-host", options);
        Grid.SetColumn(hostStack, 0);
        grid.Children.Add(hostStack);

        var portStack = CreateLabeledTextBox("Porta", "ssh-port", options);
        portStack.Width = 140;
        Grid.SetColumn(portStack, 2);
        grid.Children.Add(portStack);

        if (margin.HasValue)
        {
            grid.Margin = margin.Value;
        }

        return grid;
    }
}
