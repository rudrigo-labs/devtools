using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
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

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        _ui.Section("Acao");
        _ui.WriteLine("1) Add migration");
        _ui.WriteLine("2) Update database");
        var actionChoice = _input.ReadInt("Escolha", 1, 2);
        var action = actionChoice == 1 ? MigrationsAction.AddMigration : MigrationsAction.UpdateDatabase;

        _ui.Section("Provider");
        _ui.WriteLine("1) SqlServer");
        _ui.WriteLine("2) Sqlite");
        var providerChoice = _input.ReadInt("Escolha", 1, 2);
        var provider = providerChoice == 2 ? DatabaseProvider.Sqlite : DatabaseProvider.SqlServer;

        var root = _input.ReadRequired("Root do projeto", "ex: C:\\Projetos\\MeuApp");
        var startup = _input.ReadRequired("Projeto startup (.csproj)", "ex: C:\\Projetos\\MeuApp\\Api.csproj");
        var dbContext = _input.ReadRequired("DbContext (namespace completo)", "ex: MeuApp.Data.AppDbContext");
        var migrationsProject = _input.ReadRequired("Projeto migrations (.csproj)", "ex: C:\\Projetos\\MeuApp\\Data.csproj");
        var additionalArgs = _input.ReadOptional("Args adicionais (opcional)");

        string? migrationName = null;
        if (action == MigrationsAction.AddMigration)
            migrationName = _input.ReadRequired("Nome da migration");

        var dryRun = _input.ReadYesNo("Dry-run", true);
        var workingDir = _input.ReadOptional("Working directory (opcional)", "enter = usar root");

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
                    Provider = provider,
                    MigrationsProjectPath = migrationsProject
                }
            }
        };

        var request = new MigrationsRequest(
            action,
            provider,
            settings,
            migrationName,
            dryRun,
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
