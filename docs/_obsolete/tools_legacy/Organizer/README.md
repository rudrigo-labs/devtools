# Organizer

**Overview**
Classifies documents by category, deduplicates, and moves them into an output folder structure.

**Usage**
- CLI key: `organizer`
- Inputs: inbox folder, output folder, optional config path, optional min score, apply changes
- If `apply` is false, it only generates the plan (no move)

**Configuration**
- Config file: optional path. If empty, it loads `devtools.docs.json` from the output folder if present, otherwise defaults.
- `allowedExtensions`: default `.pdf`, `.txt`, `.md`, `.doc`, `.docx`
- `minScoreDefault`: default score threshold
- `fileNameWeight`: name weight multiplier
- `deduplicateByHash`: default true
- `deduplicateByName`: default true (name-based dedup)
- `deduplicateFirstLines`: number of first lines to compare (0 disables)
- `categories[]`: list of categories with `name`, `folder`, `keywords`, `negativeKeywords`, weights, and `minScore`

**Output**
- Creates category folders under the output path
- Creates `Duplicates` and `Outros` folders
- If no eligible files are found, the CLI shows a warning

**Logs**
- Errors only: `%AppData%/DevTools/logs/organizer-YYYYMMDD.log`
