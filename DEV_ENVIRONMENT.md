# Development-clone environment

## Safety inspection and local changes

- Verified unique hostname `valheim-server-dev`; no hostname change was required.
- Verified IPv4 `192.168.40.20/24`, default route through `192.168.40.1`, and matching `/etc/hosts` development names. External isolation was not weakened and no LAN scanning was performed.
- Identified `valheim.service` as the only Valheim service. It runs as `steam` from `/home/steam/valheim_server`.
- Changed the local display name and `-world` argument to `DeepNorthOrBust-DEV` and public/listing mode from `-public 1` to `-public 0`.
- Discovered on 2026-09-26 that `/home/valheim_data/config`, previously treated as clone-local, is a shared host-provided mount also used by production. With `valheim.service` stopped, copied the completed save-number-822 snapshot `DeepNorthOrBust_backup_auto-20260926-012542` into `/home/steam/valheim-dev-data/config/worlds_local/DeepNorthOrBust-DEV` on the LXC-local root filesystem. The source and copy matched recursively. Only the small admin, banned, permitted, and preferences files were also copied; historical backup archives were not.
- Changed `/home/steam/valheim_server/start_valheim.sh` to use `-savedir /home/steam/valheim-dev-data/config` and `-world DeepNorthOrBust-DEV`. The prior script is `/home/steam/valheim_server/start_valheim.sh.before-local-world-20260926`. Initial validation loaded all 283,426 ZDOs from 16 chunks at save number 822. Controlled restarts then wrote and loaded subsequent saves beneath the local path, most recently save number 824, reached `Game server connected`, and armed the plugin without world-load errors. The active development process no longer references `/home/valheim_data`.
- Added `InaccessiblePaths=/home/valheim_data` to `valheim.service` as defense in depth. Inside the service mount namespace the shared path is replaced by systemd's inaccessible read-only directory, and an explicit test confirmed the `steam` account cannot traverse it. The host/LXC administrative namespace still sees the original mount. The prior unit is `/etc/systemd/system/valheim.service.before-local-world-20260926`.
- Installed BepInEx 5.4.23.5 locally and changed the local launch script to run the server through `run_bepinex.sh`.
- Enabled BepInEx console logging to `StandardOut` so systemd/journald receives complete telemetry lines.
- Grafana Alloy 1.19.2 was installed in the clone with an existing production-style configuration. It was already disabled/inactive and was masked locally with `systemctl mask --now alloy.service` to prevent cloned telemetry export. `/etc/alloy/config.alloy` now routes Valheim journal entries through a validated telemetry processor that strips the BepInEx prefix, preserves a pure JSON Loki body, adds bounded event/world/category labels, and attaches the remaining JSON properties as structured metadata. An offline `loki.echo` test confirmed metadata extraction and unchanged pass-through for non-telemetry lines. Alloy remains masked and inactive; no remote endpoint was contacted. The pre-change file is `/etc/alloy/config.alloy.before-valheim-structured-20260925`.
- Tailscale is not installed. No Tailscale identity was touched.
- No custom user/root crontabs, backup agents, DDNS jobs, or custom systemd timers were found. Installed timers are routine apt, dpkg, logrotate, man-db, sysstat, tmpfiles, filesystem trim/scrub, MOTD, and Ubuntu Advantage timers.
- An enabled but inactive rsync service has no configured start condition/socket. Postfix is loopback-only. Neither was changed.
- BepInEx, the plugin DLL, and configuration were deployed only inside this clone. No remote host session or storage-administration interface was accessed. The already-mounted shared world snapshot was read only for the explicitly requested local copy and was not renamed, deleted, or modified.

Backups of changed local files are under `/home/codex/valheim-development/system-backups`, including `start_valheim.sh.before-dev-safety` and `BepInEx.cfg.before-telemetry-stdout`.

## Installed tools

The native Ubuntu package manager was used after detecting Ubuntu 25.04 (`plucky`). Installed/verified packages include:

| Package | Installed version |
|---|---|
| `git` | 2.48.1 |
| `curl` | 8.12.1 |
| `wget` | 1.24.5 |
| `ca-certificates` | 20241223 |
| `unzip` | 6.0 |
| `zip` | 3.0 |
| `tar` | 1.35 |
| `jq` | 1.7.1 |
| `ripgrep` | apt 14.1.1 (the Codex session PATH also exposes 15.2.0) |
| `tree` | 2.1.1 |
| `less` | 643 |
| `file` | 5.45 |
| `procps` | 4.0.4 |
| `build-essential` | 12.12 |
| `binutils` | 2.44 |
| `lsof` | 4.99.4 |
| `strace` | 6.14 |
| `dotnet-sdk-8.0` | SDK 8.0.122, runtime 8.0.22 |

The Ubuntu repository supplied the .NET SDK; no Microsoft package repository was required. `gdb`, desktop environments, and Unity Editor were not installed.

ILSpy command line 9.1.0.7988 is installed under `/home/codex/.dotnet/tools/ilspycmd`. The latest 11.x tool package had incompatible/malformed .NET 8 tool metadata, so the current .NET 8-compatible release was pinned. `/home/codex/.dotnet/tools` was added to `/home/codex/.profile`.

## Development artifacts

- Downloads: `/home/codex/valheim-development/downloads`
- Decompiled source: `/home/codex/valheim-development/decompiled` (not committed)
- Local compile references: `/home/codex/valheim-development/references` (not committed)
- Source repository: `/home/codex/valheim-development/ValheimTelemetry`
- System-file backups: `/home/codex/valheim-development/system-backups`

The official BepInEx Linux x64 5.4.23.5 archive was checksum-verified before installation (published SHA-256 `e538560be65739f562519ab518a75f9c65b3f57f87457403ae7cde683c12dab7`). Decompiled sources and copyrighted game assemblies are excluded from Git.
