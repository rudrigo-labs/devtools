using System.Text;
using DevTools.Core.Results;

namespace DevTools.Cli.Ui;

public sealed class CliConsole
{
    private readonly CliTheme _theme;
    private readonly CliState _state;

    public CliConsole(CliTheme theme, CliState state)
    {
        _theme = theme;
        _state = state;
    }

    public CliTheme Theme => _theme;
    public CliState State => _state;

    public void Clear()
    {
        try
        {
            if (!Console.IsOutputRedirected)
                Console.Clear();
        }
        catch
        {
            // ignore
        }
    }

    public void PrintRunResult(RunResult result)
    {
        WriteLine();
        
        var s = result.Summary;
        // Check if summary is empty (default struct/record check might be needed)
        // RunSummary.Empty uses default values.
        if (s != null && !string.IsNullOrEmpty(s.ToolName))
        {
            var width = 60;
            var line = new string('-', width);
            
            WriteDim(line);
            WriteAccent($"RESUMO DA EXECUÇÃO: {s.ToolName}");
            WriteDim(line);
            
            WriteKeyValue("Modo", s.Mode);
            WriteKeyValue("Input", s.MainInput);
            if (!string.IsNullOrWhiteSpace(s.OutputLocation))
                WriteKeyValue("Output", s.OutputLocation);
                
            WriteKeyValue("Processados", s.Processed.ToString());
            WriteKeyValue("Alterados", s.Changed.ToString());
            WriteKeyValue("Ignorados", s.Ignored.ToString());
            WriteKeyValue("Falhas", s.Failed.ToString());
            WriteKeyValue("Duração", s.Duration.ToString(@"hh\:mm\:ss\.fff"));
            
            WriteDim(line);
            WriteLine();
        }
        
        if (result.Errors.Count > 0)
        {
            WriteError($"Encontrados {result.Errors.Count} erros:");
            foreach (var err in result.Errors)
            {
                WriteLine();
                WriteError($"[x] {err.Message}");
                if (!string.IsNullOrWhiteSpace(err.Cause))
                    WriteDim($"    Causa: {err.Cause}");
                if (!string.IsNullOrWhiteSpace(err.Action))
                    WriteInfo($"    Sugestão: {err.Action}");
                if (!string.IsNullOrWhiteSpace(err.Details))
                     WriteDim($"    Detalhe Técnico: {err.Details}");
            }
            WriteLine();
        }
        
        if (result.IsSuccess)
        {
             WriteSuccess("✅ Concluído");
        }
        else
        {
             WriteError("❌ Falhou");
        }
        WriteLine();
    }

    public void Header(string title, string? subtitle = null)
    {
        Clear();

        RenderTopMenu();
        WriteLine();

        var width = GetFrameWidth(60, 90);
        var line = "+" + new string('-', width - 2) + "+";
        WriteColored(line, _theme.AccentColor);
        WriteColored($"|{Center("DEVTOOLS CLI", width - 2)}|", _theme.TitleColor);
        WriteColored($"|{Center(title, width - 2)}|", _theme.TitleColor);
        WriteColored(line, _theme.AccentColor);
        if (!string.IsNullOrWhiteSpace(subtitle))
            WriteDim(subtitle);
        WriteDim("Dica: digite 'c' para cancelar.");
        WriteLine();
    }

    public void Section(string title)
    {
        WriteLine();
        WriteColored($"-- {title} --", _theme.AccentColor);
    }

    public void WriteAccent(string message) => WriteColored(message, _theme.AccentColor);
    public void WriteInfo(string message) => WriteColored(message, _theme.InfoColor);
    public void WriteSuccess(string message) => WriteColored(message, _theme.SuccessColor);
    public void WriteWarning(string message) => WriteColored(message, _theme.WarningColor);
    public void WriteError(string message) => WriteColored(message, _theme.ErrorColor);
    public void WriteDim(string message) => WriteColored(message, _theme.DimColor);

    public void Write(string message) => Console.Write(message);
    public void WriteLine(string? message = null) => Console.WriteLine(message ?? string.Empty);

    public void WritePromptLabel(string label, string? hint = null)
    {
        WriteColored(label, _theme.InputLabelColor);
        if (!string.IsNullOrWhiteSpace(hint))
        {
            Write(" ");
            WriteDim($"- {hint}");
        }
        WriteLine();
    }

    public void WriteInputBoxPrefix()
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = _theme.InputBoxColor;
        try
        {
            Console.Write("  [>] ");
        }
        finally
        {
            Console.ForegroundColor = prev;
        }
    }

    public void WriteKeyValue(string label, string value)
    {
        WriteColored(label.PadRight(20), _theme.DimColor);
        WriteLine(value);
    }

    private void RenderTopMenu()
    {
        if (_state.MenuItems.Count == 0)
            return;

        var width = GetFrameWidth(60, 90);
        var border = new string('=', width);
        WriteColored(border, _theme.AccentColor);

        foreach (var line in BuildMenuLines(width))
            WriteColored(line.PadRight(width), _theme.AccentColor);

        WriteColored(border, _theme.AccentColor);
    }

    private IEnumerable<string> BuildMenuLines(int width)
    {
        var sep = " | ";
        var current = string.Empty;

        foreach (var item in _state.MenuItems)
        {
            var candidate = string.IsNullOrWhiteSpace(current) ? item : current + sep + item;
            if (candidate.Length > width)
            {
                if (!string.IsNullOrWhiteSpace(current))
                    yield return current;
                current = item;
            }
            else
            {
                current = candidate;
            }
        }

        if (!string.IsNullOrWhiteSpace(current))
            yield return current;
    }

    public int GetFrameWidth(int minWidth, int maxWidth)
    {
        var width = 70;
        try
        {
            if (!Console.IsOutputRedirected)
                width = Console.WindowWidth - 2;
        }
        catch
        {
            // ignore
        }

        if (width < minWidth) width = minWidth;
        if (width > maxWidth) width = maxWidth;
        return width;
    }

    public void Pause(string message = "Pressione ENTER para voltar...")
    {
        WriteLine();
        WriteDim(message);
        Console.ReadLine();
    }

    public static void ConfigureEncoding()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private void WriteColored(string message, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        try
        {
            Console.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = prev;
        }
    }

    private static string Center(string value, int width)
    {
        value ??= string.Empty;
        if (value.Length >= width)
            return value;

        var left = (width - value.Length) / 2;
        return new string(' ', left) + value;
    }
}
