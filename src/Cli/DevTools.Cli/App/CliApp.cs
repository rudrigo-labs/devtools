using DevTools.Cli.Commands;
using DevTools.Cli.Logging;
using DevTools.Cli.Ui;

namespace DevTools.Cli.App;

public sealed class CliApp
{
    private readonly CliConsole _ui;
    private readonly CliMenu _menu;
    private readonly CliInput _input;
    private readonly IReadOnlyList<ICliCommand> _commands;

    public CliApp(CliConsole ui, CliMenu menu, CliInput input, IReadOnlyList<ICliCommand> commands)
    {
        _ui = ui;
        _menu = menu;
        _input = input;
        _commands = commands;
    }

    public async Task<int> RunAsync(CancellationToken ct)
    {
        ICliCommand? pending = null;

        while (true)
        {
            ICliCommand? command = pending;
            try
            {
                command ??= _menu.Show(_commands);
            }
            catch (CliAbortException)
            {
                pending = null;
                continue;
            }

            if (command is null)
                return 0;

            _ui.Header(command.Name, command.Description);

            pending = null;
            var lastResult = 0;
            try
            {
                lastResult = await command.ExecuteAsync(ct).ConfigureAwait(false);
                _ui.WriteLine();
                _ui.Section("Resumo final");
                if (lastResult == 0)
                    _ui.WriteSuccess("Concluido.");
                else
                    _ui.WriteWarning("Concluido com avisos/erros.");
            }
            catch (CliAbortException)
            {
                _ui.WriteWarning("Operacao cancelada.");
            }
            catch (Exception ex)
            {
                _ui.WriteError($"Erro inesperado: {ex.Message}");
                CliErrorLogger.LogException(command?.Key ?? "cli", ex);
            }

            NextAction next;
            try
            {
                next = PromptNextAction(command!);
            }
            catch (CliAbortException)
            {
                continue;
            }
            if (next == NextAction.Exit)
                return 0;
            if (next == NextAction.Repeat)
                pending = command;
        }
    }

    private NextAction PromptNextAction(ICliCommand command)
    {
        _ui.Section("Proximo passo");
        _ui.WriteLine("1) Voltar ao menu");
        _ui.WriteLine("2) Repetir esta ferramenta");
        _ui.WriteLine("0) Sair");

        var choice = _input.ReadInt("Escolha", 0, 2);
        return choice switch
        {
            2 => NextAction.Repeat,
            0 => NextAction.Exit,
            _ => NextAction.Menu
        };
    }

    private enum NextAction
    {
        Menu = 0,
        Repeat = 1,
        Exit = 2
    }
}
