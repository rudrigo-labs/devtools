using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace DevTools.Host.Wpf.Components;

public class SectionExpander : HeaderedContentControl
{
    static SectionExpander()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SectionExpander),
            new FrameworkPropertyMetadata(typeof(SectionExpander)));
    }

    // ── IsExpanded ───────────────────────────────────────────────────────────

    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(SectionExpander),
            new PropertyMetadata(true));

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    // ── Toggle via ToggleButton no template ──────────────────────────────────

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PART_Header") is ToggleButton toggle)
        {
            toggle.Checked   += (_, _) => IsExpanded = true;
            toggle.Unchecked += (_, _) => IsExpanded = false;
        }
    }
}
