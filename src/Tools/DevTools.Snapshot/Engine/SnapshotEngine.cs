using DevTools.Core.Abstractions;
using DevTools.Core.Models;
using DevTools.Core.Results;
using DevTools.Core.Validation;
using DevTools.Snapshot.Models;

namespace DevTools.Snapshot.Engine;

public sealed class SnapshotEngine : IDevToolEngine<SnapshotExecutionRequest, SnapshotExecutionResult>
{
    private readonly IValidator<SnapshotExecutionRequest> _validator;

    public SnapshotEngine(IValidator<SnapshotExecutionRequest>? validator = null)
    {
        _validator = validator ?? new Validation.SnapshotExecutionRequestValidator();
    }

    public Task<RunResult<SnapshotExecutionResult>> ExecuteAsync(
        SnapshotExecutionRequest request,
        IProgressReporter? progress = null,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(x => new ErrorDetail(
                    Code: x.Code ?? "VALIDATION_ERROR",
                    Message: x.Message,
                    Cause: x.Field))
                .ToArray();

            return Task.FromResult(RunResult<SnapshotExecutionResult>.Fail(errors));
        }

        progress?.Report(new ProgressEvent("Snapshot em execucao.", 5, "snapshot"));

        var result = new SnapshotExecutionResult
        {
            RootPath = request.RootPath,
            GeneratedArtifacts = Array.Empty<string>(),
            TotalFilesScanned = 0,
            TotalFilesIncluded = 0
        };

        progress?.Report(new ProgressEvent("Snapshot concluido.", 100, "snapshot"));
        return Task.FromResult(RunResult<SnapshotExecutionResult>.Success(result));
    }
}
