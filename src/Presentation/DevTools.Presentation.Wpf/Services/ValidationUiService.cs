using System;
using DevTools.Presentation.Wpf.Components;

namespace DevTools.Presentation.Wpf.Services;

public static class ValidationUiService
{
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
            return;
        }

        control.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
        control.ClearValue(System.Windows.Controls.Control.BorderThicknessProperty);
    }

    public static void SetPathSelectorInvalid(PathSelector? selector, bool invalid)
    {
        if (selector == null)
            return;

        var textBox = selector.FindName("PathInput") as System.Windows.Controls.TextBox;
        SetControlInvalid(textBox, invalid);
    }
}
