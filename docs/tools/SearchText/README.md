# SearchText

**Overview**
Searches text or regex across files with filters.

**Usage**
- CLI key: `searchtext`
- Inputs: root folder, pattern, regex on/off, case-sensitive, whole word
- Optional: include/exclude globs, max KB per file, skip binaries, max matches per file, return lines

**Configuration**
- No config file. All options are set in the CLI prompts.

**Output**
- Summary plus optional per-file matches and lines

**Logs**
- Errors only: `%AppData%/DevTools/logs/searchtext-YYYYMMDD.log`
