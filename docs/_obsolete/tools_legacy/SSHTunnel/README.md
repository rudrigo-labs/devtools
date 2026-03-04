# SSHTunnel

**Overview**
Starts, stops, and checks status of local SSH tunnels.

**Usage**
- CLI key: `sshtunnel`
- Actions: start, stop, status
- Start inputs: SSH host/user/port, local bind/port, remote host/port, strict host key checking, optional timeout

**Configuration**
- Default config file (when used by providers): `%AppData%/Sqlt/sqlt.json`
- CLI does not require the config file; it builds the profile from prompts

**Logs**
- Errors only: `%AppData%/DevTools/logs/sshtunnel-YYYYMMDD.log`
