using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using DevTools.Core.Configuration;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Utilities;

namespace DevTools.Presentation.Wpf.Components;

public partial class ProfileSelector : System.Windows.Controls.UserControl
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
        try
        {
            var profiles = _profileManager.LoadProfiles(_toolName);
            ProfileCombo.ItemsSource = profiles;
            ProfileCombo.DisplayMemberPath = "Name";
        }
        catch (Exception ex)
        {
            DevToolsMessage.Error($"Falha ao carregar perfis: {ex.Message}", "Erro ao carregar perfis");
        }
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
            DevToolsMessage.Warning("Digite um nome para o perfil.", "Nome necessario");
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

                    DevToolsMessage.Info($"Perfil '{name}' salvo com sucesso.", "Sucesso");
                }
            }
        }
        catch (Exception ex)
        {
            DevToolsMessage.Error($"Falha ao salvar perfil: {ex.Message}", "Erro ao salvar");
        }
    }
    
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_toolName)) return;

        if (ProfileCombo.SelectedItem is ToolProfile profile)
        {
            if (DevToolsMessage.Confirm($"Tem certeza que deseja excluir o perfil '{profile.Name}'?", "Confirmar"))
            {
                try
                {
                    _profileManager.DeleteProfile(_toolName, profile.Name);
                    LoadProfiles();
                }
                catch (Exception ex)
                {
                    DevToolsMessage.Error($"Falha ao excluir perfil: {ex.Message}", "Erro ao excluir");
                }
            }
        }
    }
}
