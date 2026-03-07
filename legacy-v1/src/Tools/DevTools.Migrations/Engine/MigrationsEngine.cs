using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Migrations.Models;
using DevTools.Migrations.Providers;
using DevTools.Core.Providers;
using DevTools.Migrations.Validation;

namespace DevTools.Migrations.Engine;

public sealed class MigrationsEngine : IDevToolEngine<MigrationsRequest, MigrationsResponse>
{
    private readonly IFileSystem _fs;
    private readonly IProcessRunner _processRunner;

    public MigrationsEngine(IFileSystem? fileSystem = null, IProcessRunner? processRunner = null)
    {
        _fs = fileSystem ?? new SystemFileSystem();
        _processRunner = processRunner ?? new SystemProcessRunner();
    }

    public async Task<RunResult<MigrationsResponse>> ExecuteAsync(
        MigrationsRequest request,
        IProgressReporter? progress = null,
        CancellationToken ct = default)
    {
        var errors = MigrationsRequestValidator.Validate(request, _fs);
        if (errors.Count > 0)
            return RunResult<MigrationsResponse>.Fail(errors);

        var settings = request.Settings;
        var args = request.Action switch
        {
            MigrationsAction.AddMigration => EfCommandBuilder.BuildAddMigration(settings, request.Provider, request.MigrationName!),
            MigrationsAction.UpdateDatabase => EfCommandBuilder.BuildUpdateDatabase(settings, request.Provider),
            _ => throw new InvalidOperationException("Invalid migration action.")
        };

        var workingDirectory = !string.IsNullOrWhiteSpace(request.WorkingDirectory)
            ? request.WorkingDirectory!
            : settings.RootPath;

        var command = $"dotnet {args}";

        if (request.DryRun)
        {
            var dryResponse = new MigrationsResponse(request.Action, request.Provider, command, null, null, null, true);
            return RunResult<MigrationsResponse>.Success(dryResponse);
        }

        progress?.Report(new ProgressEvent("Running dotnet ef", 10, "run"));

        try
        {
            var result = await _processRunner.RunAsync("dotnet", args, workingDirectory, null, ct).ConfigureAwait(false);

            var response = new MigrationsResponse(
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
                return new RunResult<MigrationsResponse>
                {
                    IsSuccess = false,
                    Errors = new[] { error },
                    Value = response
                };
            }

            progress?.Report(new ProgressEvent("Done", 100, "done"));
            return RunResult<MigrationsResponse>.Success(response);
        }
        catch (OperationCanceledException ex)
        {
            return RunResult<MigrationsResponse>.FromException(
                "migrations.cancelled",
                "Operation cancelled.",
                ex);
        }
        catch (Exception ex)
        {
            return RunResult<MigrationsResponse>.FromException(
                "migrations.process.failed",
                "Failed to execute dotnet ef.",
                ex,
                ex.Message);
        }
    }

    private static ErrorDetail BuildProcessError(ProcessResult result)
    {
        var stderr = result.StdErr?.Trim();
        var stdout = result.StdOut?.Trim();

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            if (stderr.Contains("dotnet-ef", StringComparison.OrdinalIgnoreCase) &&
                stderr.Contains("No executable found", StringComparison.OrdinalIgnoreCase))
            {
                return new ErrorDetail("migrations.dotnet_ef.missing", "dotnet-ef is not installed.", stderr);
            }

            if (stderr.Contains("was not found", StringComparison.OrdinalIgnoreCase) &&
                stderr.Contains("dotnet-ef", StringComparison.OrdinalIgnoreCase))
            {
                return new ErrorDetail("migrations.dotnet_ef.missing", "dotnet-ef is not installed.", stderr);
            }
        }

        var details = string.Join("\n\n", new[] { stdout, stderr }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return new ErrorDetail("migrations.dotnet_ef.failed", "dotnet ef command failed.", details);
    }
}
