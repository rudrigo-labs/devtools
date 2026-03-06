using DevTools.Cli.Commands;
using DevTools.Cli.Logging;
using DevTools.Cli.Ui;
using DevTools.Core.Configuration;
using DevTools.Core.Models;

namespace DevTools.Cli.App;

public sealed class CliApp
{
    private readonly CliConsole _ui;
    private readonly CliMenu _menu;
    private readonly CliInput _input;
    private readonly IReadOnlyList<ICliCommand> _commands;
    private readonly ToolConfigurationManager _toolConfigurationManager;

    public CliApp(CliConsole ui, CliMenu menu, CliInput input, IReadOnlyList<ICliCommand> commands, ToolConfigurationManager toolConfigurationManager)
    {
        _ui = ui;
        _menu = menu;
        _input = input;
        _commands = commands;
        _toolConfigurationManager = toolConfigurationManager;
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
                
                // --- Configuration Logic Start ---
                
                // 1. Load Configuration from Args (only on first run)
                if (isFirstRun && runOptions.GetOption("configuration") is string configurationArg)
                {
                    var configuration = _toolConfigurationManager.GetConfiguration(command.Key, configurationArg);
                    if (configuration != null)
                    {
                        _ui.WriteSuccess($"Configuracao '{configuration.Name}' carregado.");
                        foreach (var kvp in configuration.Options)
                        {
                            if (!runOptions.Options.ContainsKey(kvp.Key))
                            {
                                runOptions.Options[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    else
                    {
                        _ui.WriteWarning($"Configuracao '{configurationArg}' nao encontrado para a ferramenta '{command.Key}'.");
                        if (options.IsNonInteractive) return 1;
                    }
                }
                else if (!options.IsNonInteractive)
                {
                    // 2. Interactive Configuration Selection
                    var configurations = _toolConfigurationManager.LoadConfigurations(command.Key);
                    if (configurations.Count > 0)
                    {
                        var useConfiguration = _input.ReadYesNo($"Existem {configurations.Count} configuracoes salvos. Carregar?", false);
                        if (useConfiguration)
                        {
                             var configurationNames = configurations.Select(p => p.Name).ToList();
                             var selectedIndex = _menu.ShowOptions("Selecione o configuracao", configurationNames);
                             if (selectedIndex >= 0)
                             {
                                 var configuration = configurations[selectedIndex];
                                 _ui.WriteSuccess($"Configuracao '{configuration.Name}' carregado.");
                                 foreach (var kvp in configuration.Options)
                                 {
                                     if (!runOptions.Options.ContainsKey(kvp.Key))
                                     {
                                         runOptions.Options[kvp.Key] = kvp.Value;
                                     }
                                 }
                             }
                        }
                    }
                }
                
                // --- Configuration Logic End ---
                
                lastResult = await command.ExecuteAsync(runOptions, ct).ConfigureAwait(false);
                
                // --- Save Configuration Logic ---
                if (lastResult == 0) // Only save on success
                {
                    var optionsToSave = SanitizeOptions(runOptions.Options);

                    if (isFirstRun && runOptions.GetOption("save-configuration") is string saveName)
                    {
                        // Save requested via args
                        var configuration = new ToolConfiguration { Name = saveName, Options = optionsToSave, UpdatedUtc = DateTime.UtcNow };
                        _toolConfigurationManager.SaveConfiguration(command.Key, configuration);
                        _ui.WriteSuccess($"Configuracao '{saveName}' salvo com sucesso.");
                    }
                    else if (!options.IsNonInteractive)
                    {
                        // Interactive save prompt
                        var save = _input.ReadYesNo("Salvar configuracao atual como configuracao?", false);
                        if (save)
                        {
                            var name = _input.ReadRequired("Nome do configuracao");
                            var configuration = new ToolConfiguration { Name = name, Options = optionsToSave, UpdatedUtc = DateTime.UtcNow };
                            _toolConfigurationManager.SaveConfiguration(command.Key, configuration);
                            _ui.WriteSuccess($"Configuracao '{name}' salvo com sucesso.");
                        }
                    }
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
                return 0;
            }

            if (next == NextAction.Exit)
                return 0;
            
            if (next == NextAction.MainMenu)
            {
                pending = null; // Loop will show menu
            }
            else if (next == NextAction.Repeat)
            {
                pending = command; // Loop will execute same command
            }
        }
    }

    private Dictionary<string, string> SanitizeOptions(Dictionary<string, string> options)
    {
        var clean = new Dictionary<string, string>(options);
        clean.Remove("configuration");
        clean.Remove("save-configuration");
        clean.Remove("non-interactive");
        return clean;
    }

    private enum NextAction
    {
        Exit,
        MainMenu,
        Repeat
    }

    private NextAction PromptNextAction(ICliCommand command)
    {
        _ui.WriteLine("");
        _ui.Section("O que fazer agora?");
        _ui.WriteLine("1) Repetir comando");
        _ui.WriteLine("2) Menu principal");
        _ui.WriteLine("3) Sair");
        
        var choice = _input.ReadInt("Escolha", 1, 3);
        return choice switch
        {
            1 => NextAction.Repeat,
            2 => NextAction.MainMenu,
            _ => NextAction.Exit
        };
    }
}


