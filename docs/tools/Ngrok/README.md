# Ngrok

**Overview**
Manages ngrok tunnels via the local ngrok API.

**Usage**
- CLI key: `ngrok`
- Actions: list tunnels, close tunnel, start HTTP tunnel, kill all, status
- Optional: API base URL (default `http://127.0.0.1:4040/`), timeout, retry count
- Start HTTP options: protocol, port, ngrok executable path, extra args

**Configuration**
- No config file. All options are set in the CLI prompts.

**Logs**
- Errors only: `%AppData%/DevTools/logs/ngrok-YYYYMMDD.log`
