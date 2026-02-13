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
    private readonly ProfileManager _profileManager;

    public CliApp(CliConsole ui, CliMenu menu, CliInput input, IReadOnlyList<ICliCommand> commands, ProfileManager profileManager)
    {
        _ui = ui;
        _menu = menu;
        _input = input;
        _commands = commands;
        _profileManager = profileManager;
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
                
                // --- Profile Logic Start ---
                
                // 1. Load Profile from Args (only on first run)
                if (isFirstRun && runOptions.GetOption("profile") is string profileArg)
                {
                    var profile = _profileManager.GetProfile(command.Key, profileArg);
                    if (profile != null)
                    {
                        _ui.WriteSuccess($"Perfil '{profile.Name}' carregado.");
                        foreach (var kvp in profile.Options)
                        {
                            if (!runOptions.Options.ContainsKey(kvp.Key))
                            {
                                runOptions.Options[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    else
                    {
                        _ui.WriteWarning($"Perfil '{profileArg}' nao encontrado para a ferramenta '{command.Key}'.");
                        if (options.IsNonInteractive) return 1;
                    }
                }
                else if (!options.IsNonInteractive)
                {
                    // 2. Interactive Profile Selection
                    var profiles = _profileManager.LoadProfiles(command.Key);
                    if (profiles.Count > 0)
                    {
                        var useProfile = _input.ReadYesNo($"Existem {profiles.Count} perfis salvos. Carregar?", false);
                        if (useProfile)
                        {
                             var profileNames = profiles.Select(p => p.Name).ToList();
                             var selectedIndex = _menu.ShowOptions("Selecione o perfil", profileNames);
                             if (selectedIndex >= 0)
                             {
                                 var profile = profiles[selectedIndex];
                                 _ui.WriteSuccess($"Perfil '{profile.Name}' carregado.");
                                 foreach (var kvp in profile.Options)
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
                
                // --- Profile Logic End ---
                
                lastResult = await command.ExecuteAsync(runOptions, ct).ConfigureAwait(false);
                
                // --- Save Profile Logic ---
                if (lastResult == 0) // Only save on success
                {
                    var optionsToSave = SanitizeOptions(runOptions.Options);

                    if (isFirstRun && runOptions.GetOption("save-profile") is string saveName)
                    {
                        // Save requested via args
                        var profile = new ToolProfile { Name = saveName, Options = optionsToSave, UpdatedUtc = DateTime.UtcNow };
                        _profileManager.SaveProfile(command.Key, profile);
                        _ui.WriteSuccess($"Perfil '{saveName}' salvo com sucesso.");
                    }
                    else if (!options.IsNonInteractive)
                    {
                        // Interactive save prompt
                        var save = _input.ReadYesNo("Salvar configuracao atual como perfil?", false);
                        if (save)
                        {
                            var name = _input.ReadRequired("Nome do perfil");
                            var profile = new ToolProfile { Name = name, Options = optionsToSave, UpdatedUtc = DateTime.UtcNow };
                            _profileManager.SaveProfile(command.Key, profile);
                            _ui.WriteSuccess($"Perfil '{name}' salvo com sucesso.");
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
        clean.Remove("profile");
        clean.Remove("save-profile");
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
