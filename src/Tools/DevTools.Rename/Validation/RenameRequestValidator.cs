using Microsoft.CodeAnalysis.CSharp;
using DevTools.Core.Results;
using DevTools.Rename.Models;

namespace DevTools.Rename.Validation;

public static class RenameRequestValidator
{
    public static IReadOnlyList<ErrorDetail> Validate(RenameRequest request)
    {
        var errors = new List<ErrorDetail>();

        if (request is null)
        {
            errors.Add(new ErrorDetail("rename.request.null", "Request is null."));
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.RootPath))
        {
            errors.Add(new ErrorDetail("rename.root.required", "RootPath is required."));
        }
        else if (!Directory.Exists(request.RootPath))
        {
            errors.Add(new ErrorDetail("rename.root.not_found", "RootPath not found.", request.RootPath));
        }

        if (string.IsNullOrWhiteSpace(request.OldText))
            errors.Add(new ErrorDetail("rename.old.required", "OldText is required."));

        if (string.IsNullOrWhiteSpace(request.NewText))
            errors.Add(new ErrorDetail("rename.new.required", "NewText is required."));

        if (!string.IsNullOrWhiteSpace(request.OldText) &&
            !string.IsNullOrWhiteSpace(request.NewText) &&
            string.Equals(request.OldText, request.NewText, StringComparison.Ordinal))
        {
            errors.Add(new ErrorDetail("rename.text.same", "OldText and NewText are the same."));
        }

        if (request.MaxDiffLinesPerFile <= 0)
            errors.Add(new ErrorDetail("rename.diff.invalid", "MaxDiffLinesPerFile must be greater than zero."));

        if (!Enum.IsDefined(typeof(RenameMode), request.Mode))
            errors.Add(new ErrorDetail("rename.mode.invalid", "Mode is invalid."));

        if (!string.IsNullOrWhiteSpace(request.ReportPath))
        {
            var trimmed = request.ReportPath.Trim();
            if (trimmed.Length == 0)
                errors.Add(new ErrorDetail("rename.report.invalid", "ReportPath is invalid."));
        }

        if (!string.IsNullOrWhiteSpace(request.UndoLogPath))
        {
            var trimmed = request.UndoLogPath.Trim();
            if (trimmed.Length == 0)
                errors.Add(new ErrorDetail("rename.undo.invalid", "UndoLogPath is invalid."));
        }

        if (!string.IsNullOrWhiteSpace(request.OldText) &&
            !string.IsNullOrWhiteSpace(request.NewText))
        {
            if (request.Mode == RenameMode.NamespaceOnly || request.OldText.Contains('.'))
            {
                if (!IsValidNamespace(request.OldText))
                    errors.Add(new ErrorDetail("rename.namespace.old.invalid", "OldText must be a valid namespace."));

                if (!IsValidNamespace(request.NewText))
                    errors.Add(new ErrorDetail("rename.namespace.new.invalid", "NewText must be a valid namespace."));
            }
            else
            {
                if (!SyntaxFacts.IsValidIdentifier(request.OldText))
                    errors.Add(new ErrorDetail("rename.identifier.old.invalid", "OldText must be a valid identifier."));

                if (!SyntaxFacts.IsValidIdentifier(request.NewText))
                    errors.Add(new ErrorDetail("rename.identifier.new.invalid", "NewText must be a valid identifier."));
            }
        }

        return errors;
    }

    private static bool IsValidNamespace(string value)
    {
        var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;

        return parts.All(SyntaxFacts.IsValidIdentifier);
    }
}
