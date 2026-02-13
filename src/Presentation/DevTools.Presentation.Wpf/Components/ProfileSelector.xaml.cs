using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using DevTools.Core.Configuration;
using DevTools.Core.Models;

namespace DevTools.Presentation.Wpf.Components;

public partial class ProfileSelector : UserControl
{
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
        
        var profiles = _profileManager.LoadProfiles(_toolName);
        ProfileCombo.ItemsSource = profiles;
        ProfileCombo.DisplayMemberPath = "Name";
    }
    
    private void ProfileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is ToolProfile profile)
        {
            ProfileLoaded?.Invoke(profile);
        }
    }
    
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_toolName)) return;

        var name = ProfileCombo.Text;
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Digite um nome para o perfil.", "Nome necessario", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (GetOptionsFunc != null)
        {
            var options = GetOptionsFunc();
            if (options != null)
            {
                var profile = new ToolProfile { Name = name, Options = options, UpdatedUtc = DateTime.UtcNow };
                _profileManager.SaveProfile(_toolName, profile);
                
                LoadProfiles();
                
                // Reselect
                foreach(var item in ProfileCombo.Items)
                {
                    if (item is ToolProfile p && p.Name == name)
                    {
                        ProfileCombo.SelectedItem = item;
                        break;
                    }
                }
                
                MessageBox.Show($"Perfil '{name}' salvo com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
    
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_toolName)) return;

        if (ProfileCombo.SelectedItem is ToolProfile profile)
        {
            if (MessageBox.Show($"Tem certeza que deseja excluir o perfil '{profile.Name}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _profileManager.DeleteProfile(_toolName, profile.Name);
                LoadProfiles();
            }
        }
    }
}
