using System.Windows;
using System.Windows.Controls;

namespace DevTools.Host.Wpf.Components;

public class SidebarNavItem : System.Windows.Controls.Button
{
    static SidebarNavItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SidebarNavItem),
            new FrameworkPropertyMetadata(typeof(SidebarNavItem)));
    }

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(SidebarNavItem),
            new PropertyMetadata("\uE8A7"));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(SidebarNavItem),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsSubItemProperty =
        DependencyProperty.Register(
            nameof(IsSubItem),
            typeof(bool),
            typeof(SidebarNavItem),
            new PropertyMetadata(false));

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public bool IsSubItem
    {
        get => (bool)GetValue(IsSubItemProperty);
        set => SetValue(IsSubItemProperty, value);
    }
}
