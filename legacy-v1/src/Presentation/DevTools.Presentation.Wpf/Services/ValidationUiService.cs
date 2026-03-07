using System;
using System.Collections.Generic;
using DevTools.Presentation.Wpf.Components;

namespace DevTools.Presentation.Wpf.Services;

public static class ValidationUiService
{
    private static readonly Dictionary<System.Windows.UIElement, System.Windows.Documents.Adorner> ValidationAsteriskAdorners = new();
    private const string RequiredFieldsPrefix = "Os campos abaixo nao podem ficar em branco:\n- ";

    public static void ShowInline(DevToolsToolFrame frame, string message)
    {
        if (frame == null)
        {
            return;
        }

        frame.StatusText = Format(message);
    }

    public static void ClearInline(DevToolsToolFrame frame)
    {
        if (frame == null)
        {
            return;
        }

        if (IsValidationStatus(frame.StatusText))
        {
            frame.StatusText = string.Empty;
        }
    }

    private static string Format(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Validacao invalida.";
        }

        var singleLine = message
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("  ", " ")
            .Trim();

        singleLine = singleLine
            .Replace("Os campos abaixo nao podem ficar em branco:", "Campos obrigatorios:");

        return $"Validacao: {singleLine}";
    }

    private static bool IsValidationStatus(string? statusText)
    {
        if (string.IsNullOrWhiteSpace(statusText))
        {
            return false;
        }

        return statusText.StartsWith("Validacao:", StringComparison.OrdinalIgnoreCase)
            || statusText.StartsWith("Campos obrigatorios:", StringComparison.OrdinalIgnoreCase);
    }

    public static void SetControlInvalid(System.Windows.Controls.Control? control, bool invalid)
    {
        if (control == null)
            return;

        if (invalid)
        {
            if (System.Windows.Application.Current?.TryFindResource("ErrorBrush") is System.Windows.Media.Brush errorBrush)
            {
                control.BorderBrush = errorBrush;
            }

            control.BorderThickness = new System.Windows.Thickness(1.5);
            SetValidationAsterisk(control, true);
            return;
        }

        control.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
        control.ClearValue(System.Windows.Controls.Control.BorderThicknessProperty);
        SetValidationAsterisk(control, false);
    }

    public static void SetPathSelectorInvalid(PathSelector? selector, bool invalid)
    {
        if (selector == null)
            return;

        var textBox = selector.FindName("PathInput") as System.Windows.Controls.TextBox;
        SetControlInvalid(textBox, invalid);
    }

    public readonly record struct RequiredField(string Label, bool IsMissing, Action<bool> ApplyState);

    public static RequiredField RequiredControl(string label, System.Windows.Controls.Control? control, string? value)
    {
        return new RequiredField(
            Label: label,
            IsMissing: string.IsNullOrWhiteSpace(value),
            ApplyState: invalid => SetControlInvalid(control, invalid));
    }

    public static RequiredField RequiredPath(string label, PathSelector? selector, string? value)
    {
        return new RequiredField(
            Label: label,
            IsMissing: string.IsNullOrWhiteSpace(value),
            ApplyState: invalid => SetPathSelectorInvalid(selector, invalid));
    }

    public static bool ValidateRequiredFields(out string errorMessage, params RequiredField[] fields)
    {
        var missing = new List<string>();
        foreach (var field in fields)
        {
            field.ApplyState(field.IsMissing);
            if (field.IsMissing)
                missing.Add(field.Label);
        }

        if (missing.Count > 0)
        {
            errorMessage = RequiredFieldsPrefix + string.Join("\n- ", missing);
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static void SetValidationAsterisk(System.Windows.UIElement element, bool show)
    {
        if (show)
        {
            if (ValidationAsteriskAdorners.ContainsKey(element))
                return;

            var layer = System.Windows.Documents.AdornerLayer.GetAdornerLayer(element);
            if (layer == null)
                return;

            var adorner = new ValidationAsteriskAdorner(element);
            layer.Add(adorner);
            ValidationAsteriskAdorners[element] = adorner;
            return;
        }

        if (!ValidationAsteriskAdorners.TryGetValue(element, out var existing))
            return;

        var existingLayer = System.Windows.Documents.AdornerLayer.GetAdornerLayer(element);
        existingLayer?.Remove(existing);
        ValidationAsteriskAdorners.Remove(element);
    }

    private sealed class ValidationAsteriskAdorner : System.Windows.Documents.Adorner
    {
        private readonly System.Windows.Controls.TextBlock _asterisk;
        private readonly System.Windows.Media.VisualCollection _visuals;

        public ValidationAsteriskAdorner(System.Windows.UIElement adornedElement)
            : base(adornedElement)
        {
            IsHitTestVisible = false;
            _visuals = new System.Windows.Media.VisualCollection(this);

            _asterisk = new System.Windows.Controls.TextBlock
            {
                Text = "*",
                FontSize = 16,
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = ResolveAsteriskBrush(),
                IsHitTestVisible = false
            };

            _visuals.Add(_asterisk);
        }

        protected override int VisualChildrenCount => _visuals.Count;

        protected override System.Windows.Media.Visual GetVisualChild(int index)
        {
            return _visuals[index];
        }

        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
        {
            var y = -8.0;
            var x = Math.Max(0, finalSize.Width - 10);
            _asterisk.Arrange(new System.Windows.Rect(new System.Windows.Point(x, y), new System.Windows.Size(12, 16)));
            return finalSize;
        }

        private static System.Windows.Media.Brush ResolveAsteriskBrush()
        {
            if (System.Windows.Application.Current?.TryFindResource("ErrorBrush") is System.Windows.Media.Brush errorBrush)
            {
                return errorBrush;
            }

            return System.Windows.Media.Brushes.Red;
        }
    }
}
