# Harvest

**Overview**
Harvest scans a codebase and ranks files by reuse signals (fan-in, fan-out, keyword density, public static usage).

**Usage**
- CLI key: `harvest`
- Inputs: root path, optional config path, optional min score, optional top N
- Output: ranked list in the console (use "Mostrar lista detalhada" to show hits)

**Configuration**
- Config file: optional JSON path. If omitted, embedded defaults are used.
- `rules.extensions`: file extensions to analyze
- `rules.excludeDirectories`: folders to skip
- `rules.ignoreUsingPrefixes`: namespace prefixes to ignore in C# using resolution
- `rules.maxFileSizeKb`: optional size cap per file
- `weights.fanInWeight`, `weights.fanOutWeight`, `weights.keywordDensityWeight`
- `weights.densityScale`, `weights.staticMethodThreshold`, `weights.staticMethodBonus`
- `weights.deadCodePenalty`, `weights.largeFileThresholdLines`, `weights.largeFilePenalty`
- `categories[]`: keyword categories with `name`, `weight`, `keywords`

**Logs**
- Errors only: `%AppData%/DevTools/logs/harvest-YYYYMMDD.log`
