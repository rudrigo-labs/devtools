using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using DevTools.Core.Configuration;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Components;

public partial class ProfileSelector : System.Windows.Controls.UserControl
{
    private const string CreateNewProfileLabel = "Criar novo perfil...";

    private readonly ProfileManager _profileManager;
    private string? _toolName;
    
    public static readonly DependencyProperty ToolNameProperty = DependencyProperty.Register(
        nameof(ToolName), typeof(string), typeof(ProfileSelector), new PropertyMetadata(null, OnToolNameChanged));

    public string? ToolName
    {
        get => (string?)GetValue(ToolNameProperty);
        set => SetValue(ToolNameProperty, value);
    }

    public ToolProfile? SelectedProfile => ProfileCombo.SelectedItem as ToolProfile;
    
    public event Action<ToolProfile>? ProfileLoaded;
    public Func<Dictionary<string, string>>? GetOptionsFunc { get; set; }
    
    public ProfileSelector()
    {
        InitializeComponent();
        _profileManager = new ProfileManager();
    }
    
    private static void OnToolNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProfileSelector ctrl)
        {
            ctrl._toolName = (string?)e.NewValue;
            ctrl.LoadProfiles();
        }
    }
    
    public void LoadProfiles()
    {
        if (string.IsNullOrEmpty(_toolName)) return;
        try
        {
            var profiles = _profileManager.LoadProfiles(_toolName);

            if (profiles.Count == 0)
            {
                ProfileRowGrid.Visibility = Visibility.Collapsed;
                CreateProfileButton.Visibility = Visibility.Visible;
                ProfileCombo.ItemsSource = null;
                return;
            }

            ProfileRowGrid.Visibility = Visibility.Visible;
            CreateProfileButton.Visibility = Visibility.Collapsed;

            SaveButton.Visibility = Visibility.Collapsed;

            ProfileCombo.IsEditable = false;

            var items = new List<object>();
            items.Add(new ToolProfile { Name = CreateNewProfileLabel, Options = new Dictionary<string, string>(), UpdatedUtc = DateTime.UtcNow });
            foreach (var p in profiles)
                items.Add(p);

            ProfileCombo.ItemsSource = items;
            ProfileCombo.DisplayMemberPath = "Name";
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Falha ao carregar perfis.", "Erro ao carregar perfis", ex);
        }
    }
    
    private void ProfileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is ToolProfile profile)
        {
            if (profile.Name == CreateNewProfileLabel)
            {
                CreateProfileForCurrentTool();
                return;
            }

            ProfileLoaded?.Invoke(profile);
        }
    }
    
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_toolName)) return;

        var name = ProfileCombo.Text;
        if (string.IsNullOrWhiteSpace(name))
        {
            UiMessageService.ShowError("Digite um nome para o perfil.", "Nome necessario");
            return;
        }
        
        try
        {
            if (GetOptionsFunc != null)
            {
                var options = GetOptionsFunc();
                if (options != null)
                {
                    var profile = new ToolProfile { Name = name, Options = options, UpdatedUtc = DateTime.UtcNow };
                    _profileManager.SaveProfile(_toolName, profile);

                    LoadProfiles();

                    foreach (var item in ProfileCombo.Items)
                    {
                        if (item is ToolProfile p && p.Name == name)
                        {
                            ProfileCombo.SelectedItem = item;
                            break;
                        }
                    }

                    UiMessageService.ShowInfo($"Perfil '{name}' salvo com sucesso.", "Sucesso");
                }
            }
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Falha ao salvar perfil.", "Erro ao salvar", ex);
        }
    }
    
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_toolName)) return;

        if (ProfileCombo.SelectedItem is ToolProfile profile)
        {
            if (profile.Name == CreateNewProfileLabel)
                return;

            if (UiMessageService.Confirm($"Tem certeza que deseja excluir o perfil '{profile.Name}'?", "Confirmar"))
            {
                try
                {
                    _profileManager.DeleteProfile(_toolName, profile.Name);
                    LoadProfiles();
                }
                catch (Exception ex)
                {
                    UiMessageService.ShowError("Falha ao excluir perfil.", "Erro ao excluir", ex);
                }
            }
        }
    }

    private void CreateProfileButton_Click(object sender, RoutedEventArgs e)
    {
        CreateProfileForCurrentTool();
    }

    private void CreateProfileForCurrentTool()
    {
        if (string.IsNullOrEmpty(_toolName)) return;
        if (GetOptionsFunc == null) return;

        var name = PromptProfileName();
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            var options = GetOptionsFunc();
            if (options == null) return;

            var profile = new ToolProfile { Name = name, Options = options, UpdatedUtc = DateTime.UtcNow };
            _profileManager.SaveProfile(_toolName, profile);

            LoadProfiles();

            foreach (var item in ProfileCombo.Items)
            {
                if (item is ToolProfile p && p.Name == name)
                {
                    ProfileCombo.SelectedItem = item;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            UiMessageService.ShowError("Falha ao salvar perfil.", "Erro ao salvar", ex);
        }
    }

    private string? PromptProfileName()
    {
        var owner = Window.GetWindow(this);

        var window = new System.Windows.Window
        {
            Title = "Novo perfil",
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            Background = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("WindowBackgroundBrush"),
            ShowInTaskbar = false
        };

        window.BorderThickness = new System.Windows.Thickness(1);
        window.BorderBrush = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("BorderBrush");

        var rootBorder = new System.Windows.Controls.Border
        {
            CornerRadius = new System.Windows.CornerRadius(8),
            Background = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("WindowBackgroundBrush"),
            BorderBrush = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("BorderBrush"),
            BorderThickness = new System.Windows.Thickness(1),
            Padding = new System.Windows.Thickness(20)
        };

        var panel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical };

        var label = new System.Windows.Controls.TextBlock
        {
            Text = "Nome do perfil",
            Margin = new System.Windows.Thickness(0, 0, 0, 5),
            Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("PrimaryTextBrush")
        };

        var textBox = new System.Windows.Controls.TextBox
        {
            MinWidth = 260,
            Style = (System.Windows.Style)System.Windows.Application.Current.FindResource("ModernTextBoxStyle")
        };

        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new System.Windows.Thickness(0, 10, 0, 0)
        };

        var okButton = new System.Windows.Controls.Button
        {
            Content = "Salvar",
            IsDefault = true,
            MinWidth = 100,
            Margin = new System.Windows.Thickness(0, 0, 10, 0),
            Style = (System.Windows.Style)System.Windows.Application.Current.FindResource("PrimaryButtonStyle")
        };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "Cancelar",
            IsCancel = true,
            MinWidth = 100,
            Style = (System.Windows.Style)System.Windows.Application.Current.FindResource("SecondaryButtonStyle")
        };

        okButton.Click += (_, _) =>
        {
            window.DialogResult = true;
            window.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            window.DialogResult = false;
            window.Close();
        };

        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);

        panel.Children.Add(label);
        panel.Children.Add(textBox);
        panel.Children.Add(buttons);

        rootBorder.Child = panel;
        window.Content = rootBorder;

        var result = window.ShowDialog();
        if (result == true)
            return textBox.Text;

        return null;
    }
}
