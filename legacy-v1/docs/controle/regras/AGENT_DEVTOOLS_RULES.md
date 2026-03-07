# AGENT_DEVTOOLS_RULES (MANDATORY — Project Extension)

This file extends AGENT_EXECUTION_RULES.md for the DevTools ecosystem.
If there is any conflict, these DevTools rules win over the base rules.

---

## 1) Project folder rule (MANDATORY)

Every new project MUST be created inside a folder with the SAME name as the project.

Example:
- Project name: `DevTools.Snapshot.Core`
- Required folder path: `DevTools.Snapshot.Core/`
- The project MUST be created inside that folder.

No exceptions unless explicitly instructed.

---

## 2) Target framework rule (MANDATORY)

All projects MUST target **.NET 10**.

- Use `net10.0` for non-Windows projects.
- Do not use older target frameworks.
- Do not change the target unless explicitly instructed.

This is mandatory for all new projects and any adjustments requested by the user.
