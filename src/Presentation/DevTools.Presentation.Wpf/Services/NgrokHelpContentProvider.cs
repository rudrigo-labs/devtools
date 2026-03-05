using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using DevTools.Ngrok.Services;
using System.Windows.Navigation;

namespace DevTools.Presentation.Wpf.Services;

public static class NgrokHelpContentProvider
{
    public static FlowDocument GetHelp()
    {
        var onboardingService = new NgrokOnboardingService();
        var accentBrush = TryGetBrush("AccentBrush", System.Windows.Media.Brushes.DeepSkyBlue);
        var primaryTextBrush = TryGetBrush("PrimaryTextBrush", System.Windows.Media.Brushes.WhiteSmoke);
        var secondaryTextBrush = TryGetBrush(
            "SecondaryTextBrush",
            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220)));

        var document = new FlowDocument
        {
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = secondaryTextBrush,
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 14.0,
            LineHeight = 22.0,
            PagePadding = new System.Windows.Thickness(0),
            ColumnWidth = 2200
        };

        var titleStyle = new Style(typeof(Paragraph));
        titleStyle.Setters.Add(new Setter(Paragraph.FontSizeProperty, 22.0));
        titleStyle.Setters.Add(new Setter(Paragraph.FontWeightProperty, FontWeights.Bold));
        titleStyle.Setters.Add(new Setter(Paragraph.ForegroundProperty, primaryTextBrush));
        titleStyle.Setters.Add(new Setter(Paragraph.MarginProperty, new System.Windows.Thickness(0, 0, 0, 18)));

        var h2Style = new Style(typeof(Paragraph));
        h2Style.Setters.Add(new Setter(Paragraph.FontSizeProperty, 18.0));
        h2Style.Setters.Add(new Setter(Paragraph.FontWeightProperty, FontWeights.SemiBold));
        h2Style.Setters.Add(new Setter(Paragraph.ForegroundProperty, accentBrush));
        h2Style.Setters.Add(new Setter(Paragraph.MarginProperty, new System.Windows.Thickness(0, 14, 0, 10)));

        var bodyStyle = new Style(typeof(Paragraph));
        bodyStyle.Setters.Add(new Setter(Paragraph.FontSizeProperty, 14.0));
        bodyStyle.Setters.Add(new Setter(Paragraph.LineHeightProperty, 21.0));
        bodyStyle.Setters.Add(new Setter(Paragraph.ForegroundProperty, secondaryTextBrush));
        bodyStyle.Setters.Add(new Setter(Paragraph.MarginProperty, new System.Windows.Thickness(0, 0, 0, 8)));

        document.Blocks.Add(new Paragraph(new Run("Como configurar o Ngrok")) { Style = titleStyle });
        document.Blocks.Add(new Paragraph(new Run("Siga os passos abaixo para concluir o onboarding no DevTools.")) { Style = bodyStyle });

        document.Blocks.Add(new Paragraph(new Run("Passo a passo")) { Style = h2Style });
        var steps = onboardingService.GetSetupSteps();
        for (var i = 0; i < steps.Count; i++)
        {
            document.Blocks.Add(new Paragraph(new Run($"{i + 1}. {steps[i]}")) { Style = bodyStyle });
        }
        document.Blocks.Add(new Paragraph(new Run("5. Informe a porta local e clique em Iniciar Tunel.")) { Style = bodyStyle });

        document.Blocks.Add(new Paragraph(new Run("Links uteis")) { Style = h2Style });
        document.Blocks.Add(CreateParagraphWithLink("Criar conta: ", onboardingService.GetSignupUrl(), bodyStyle, accentBrush));
        document.Blocks.Add(CreateParagraphWithLink("Pagina do token: ", onboardingService.GetTokenPageUrl(), bodyStyle, accentBrush));

        return document;
    }

    private static Paragraph CreateParagraphWithLink(string prefix, string url, System.Windows.Style paragraphStyle, System.Windows.Media.Brush linkBrush)
    {
        var paragraph = new Paragraph { Style = paragraphStyle };
        paragraph.Inlines.Add(new Run(prefix));
        paragraph.Inlines.Add(CreateHyperlink(url, linkBrush));
        return paragraph;
    }

    private static Hyperlink CreateHyperlink(string url, System.Windows.Media.Brush linkBrush)
    {
        var hyperlink = new Hyperlink(new Run(url))
        {
            NavigateUri = new Uri(url),
            Foreground = linkBrush,
            TextDecorations = TextDecorations.Underline
        };

        hyperlink.RequestNavigate += (_, e) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch
            {
            }

            e.Handled = true;
        };

        return hyperlink;
    }

    private static System.Windows.Media.Brush TryGetBrush(string key, System.Windows.Media.Brush fallback)
    {
        return System.Windows.Application.Current?.TryFindResource(key) as System.Windows.Media.Brush ?? fallback;
    }
}
