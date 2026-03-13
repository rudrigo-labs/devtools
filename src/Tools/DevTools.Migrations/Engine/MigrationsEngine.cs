using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Migrations.Models;
using DevTools.Migrations.Validators;

namespace DevTools.Migrations.Engine;

public sealed class MigrationsEngine : IDevToolEngine<MigrationsRequest, MigrationsResult>
{
    private readonly IProcessRunner _processRunner;
    private readonly IValidator<MigrationsRequest> _validator;

    public MigrationsEngine(IProcessRunner processRunner, IValidator<MigrationsRequest>? validator = null)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _validator = validator ?? new MigrationsRequestValidator();
    }

    public async Task<RunResult<MigrationsResult>> ExecuteAsync(
        MigrationsRequest request,
        IProgressReporter? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(e => new ErrorDetail($"migrations.{e.Field}", e.Message))
                .ToList();
            return RunResult<MigrationsResult>.Fail(errors);
        }

        var settings = request.Settings;
        var args = request.Action switch
        {
            MigrationsAction.AddMigration  => EfCommandBuilder.BuildAddMigration(settings, request.Provider, request.MigrationName!),
            MigrationsAction.UpdateDatabase => EfCommandBuilder.BuildUpdateDatabase(settings, request.Provider),
            _ => throw new InvalidOperationException("Action inválida.")
        };

        var workingDirectory = !string.IsNullOrWhiteSpace(request.WorkingDirectory)
            ? request.WorkingDirectory!
            : settings.RootPath;

        var command = $"dotnet {args}";

        if (request.DryRun)
        {
            var dryResult = new MigrationsResult(request.Action, request.Provider, command, null, null, null, true);
            return RunResult<MigrationsResult>.Success(dryResult);
        }

        progress?.Report(new ProgressEvent("Executando dotnet ef", 10, "run"));

        try
        {
            var result = await _processRunner
                .RunAsync("dotnet", args, workingDirectory, null, cancellationToken)
                .ConfigureAwait(false);

            var response = new MigrationsResult(
                request.Action,
                request.Provider,
                command,
                result.ExitCode,
                result.StdOut,
                result.StdErr,
                false);

            if (result.ExitCode != 0)
            {
                var error = BuildProcessError(result);
                return new RunResult<MigrationsResult>
                {
                    IsSuccess = false,
                    Errors = [error],
                    Value = response
                };
            }

            progress?.Report(new ProgressEvent("Concluído", 100, "done"));
            return RunResult<MigrationsResult>.Success(response);
        }
        catch (OperationCanceledException ex)
        {
            return RunResult<MigrationsResult>.FromException("migrations.cancelled", "Operação cancelada.", ex);
        }
        catch (Exception ex)
        {
            return RunResult<MigrationsResult>.FromException("migrations.process.failed", "Falha ao executar dotnet ef.", ex, ex.Message);
        }
    }

    private static ErrorDetail BuildProcessError(ProcessResult result)
    {
        var stderr = result.StdErr?.Trim();
        var stdout = result.StdOut?.Trim();

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            if (stderr.Contains("dotnet-ef", StringComparison.OrdinalIgnoreCase) &&
                (stderr.Contains("No executable found", StringComparison.OrdinalIgnoreCase) ||
                 stderr.Contains("was not found", StringComparison.OrdinalIgnoreCase)))
            {
                return new ErrorDetail("migrations.dotnet_ef.missing", "dotnet-ef não está instalado.", stderr);
            }
        }

        var details = string.Join("\n\n", new[] { stdout, stderr }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return new ErrorDetail("migrations.dotnet_ef.failed", "Comando dotnet ef falhou.", details);
    }
}
