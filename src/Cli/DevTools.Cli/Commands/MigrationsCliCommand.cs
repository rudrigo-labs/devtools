using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Cli.App;
using DevTools.Migrations.Engine;
using DevTools.Migrations.Models;

namespace DevTools.Cli.Commands;

public sealed class MigrationsCliCommand : ICliCommand
{
    private readonly CliConsole _ui;
    private readonly CliInput _input;
    private readonly MigrationsEngine _engine;

    public MigrationsCliCommand(CliConsole ui, CliInput input)
    {
        _ui = ui;
        _input = input;
        _engine = new MigrationsEngine();
    }

    public string Key => "migrations";
    public string Name => "Migrations";
    public string Description => "Assiste o dotnet ef (add migration / update database).";

    public async Task<int> ExecuteAsync(CliLaunchOptions options, CancellationToken ct)
    {
        // 1. Resolve Parameters
        var actionStr = options.GetOption("action");
        var providerStr = options.GetOption("provider");
        var root = options.GetOption("root");
        var startup = options.GetOption("startup");
        var dbContext = options.GetOption("db-context") ?? options.GetOption("context");
        var migrationsProject = options.GetOption("migrations-project") ?? options.GetOption("migrations");
        var additionalArgs = options.GetOption("args");
        var migrationName = options.GetOption("migration-name") ?? options.GetOption("name");
        var dryRunStr = options.GetOption("dry-run");
        var workingDir = options.GetOption("working-dir") ?? options.GetOption("cwd");

        MigrationsAction? action = null;
        if (actionStr != null)
        {
            if (Enum.TryParse<MigrationsAction>(actionStr, true, out var a)) action = a;
            else if (actionStr.Equals("add", StringComparison.OrdinalIgnoreCase)) action = MigrationsAction.AddMigration;
            else if (actionStr.Equals("update", StringComparison.OrdinalIgnoreCase)) action = MigrationsAction.UpdateDatabase;
        }

        DatabaseProvider? provider = null;
        if (providerStr != null)
        {
            if (Enum.TryParse<DatabaseProvider>(providerStr, true, out var p)) provider = p;
            else if (providerStr.Equals("sqlserver", StringComparison.OrdinalIgnoreCase)) provider = DatabaseProvider.SqlServer;
            else if (providerStr.Equals("sqlite", StringComparison.OrdinalIgnoreCase)) provider = DatabaseProvider.Sqlite;
        }

        bool? dryRun = dryRunStr != null ? (dryRunStr == "true") : null;

        // Interactive Fallback
        if (!options.IsNonInteractive)
        {
            if (action == null)
            {
                _ui.Section("Acao");
                _ui.WriteLine("1) Add migration");
                _ui.WriteLine("2) Update database");
                var actionChoice = _input.ReadInt("Escolha", 1, 2);
                action = actionChoice == 1 ? MigrationsAction.AddMigration : MigrationsAction.UpdateDatabase;
                options.Options["action"] = action == MigrationsAction.AddMigration ? "add" : "update";
            }

            if (provider == null)
            {
                _ui.Section("Provider");
                _ui.WriteLine("1) SqlServer");
                _ui.WriteLine("2) Sqlite");
                var providerChoice = _input.ReadInt("Escolha", 1, 2);
                provider = providerChoice == 2 ? DatabaseProvider.Sqlite : DatabaseProvider.SqlServer;
                options.Options["provider"] = provider == DatabaseProvider.Sqlite ? "sqlite" : "sqlserver";
            }

            if (string.IsNullOrWhiteSpace(root))
            {
                root = _input.ReadRequired("Root do projeto", "ex: C:\\Projetos\\MeuApp");
                options.Options["root"] = root;
            }
            
            if (string.IsNullOrWhiteSpace(startup))
            {
                startup = _input.ReadRequired("Projeto startup (.csproj)", "ex: C:\\Projetos\\MeuApp\\Api.csproj");
                options.Options["startup"] = startup;
            }
            
            if (string.IsNullOrWhiteSpace(dbContext))
            {
                dbContext = _input.ReadRequired("DbContext (namespace completo)", "ex: MeuApp.Data.AppDbContext");
                options.Options["db-context"] = dbContext;
            }
            
            if (string.IsNullOrWhiteSpace(migrationsProject))
            {
                migrationsProject = _input.ReadRequired("Projeto migrations (.csproj)", "ex: C:\\Projetos\\MeuApp\\Data.csproj");
                options.Options["migrations-project"] = migrationsProject;
            }
            
            if (string.IsNullOrWhiteSpace(additionalArgs))
            {
                additionalArgs = _input.ReadOptional("Args adicionais (opcional)");
                if (!string.IsNullOrWhiteSpace(additionalArgs)) options.Options["args"] = additionalArgs;
            }

            if (action == MigrationsAction.AddMigration && string.IsNullOrWhiteSpace(migrationName))
            {
                migrationName = _input.ReadRequired("Nome da migration");
                options.Options["migration-name"] = migrationName;
            }
            
            if (dryRun == null)
            {
                dryRun = _input.ReadYesNo("Dry-run", true);
                options.Options["dry-run"] = dryRun.Value.ToString().ToLower();
            }
            
            if (string.IsNullOrWhiteSpace(workingDir))
            {
                workingDir = _input.ReadOptional("Working directory (opcional)", "enter = usar root");
                if (!string.IsNullOrWhiteSpace(workingDir)) options.Options["working-dir"] = workingDir;
            }
        }

        // Defaults
        dryRun ??= false;

        // Validation
        if (action == null)
        {
             _ui.WriteError("Action required (--action add|update).");
             return 1;
        }
        if (provider == null)
        {
            _ui.WriteError("Provider required (--provider sqlserver|sqlite).");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(root))
        {
            _ui.WriteError("Root path required (--root).");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(startup))
        {
            _ui.WriteError("Startup project required (--startup).");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(dbContext))
        {
            _ui.WriteError("DbContext required (--db-context).");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(migrationsProject))
        {
            _ui.WriteError("Migrations project required (--migrations-project).");
            return 1;
        }
        if (action == MigrationsAction.AddMigration && string.IsNullOrWhiteSpace(migrationName))
        {
            _ui.WriteError("Migration name required for 'add' action (--migration-name).");
            return 1;
        }

        var settings = new MigrationsSettings
        {
            RootPath = root,
            StartupProjectPath = startup,
            DbContextFullName = dbContext,
            AdditionalArgs = string.IsNullOrWhiteSpace(additionalArgs) ? null : additionalArgs,
            Targets = new List<MigrationTarget>
            {
                new MigrationTarget
                {
                    Provider = provider.Value,
                    MigrationsProjectPath = migrationsProject
                }
            }
        };

        var request = new MigrationsRequest(
            action.Value,
            provider.Value,
            settings,
            migrationName,
            dryRun.Value,
            string.IsNullOrWhiteSpace(workingDir) ? null : workingDir);

        using var progress = new CliProgressReporter(_ui.Theme);
        var result = await _engine.ExecuteAsync(request, progress, ct).ConfigureAwait(false);
        progress.Finish();

        if (!result.IsSuccess || result.Value is null)
        {
            WriteErrors(result.Errors);
            return 1;
        }

        var response = result.Value;
        
        if (!options.IsNonInteractive)
        {
            _ui.Section("Resumo");
            _ui.WriteKeyValue("Comando", response.Command);
            if (response.WasDryRun)
            {
                _ui.WriteWarning("Dry-run: comando nao executado.");
                return 0;
            }

            if (!string.IsNullOrWhiteSpace(response.StdOut))
            {
                _ui.Section("StdOut");
                _ui.WriteLine(response.StdOut.Trim());
            }

            if (!string.IsNullOrWhiteSpace(response.StdErr))
            {
                _ui.Section("StdErr");
                _ui.WriteWarning(response.StdErr.Trim());
            }
        }

        return response.ExitCode == 0 ? 0 : 1;
    }

    private void WriteErrors(IReadOnlyList<DevTools.Core.Results.ErrorDetail> errors)
    {
        CliErrorLogger.LogErrors(Key, errors);
        _ui.Section("Erros");
        foreach (var error in errors)
        {
            _ui.WriteError($"{error.Code}: {error.Message}");
            if (!string.IsNullOrWhiteSpace(error.Details))
                _ui.WriteDim(error.Details);
        }
    }
}
