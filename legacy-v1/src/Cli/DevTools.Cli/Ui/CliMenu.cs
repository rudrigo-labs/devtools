using DevTools.Cli.Commands;

namespace DevTools.Cli.Ui;

public sealed class CliMenu
{
    private readonly CliConsole _ui;
    private readonly CliInput _input;

    public CliMenu(CliConsole ui, CliInput input)
    {
        _ui = ui;
        _input = input;
    }

    public ICliCommand? Show(IReadOnlyList<ICliCommand> commands)
    {
        while (true)
        {
            _ui.Header("Menu Principal", "Selecione a ferramenta que deseja executar.");
            RenderMenu();

            var raw = _input.ReadOptional("Escolha uma opcao", "ex: 01 ou harvest");
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            raw = raw.Trim();
            if (raw.Equals("sair", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("q", StringComparison.OrdinalIgnoreCase))
                return null;

            var byKey = commands.FirstOrDefault(c => c.Key.Equals(raw, StringComparison.OrdinalIgnoreCase));
            if (byKey is not null)
                return byKey;

            if (int.TryParse(raw, out var index))
            {
                if (index >= 1 && index <= commands.Count)
                    return commands[index - 1];
            }

            _ui.WriteWarning("Opcao invalida. Tente novamente.");
        }
    }

    public int ShowOptions(string title, IReadOnlyList<string> options)
    {
        while (true)
        {
            _ui.Section(title);
            for (int i = 0; i < options.Count; i++)
            {
                _ui.WriteLine($"{i + 1}) {options[i]}");
            }

            var raw = _input.ReadOptional("Escolha uma opcao", $"1-{options.Count} ou cancelar");
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            if (IsCancel(raw))
                return -1;

            if (int.TryParse(raw, out var index))
            {
                if (index >= 1 && index <= options.Count)
                    return index - 1;
            }

            _ui.WriteWarning("Opcao invalida.");
        }
    }

    private static bool IsCancel(string value)
    {
        return value.Equals("c", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("cancelar", StringComparison.OrdinalIgnoreCase);
    }

    private void RenderMenu()
    {
        _ui.Section("Selecao");
        _ui.WriteDim("Digite o numero ou comando. Para sair, digite 'sair'.");
    }
}
