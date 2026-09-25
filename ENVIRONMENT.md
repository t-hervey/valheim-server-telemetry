# Installed Valheim environment

Inspected on 2026-09-25 UTC. Paths and versions below are from this development clone, not assumptions from older Valheim releases.

| Item | Value |
|---|---|
| Host | `valheim-server-dev` |
| Service | `valheim.service` |
| Service user | `steam` |
| Working directory | `/home/steam/valheim_server` |
| Launch script | `/home/steam/valheim_server/start_valheim.sh` |
| BepInEx runner | `/home/steam/valheim_server/run_bepinex.sh` |
| Executable | `/home/steam/valheim_server/valheim_server.x86_64` |
| Valheim | `l-1.0.12`, network version 40, Steam build ID 25253791 |
| Unity | `6000.0.75f1` |
| Managed runtime | Unity MonoBleedingEdge, CLR `4.0.30319.42000` |
| Game assembly | `/home/steam/valheim_server/valheim_server_Data/Managed/assembly_valheim.dll` |
| Managed assemblies | `/home/steam/valheim_server/valheim_server_Data/Managed` |
| BepInEx | `5.4.23.5` Linux x64 |
| BepInEx root | `/home/steam/valheim_server/BepInEx` |
| HarmonyX | bundled `0Harmony.dll`, assembly version `2.9.0` |
| BepInEx config | `/home/steam/valheim_server/BepInEx/config/BepInEx.cfg` |
| Plugin config | `/home/steam/valheim_server/BepInEx/config/dev.deepnorth.valheimtelemetry.cfg` |
| Plugin directory | `/home/steam/valheim_server/BepInEx/plugins` |
| BepInEx file log | `/home/steam/valheim_server/BepInEx/LogOutput.log` |
| Save root | `/home/valheim_data/config` |
| World | `DeepNorthOrBust` |
| World storage | `/home/valheim_data/config/worlds_local/DeepNorthOrBust` (chunked 1.0 format) |
| Development server display name | `DeepNorthOrBust-DEV` |
| Public listing | disabled with `-public 0` |
| Plugin target | `netstandard2.1` |

The target framework was selected from the decompiled Valheim 1.0 project metadata (`netstandard2.1`) and verified by loading the built DLL under the installed Unity Mono/BepInEx runtime. Build references point at local development copies when `../references` exists, with configurable fallback paths to the live installation. No Valheim, Unity, BepInEx, or Harmony DLL is copied into the plugin output.

Normal BepInEx `Info` logging initially went only to `LogOutput.log`. The local BepInEx console sink was enabled and `StandardOutType` set to `StandardOut`, after which plugin lines were verified in `journalctl -u valheim.service`.

The server has a pre-existing vanilla `DllNotFoundException` for `libParty.so`, plus Unity `AsyncResourceUpload`/missing-behaviour messages. These occurred without the telemetry plugin and do not prevent normal Steam server readiness. BepInEx also prints Unity 6 `UnityLogWriter` internal-call synchronization warnings during bootstrap; BepInEx 5.4.23.5 continues to chainload successfully and the plugin/server remain operational.

Final deployed artifact:

```text
/home/steam/valheim_server/BepInEx/plugins/ValheimTelemetry.dll
```
