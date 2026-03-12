using System;
using System.Collections.Generic;
using DevTools.Host.Wpf.Components;

namespace DevTools.Host.Wpf.Services;

/// <summary>
/// Serviço de validação de UI. Adorners são gerenciados por instância de elemento,
/// sem dicionário estático global para evitar memory leak entre views.
/// </summary>
public static class ValidationUiService
{
    private const string RequiredFieldsPrefix = "Os campos abaixo nao podem ficar em branco:\n- ";

    public static void ShowInline(DevToolsToolFrame frame, string message)
    {
        if (frame is null) return;
        frame.StatusText = Format(message);
    }

    public static void ShowInline(System.Windows.Controls.TextBlock? statusText, string message)
    {
        if (statusText is null) return;
        statusText.Text = Format(message);
    }

    public static void ClearInline(DevToolsToolFrame frame)
    {
        if (frame is null) return;
        if (IsValidationStatus(frame.StatusText))
            frame.StatusText = string.Empty;
    }

    public static void ClearInline(System.Windows.Controls.TextBlock? statusText)
    {
        if (statusText is null) return;
        if (IsValidationStatus(statusText.Text))
            statusText.Text = string.Empty;
    }

    private static string Format(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Validacao invalida.";

        var singleLine = message
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("  ", " ")
            .Trim();

        singleLine = singleLine.Replace(
            "Os campos abaixo nao podem ficar em branco:",
            "Campos obrigatorios:");

        return $"Validacao: {singleLine}";
    }

    private static bool IsValidationStatus(string? statusText)
    {
        if (string.IsNullOrWhiteSpace(statusText)) return false;
        return statusText.StartsWith("Validacao:", StringComparison.OrdinalIgnoreCase)
            || statusText.StartsWith("Campos obrigatorios:", StringComparison.OrdinalIgnoreCase);
    }

    public static void SetControlInvalid(System.Windows.Controls.Control? control, bool invalid)
    {
        if (control is null) return;

        if (invalid)
        {
            if (System.Windows.Application.Current?.TryFindResource("ErrorBrush")
                is System.Windows.Media.Brush errorBrush)
                control.BorderBrush = errorBrush;

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
        if (selector is null) return;
        var textBox = selector.FindName("PathInput") as System.Windows.Controls.TextBox;
        SetControlInvalid(textBox, invalid);
    }

    public readonly record struct RequiredField(string Label, bool IsMissing, Action<bool> ApplyState);

    public static RequiredField RequiredControl(string label, System.Windows.Controls.Control? control, string? value)
        => new(label, string.IsNullOrWhiteSpace(value), invalid => SetControlInvalid(control, invalid));

    public static RequiredField RequiredPath(string label, PathSelector? selector, string? value)
        => new(label, string.IsNullOrWhiteSpace(value), invalid => SetPathSelectorInvalid(selector, invalid));

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

    // -------------------------------------------------------------------------
    // Adorners — gerenciados por elemento individual via tag, sem dicionário estático.
    // -------------------------------------------------------------------------

    private static void SetValidationAsterisk(System.Windows.UIElement element, bool show)
    {
        var layer = System.Windows.Documents.AdornerLayer.GetAdornerLayer(element);
        if (layer is null) return;

        // Adorner existente fica na tag do elemento para evitar dicionário estático global.
        var existing = GetStoredAdorner(element);

        if (show)
        {
            if (existing is not null) return; // já existe
            var adorner = new ValidationAsteriskAdorner(element);
            layer.Add(adorner);
            StoreAdorner(element, adorner);
            return;
        }

        if (existing is null) return;
        layer.Remove(existing);
        StoreAdorner(element, null);
    }

    private static ValidationAsteriskAdorner? GetStoredAdorner(System.Windows.UIElement element)
    {
        if (element is System.Windows.FrameworkElement fe)
            return fe.Tag as ValidationAsteriskAdorner;
        return null;
    }

    private static void StoreAdorner(System.Windows.UIElement element, ValidationAsteriskAdorner? adorner)
    {
        if (element is System.Windows.FrameworkElement fe)
            fe.Tag = adorner;
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
                Foreground = ResolveErrorBrush(),
                IsHitTestVisible = false
            };
            _visuals.Add(_asterisk);
        }

        protected override int VisualChildrenCount => _visuals.Count;

        protected override System.Windows.Media.Visual GetVisualChild(int index) => _visuals[index];

        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
        {
            var x = Math.Max(0, finalSize.Width - 10);
            _asterisk.Arrange(new System.Windows.Rect(new System.Windows.Point(x, -8), new System.Windows.Size(12, 16)));
            return finalSize;
        }

        private static System.Windows.Media.Brush ResolveErrorBrush()
        {
            if (System.Windows.Application.Current?.TryFindResource("ErrorBrush")
                is System.Windows.Media.Brush b) return b;
            return System.Windows.Media.Brushes.Red;
        }
    }
}
