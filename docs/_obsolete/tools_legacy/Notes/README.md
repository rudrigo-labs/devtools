# Notes

**Overview**
Reads, saves, and sends notes (local store or email).

**Usage**
- CLI key: `notes`
- Actions: read note, save note, send email
- Note storage path: default `%AppData%/DevTools/notes` (override in CLI)

**Configuration**
- Email config file: optional path in CLI
- Default email config path: `%LocalAppData%/DevTools/mail-config.json`
- Config uses JSON with SMTP settings and credentials (see `DevTools.Notes.Models.EmailConfig`)

**Output**
- Notes are stored as `.txt` files in the notes folder

**Logs**
- Errors only: `%AppData%/DevTools/logs/notes-YYYYMMDD.log`
