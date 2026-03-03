using DevTools.Core.Configuration;
using DevTools.Core.Models;
using DevTools.Presentation.Wpf.Components;
using System.Collections.Generic;
using System.Linq;
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
                AddProfileField(container, "Texto Antigo", "old-text", options);
                AddProfileField(container, "Texto Novo", "new-text", options);
                AddProfileField(container, "Include (Glob)", "include", options);
                AddProfileField(container, "Exclude (Glob)", "exclude", options);
                break;
            case "Migrations":
                AddProfileField(container, "Pasta Raiz", "root-path", options, isPath: true);
                AddProfileField(container, "Startup Project", "startup-path", options, isPath: true);
                AddProfileField(container, "DbContext Full Name", "dbcontext", options);
                break;
            case "Harvest":
                AddProfileField(container, "Origem", "source-path", options, isPath: true);
                AddProfileField(container, "Destino", "output-path", options, isPath: true);
                AddProfileField(container, "Score Mínimo", "min-score", options);
                break;
            case "SearchText":
                AddProfileField(container, "Pasta Raiz", "root-path", options, isPath: true);
                AddProfileField(container, "Padrão de Busca", "search-pattern", options);
                AddProfileField(container, "Include (Glob)", "include", options);
                AddProfileField(container, "Exclude (Glob)", "exclude", options);
                break;
            case "Snapshot":
                AddProfileField(container, "Pasta do Projeto", "project-path", options, isPath: true);
                break;
        }
    }

    private void AddProfileField(StackPanel container, string labelText, string key, Dictionary<string, string> options, bool isPath = false)
    {
        var label = new System.Windows.Controls.Label { Content = labelText, Margin = new Thickness(0, 10, 0, 0) };
        container.Children.Add(label);

        string value = options.TryGetValue(key, out var val) ? val : "";

        if (isPath)
        {
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
            var textBox = new System.Windows.Controls.TextBox 
            { 
                Style = (Style)Application.Current.FindResource("InputStyle"),
                Text = value,
                Tag = key
            };
            container.Children.Add(textBox);
        }
    }
}