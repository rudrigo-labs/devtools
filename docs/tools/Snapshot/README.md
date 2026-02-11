# Snapshot

**Overview**
Generates a snapshot of a folder structure in TXT/JSON/HTML.

**Usage**
- CLI key: `snapshot`
- Inputs: root path, optional output base, formats, max KB per file, ignored folders
- Output base default: `<root>/Snapshot`

**Configuration**
- No config file. All options are set in the CLI prompts.

**Output**
- TXT: `Snapshot/Text/snapshot.txt`
- JSON (nested): `Snapshot/JsonNested/snapshot-nested.json`
- JSON (recursive): `Snapshot/JsonRecursive/snapshot-recursive.json`
- HTML preview: `Snapshot/WebPreview/`

**Logs**
- Errors only: `%AppData%/DevTools/logs/snapshot-YYYYMMDD.log`
