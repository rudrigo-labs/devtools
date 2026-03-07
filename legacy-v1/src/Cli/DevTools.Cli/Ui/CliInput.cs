namespace DevTools.Cli.Ui;

public sealed class CliInput
{
    private readonly CliConsole _ui;

    public CliInput(CliConsole ui)
    {
        _ui = ui;
    }

    public string ReadRequired(string label, string? hint = null)
    {
        while (true)
        {
            _ui.WritePromptLabel(label, hint);
            _ui.WriteInputBoxPrefix();
            var value = ReadLine();

            if (IsCancel(value))
                throw new CliAbortException();

            value = (value ?? string.Empty).Trim().Trim('"');
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            _ui.WriteWarning("Campo obrigatorio. Tente novamente.");
        }
    }

    public string ReadOptional(string label, string? hint = null)
    {
        _ui.WritePromptLabel(label, hint);
        _ui.WriteInputBoxPrefix();
        var value = ReadLine();

        if (IsCancel(value))
            throw new CliAbortException();

        return (value ?? string.Empty).Trim().Trim('"');
    }

    public int? ReadOptionalInt(string label, string? hint = null)
    {
        while (true)
        {
            var raw = ReadOptional(label, hint);
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            if (int.TryParse(raw, out var value))
                return value;

            _ui.WriteWarning("Numero invalido. Tente novamente.");
        }
    }

    public int ReadInt(string label, int min, int max, string? hint = null)
    {
        while (true)
        {
            var raw = ReadRequired(label, hint);
            if (int.TryParse(raw, out var value) && value >= min && value <= max)
                return value;

            _ui.WriteWarning($"Digite um numero entre {min} e {max}.");
        }
    }

    public bool ReadYesNo(string label, bool defaultValue = false, string? hint = null)
    {
        var suffix = defaultValue ? "S/n" : "s/N";
        while (true)
        {
            var value = ReadOptional(label, CombineHint(hint, suffix));
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            value = value.Trim().ToLowerInvariant();
            if (value is "s" or "sim" or "y" or "yes")
                return true;
            if (value is "n" or "nao" or "no")
                return false;

            _ui.WriteWarning("Resposta invalida. Use s/n.");
        }
    }

    public IReadOnlyList<string> ReadCsv(string label, string? hint = null)
    {
        var raw = ReadOptional(label, hint);
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        return raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    public string ReadMultiline(string label, string? hint = null, string terminator = ".")
    {
        _ui.WritePromptLabel(label, CombineHint(hint, $"finalize com '{terminator}'"));
        var lines = new List<string>();
        while (true)
        {
            _ui.WriteInputBoxPrefix();
            var line = ReadLine();
            if (line is null)
                break;

            if (IsCancel(line))
                throw new CliAbortException();

            if (line.Trim() == terminator)
                break;

            lines.Add(line);
        }

        return string.Join(Environment.NewLine, lines);
    }

    public string? ReadLine() => Console.ReadLine();

    private static bool IsCancel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim().ToLowerInvariant();
        return value is "c" or "cancelar" or "cancel" or "q" or "sair" or "exit";
    }

    private static string? CombineHint(string? hint, string suffix)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return suffix;

        return $"{hint} | {suffix}";
    }
}
