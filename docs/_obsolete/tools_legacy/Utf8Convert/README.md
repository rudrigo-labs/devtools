# Utf8Convert

**Overview**
Converts text files to UTF-8 with optional backup and dry-run.

**Usage**
- CLI key: `utf8`
- Inputs: root folder, recursive, dry-run, backup, output BOM
- Optional: include/exclude globs

**Configuration**
- No config file. All options are set in the CLI prompts.

**Output**
- Converts files in place (backup optional)

**Logs**
- Errors only: `%AppData%/DevTools/logs/utf8-YYYYMMDD.log`
