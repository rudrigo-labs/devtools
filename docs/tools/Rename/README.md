# Rename

**Overview**
Renames C# identifiers and files using Roslyn with dry-run, backup, and undo log options.

**Usage**
- CLI key: `rename`
- Inputs: root folder, old text, new text
- Mode: general identifiers or namespace-only
- Options: dry-run, backup, undo log
- Filters: include/exclude globs

**Configuration**
- No config file. All options are set in the CLI prompts.

**Output**
- Summary of updated/renamed files and optional report/undo log

**Logs**
- Errors only: `%AppData%/DevTools/logs/rename-YYYYMMDD.log`
