# Migrations

**Overview**
Assists `dotnet ef` for add migration and update database operations.

**Usage**
- CLI key: `migrations`
- Inputs: action (add/update), provider (SqlServer/Sqlite), project root, startup project, DbContext, migrations project, optional args
- Optional: migration name (add only), dry-run (default true), working directory

**Configuration**
- No config file. All options are set in the CLI prompts.

**Output**
- Dry-run prints the full `dotnet ef` command
- Non dry-run executes `dotnet ef` and prints stdout/stderr

**Requirements**
- `dotnet-ef` must be installed

**Logs**
- Errors only: `%AppData%/DevTools/logs/migrations-YYYYMMDD.log`
