using System.Text.RegularExpressions;
using DevTools.Core.Validation;
using DevTools.Migrations.Models;

namespace DevTools.Migrations.Validators;

public sealed class MigrationsEntityValidator : IValidator<MigrationsEntity>
{
    public ValidationResult Validate(MigrationsEntity instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("entity", "Entity não pode ser nulo."));

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.Name))
            errors.Add(new ValidationError("name", "Nome é obrigatório."));

        if (string.IsNullOrWhiteSpace(instance.RootPath))
            errors.Add(new ValidationError("rootPath", "RootPath é obrigatório."));

        if (string.IsNullOrWhiteSpace(instance.StartupProjectPath))
            errors.Add(new ValidationError("startupProjectPath", "StartupProjectPath é obrigatório."));

        if (string.IsNullOrWhiteSpace(instance.DbContextFullName))
            errors.Add(new ValidationError("dbContextFullName", "DbContextFullName é obrigatório."));

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }
}

public sealed class MigrationsRequestValidator : IValidator<MigrationsRequest>
{
    private static readonly Regex MigrationNameRegex =
        new("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public ValidationResult Validate(MigrationsRequest instance)
    {
        if (instance is null)
            return ValidationResult.Fail(new ValidationError("request", "Request não pode ser nulo."));

        var errors = new List<ValidationError>();

        if (instance.Settings is null)
        {
            errors.Add(new ValidationError("settings", "Settings são obrigatórios."));
            return ValidationResult.Fail(errors);
        }

        if (!Enum.IsDefined(typeof(MigrationsAction), instance.Action))
            errors.Add(new ValidationError("action", "Action inválida."));

        if (!Enum.IsDefined(typeof(DatabaseProvider), instance.Provider))
            errors.Add(new ValidationError("provider", "Provider inválido."));

        if (string.IsNullOrWhiteSpace(instance.Settings.RootPath))
            errors.Add(new ValidationError("rootPath", "RootPath é obrigatório."));
        else if (!Directory.Exists(instance.Settings.RootPath))
            errors.Add(new ValidationError("rootPath", "RootPath não encontrado.", instance.Settings.RootPath));

        if (string.IsNullOrWhiteSpace(instance.Settings.StartupProjectPath))
            errors.Add(new ValidationError("startupProjectPath", "StartupProjectPath é obrigatório."));
        else if (!File.Exists(instance.Settings.StartupProjectPath))
            errors.Add(new ValidationError("startupProjectPath", "StartupProjectPath não encontrado.", instance.Settings.StartupProjectPath));

        if (string.IsNullOrWhiteSpace(instance.Settings.DbContextFullName))
            errors.Add(new ValidationError("dbContextFullName", "DbContextFullName é obrigatório."));

        if (instance.Action == MigrationsAction.AddMigration)
        {
            if (string.IsNullOrWhiteSpace(instance.MigrationName))
                errors.Add(new ValidationError("migrationName", "MigrationName é obrigatório para AddMigration."));
            else if (!MigrationNameRegex.IsMatch(instance.MigrationName))
                errors.Add(new ValidationError("migrationName", "MigrationName deve ser alfanumérico/underscore e começar com letra."));
        }

        var target = instance.Settings.Targets?.FirstOrDefault(t => t.Provider == instance.Provider);
        if (target is null || string.IsNullOrWhiteSpace(target.MigrationsProjectPath))
            errors.Add(new ValidationError("target", "MigrationsProjectPath para o provider é obrigatório."));
        else if (!File.Exists(target.MigrationsProjectPath))
            errors.Add(new ValidationError("target", "MigrationsProjectPath não encontrado.", target.MigrationsProjectPath));

        if (!string.IsNullOrWhiteSpace(instance.WorkingDirectory) && !Directory.Exists(instance.WorkingDirectory))
            errors.Add(new ValidationError("workingDirectory", "WorkingDirectory não encontrado.", instance.WorkingDirectory));

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }
}
