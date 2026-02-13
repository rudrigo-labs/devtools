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

    public async Task<int> RunAsync(CliLaunchOptions options, CancellationToken ct)
    {
        ICliCommand? pending = null;
        bool isFirstRun = true;

        // Try to match initial command from args
        if (options.CommandName != null)
        {
            pending = _commands.FirstOrDefault(c => c.Key.Equals(options.CommandName, StringComparison.OrdinalIgnoreCase));
            if (pending == null)
            {
                _ui.WriteError($"Comando '{options.CommandName}' nao encontrado.");
                if (options.IsNonInteractive) return 1;
            }
        }

        while (true)
        {
            ICliCommand? command = pending;
            try
            {
                if (command == null)
                {
                    // If non-interactive and no command, exit
                    if (options.IsNonInteractive && isFirstRun)
                    {
                        _ui.WriteError("Nenhum comando especificado em modo non-interactive.");
                        return 1;
                    }
                    command = _menu.Show(_commands);
                }
            }
            catch (CliAbortException)
            {
                if (options.IsNonInteractive) return 1;
                pending = null;
                continue;
            }

            if (command is null)
                return 0;

            if (!options.IsNonInteractive)
                _ui.Header(command.Name, command.Description);

            pending = null;
            var lastResult = 0;
            try
            {
                // Pass options only on first run, otherwise empty options for repeated runs
                var runOptions = isFirstRun ? options : new CliLaunchOptions();
                lastResult = await command.ExecuteAsync(runOptions, ct).ConfigureAwait(false);
                
                if (!options.IsNonInteractive)
                {
                    _ui.WriteLine();
                    _ui.Section("Resumo final");
                    if (lastResult == 0)
                        _ui.WriteSuccess("Concluido.");
                    else
                        _ui.WriteWarning("Concluido com avisos/erros.");
                }
            }
            catch (CliAbortException)
            {
                _ui.WriteWarning("Operacao cancelada.");
                if (options.IsNonInteractive) return 1;
            }
            catch (Exception ex)
            {
                _ui.WriteError($"Erro inesperado: {ex.Message}");
                CliErrorLogger.LogException(command?.Key ?? "cli", ex);
                if (options.IsNonInteractive) return 1;
            }
            
            isFirstRun = false;

            if (options.IsNonInteractive)
                return lastResult;

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
