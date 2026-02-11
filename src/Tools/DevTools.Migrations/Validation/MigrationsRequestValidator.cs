using System.Text.RegularExpressions;
using DevTools.Core.Abstractions;
using DevTools.Core.Results;
using DevTools.Migrations.Models;

namespace DevTools.Migrations.Validation;

public static class MigrationsRequestValidator
{
    private static readonly Regex MigrationNameRegex = new("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public static IReadOnlyList<ErrorDetail> Validate(MigrationsRequest request, IFileSystem fs)
    {
        var errors = new List<ErrorDetail>();

        if (request is null)
        {
            errors.Add(new ErrorDetail("migrations.request.null", "Request is null."));
            return errors;
        }

        if (request.Settings is null)
        {
            errors.Add(new ErrorDetail("migrations.settings.null", "Settings are required."));
            return errors;
        }

        if (!Enum.IsDefined(typeof(MigrationsAction), request.Action))
            errors.Add(new ErrorDetail("migrations.action.invalid", "Action is invalid."));

        if (!Enum.IsDefined(typeof(DatabaseProvider), request.Provider))
            errors.Add(new ErrorDetail("migrations.provider.invalid", "Provider is invalid."));

        if (string.IsNullOrWhiteSpace(request.Settings.RootPath))
        {
            errors.Add(new ErrorDetail("migrations.root.required", "RootPath is required."));
        }
        else if (!fs.DirectoryExists(request.Settings.RootPath))
        {
            errors.Add(new ErrorDetail("migrations.root.not_found", "RootPath not found.", request.Settings.RootPath));
        }

        if (string.IsNullOrWhiteSpace(request.Settings.StartupProjectPath))
        {
            errors.Add(new ErrorDetail("migrations.startup.required", "StartupProjectPath is required."));
        }
        else if (!fs.FileExists(request.Settings.StartupProjectPath))
        {
            errors.Add(new ErrorDetail("migrations.startup.not_found", "StartupProjectPath not found.", request.Settings.StartupProjectPath));
        }

        if (string.IsNullOrWhiteSpace(request.Settings.DbContextFullName))
        {
            errors.Add(new ErrorDetail("migrations.dbcontext.required", "DbContextFullName is required."));
        }

        if (request.Action == MigrationsAction.AddMigration)
        {
            if (string.IsNullOrWhiteSpace(request.MigrationName))
                errors.Add(new ErrorDetail("migrations.name.required", "MigrationName is required for AddMigration."));
            else if (!MigrationNameRegex.IsMatch(request.MigrationName))
                errors.Add(new ErrorDetail("migrations.name.invalid", "MigrationName must be alphanumeric/underscore and start with a letter."));
        }

        var target = request.Settings.Targets?.FirstOrDefault(t => t.Provider == request.Provider);
        if (target is null || string.IsNullOrWhiteSpace(target.MigrationsProjectPath))
        {
            errors.Add(new ErrorDetail("migrations.target.required", "MigrationsProjectPath for provider is required."));
        }
        else if (!fs.FileExists(target.MigrationsProjectPath))
        {
            errors.Add(new ErrorDetail("migrations.target.not_found", "MigrationsProjectPath not found.", target.MigrationsProjectPath));
        }

        if (!string.IsNullOrWhiteSpace(request.WorkingDirectory) && !fs.DirectoryExists(request.WorkingDirectory))
            errors.Add(new ErrorDetail("migrations.workdir.not_found", "WorkingDirectory not found.", request.WorkingDirectory));

        return errors;
    }
}
