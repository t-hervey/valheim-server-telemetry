# ValheimTelemetry

ValheimTelemetry is a passive, server-only BepInEx plugin for Valheim 1.0. It emits one flat JSON document per event through the normal BepInEx logger. On this development clone that logger is routed to stdout, so systemd captures the complete line for journald, Loki, and Grafana.

The plugin does not change ownership, spawn objects, write ZDO properties, register custom RPCs, alter game behavior, or require a client mod. Vanilla Valheim 1.0 clients can connect normally.

## Architecture

Harmony postfix/prefix observers watch the server's existing persistence and routing paths:

- genuine ZDO creation and destruction through `ZDOMan`;
- replicated health and tame-state changes through `ZDO.Deserialize` and local `ZDO.Set` calls;
- copies of vanilla routed `RPC_Damage` packages for conservative player attribution;
- vanilla peer lifecycle, player-death RPC/state, portal-tag, and global-key transitions;
- peer active simulation sectors for periodic loaded/active counts.

Callbacks only copy or read data, catch their own exceptions, and never suppress a vanilla method. `ITelemetrySink` separates event tracking from output formatting. Lifecycle caches are bounded. A 20-second post-world-load grace means existing ZDOs establish a baseline without generating build, spawn, tame, or felling events.

## Build

The development tree contains uncommitted local reference copies in `../references`. They are not bundled in the output or tracked by Git.

```bash
cd /home/codex/valheim-development/ValheimTelemetry
dotnet build ValheimTelemetry.sln -c Release
```

For another installation, override `LocalReferenceDir`, or set `ValheimManagedDir` and `BepInExCoreDir` as MSBuild properties. The target is `netstandard2.1`, determined from this Valheim 1.0 installation. The release artifact is:

```text
/home/codex/valheim-development/ValheimTelemetry/bin/Release/netstandard2.1/ValheimTelemetry.dll
```

## Unit tests

The .NET 8 test project uses xUnit, FluentAssertions, and Moq. Tests follow Arrange/Act/Assert and keep Unity, BepInEx, and live-world behavior outside the unit-test boundary.

```bash
dotnet test ValheimTelemetry.sln -c Release
```

The phased testing strategy, conventions, package choices, and integration-test boundaries are recorded in [TESTING.md](TESTING.md).

## Installation and upgrades

### Runtime dependencies

The server must already have:

- a Valheim dedicated server compatible with Valheim 1.0;
- BepInEx 5 for Unix x64 (tested with `5.4.23.5`); and
- HarmonyX/`0Harmony.dll`, which is included in a normal BepInEx 5 installation.

The release contains only `ValheimTelemetry.dll`. Do not copy Valheim, Unity, BepInEx, or Harmony assemblies from the build machine. Jotunn and a .NET SDK are not runtime dependencies. Nothing is installed on clients, and an unmodified Valheim client can connect normally.

If BepInEx is not installed yet, obtain the current BepInEx 5 Unix x64 release from the [official BepInEx releases](https://github.com/BepInEx/BepInEx/releases), extract it into the Valheim server root, and start the server through BepInEx's `run_bepinex.sh` once. Preserve all of the server's existing Valheim launch arguments. Before continuing, verify that these files exist (paths are relative to the server root):

```text
BepInEx/core/BepInEx.dll
BepInEx/core/0Harmony.dll
run_bepinex.sh
```

Also verify that `BepInEx/LogOutput.log` reports a successful chainloader startup. Service-manager configuration varies by installation; a systemd service must invoke `run_bepinex.sh`, directly or through a launch script, rather than bypassing BepInEx and starting `valheim_server.x86_64` by itself.

### First installation

1. Download `ValheimTelemetry.dll` from the [latest GitHub release](https://github.com/t-hervey/valheim-server-telemetry/releases/latest). The ZIP contains the same DLL plus documentation and an example configuration. Authenticated GitHub CLI users can instead download the DLL with:

   ```bash
   download_dir="$(mktemp -d)"
   gh release download \
     --repo t-hervey/valheim-server-telemetry \
     --pattern 'ValheimTelemetry.dll' \
     --dir "$download_dir"
   ```

2. Stop the dedicated-server service. Substitute the actual service name and server root for the examples below:

   ```bash
   sudo systemctl stop valheim.service
   ```

3. Install the DLL with the same owner and group as the server process. This example uses the paths and account from the development clone:

   ```bash
   sudo install -o steam -g steam -m 0644 \
     "$download_dir/ValheimTelemetry.dll" \
     /home/steam/valheim_server/BepInEx/plugins/ValheimTelemetry.dll
   ```

4. Start the service and verify both the BepInEx load message and telemetry initialization:

   ```bash
   sudo systemctl start valheim.service
   sudo journalctl -u valheim.service -n 200 --no-pager \
     | grep -E 'BepInEx|ValheimTelemetry|VALHEIM_TELEMETRY|Exception|MissingMethod|TypeLoad|FileNotFound'
   ```

5. On first load, BepInEx creates `BepInEx/config/dev.deepnorth.valheimtelemetry.cfg`. Stop the service before editing it, compare it with [the example configuration](examples/dev.deepnorth.valheimtelemetry.cfg), then start the service again. The default configuration is usable without edits.

Successful installation shows `Loading ValheimTelemetry <version>` followed by the plugin's initialization and arming messages. A normal startup should not contain a Harmony patch failure, `MissingMethodException`, `TypeLoadException`, or plugin-related `FileNotFoundException`.

### Updating to a new release

Read the new release notes and [feature inventory](FEATURES.md) first, especially when an event schema or configuration setting changes. Upgrading the DLL does not overwrite the existing BepInEx configuration.

1. Download the new release DLL into a temporary directory using the release page or the `gh release download` command above.
2. Stop the server and preserve the currently working DLL and configuration:

   ```bash
   sudo systemctl stop valheim.service
   backup_dir="/home/steam/valheim_server/BepInEx/backups/ValheimTelemetry-$(date -u +%Y%m%dT%H%M%SZ)"
   sudo install -d -o steam -g steam -m 0755 "$backup_dir"
   sudo cp -a /home/steam/valheim_server/BepInEx/plugins/ValheimTelemetry.dll "$backup_dir/"
   sudo cp -a /home/steam/valheim_server/BepInEx/config/dev.deepnorth.valheimtelemetry.cfg "$backup_dir/"
   ```

3. Replace only the plugin DLL, start the server, and repeat the log verification from the installation procedure:

   ```bash
   sudo install -o steam -g steam -m 0644 \
     "$download_dir/ValheimTelemetry.dll" \
     /home/steam/valheim_server/BepInEx/plugins/ValheimTelemetry.dll
   sudo systemctl start valheim.service
   sudo journalctl -u valheim.service -n 200 --no-pager \
     | grep -E 'BepInEx|ValheimTelemetry|VALHEIM_TELEMETRY|Exception|MissingMethod|TypeLoad|FileNotFound'
   ```

Confirm that the log reports the expected new version and that the server reaches its normal ready/listening state. Existing objects must not generate false lifecycle events after an update or restart; periodic loaded-entity snapshots are expected.

To roll back, stop the service, install the backed-up DLL over `BepInEx/plugins/ValheimTelemetry.dll`, restore the matching configuration only if it was changed for the new version, and start the service. Keep only one copy of the plugin DLL anywhere under `BepInEx/plugins`; a second renamed DLL can cause BepInEx to load the plugin twice.

## Development-clone installation

The verified plugin path is `/home/steam/valheim_server/BepInEx/plugins/ValheimTelemetry.dll` and the service is `valheim.service`.

```bash
sudo install -o steam -g steam -m 0644 \
  /home/codex/valheim-development/ValheimTelemetry/bin/Release/netstandard2.1/ValheimTelemetry.dll \
  /home/steam/valheim_server/BepInEx/plugins/ValheimTelemetry.dll
sudo systemctl restart valheim.service
```

Do not copy this development artifact to production as part of this workflow.

## Configuration

BepInEx generated `/home/steam/valheim_server/BepInEx/config/dev.deepnorth.valheimtelemetry.cfg`. A generated example is in [examples/dev.deepnorth.valheimtelemetry.cfg](examples/dev.deepnorth.valheimtelemetry.cfg).

Defaults enable all event families and snapshots, including player sessions/deaths, tamed-creature deaths, boss kills, portal tag changes, and world-key changes. They include player identity and positions when reliable, use a 300-second interval, and prefix every document with `VALHEIM_TELEMETRY`. Set `DebugLogging = true` to log snapshot duration and object counts. The minimum accepted snapshot interval is 10 seconds.

Configuration errors fall back to safe defaults. Disabling privacy fields keeps their JSON properties but sets their values to `null`.

## Output

```text
VALHEIM_TELEMETRY {"schema_version":1,"event":"mob_killed","timestamp":"2026-09-25T02:14:17.421Z","world":"DeepNorthOrBust","player_name":"Travis","player_id":"1234","mob_type":"Greydwarf","mob_display_name":"Greydwarf","mob_level":2,"mob_stars":1,"x":123.4,"y":31.2,"z":-412.7,"biome":"BlackForest"}
```

The JSON is invariant-culture, UTC, single-line, and manually serialized without an extra runtime dependency. High-cardinality values remain JSON fields and are not treated as Loki labels.

Watch live telemetry on this clone:

```bash
sudo journalctl -u valheim.service -f --no-pager | grep --line-buffered VALHEIM_TELEMETRY
```

For startup and snapshot diagnostics:

```bash
sudo journalctl -u valheim.service -f --no-pager | grep --line-buffered -E 'ValheimTelemetry|VALHEIM_TELEMETRY'
```

## Loaded versus world totals

Valheim 1.0 stores the world in persistent ZDO chunks, but only areas near connected peers are actively simulated. Snapshot collection unions and de-duplicates the ready peers' near simulation sectors. It therefore emits `active_mob`, `active_tamed_creature`, `loaded_piece`, `loaded_portal`, and `loaded_ship`. These are not world totals. A reliable, inexpensive world-total classification is not available to this passive plugin and no metric is labeled as one.

With no connected players, a snapshot legitimately has no entity-count lines. With debug logging enabled, it still records a successful zero-peer execution time.

## Manual gameplay test checklist

Use an unmodified Valheim 1.0 client. For quicker snapshot testing, temporarily set `DebugLogging = true` and `SnapshotIntervalSeconds = 10`, then restart the service.

1. Restart `valheim.service`; expect initialization, server ready, then arming. Expect no lifecycle JSON during startup.
2. Connect the vanilla client; expect one `player_connected`. No plugin handshake or client DLL is required.
3. Die and respawn; expect one `player_died`. `death_cause` is null because the authoritative cause stays on the client.
4. Kill one Greydwarf; expect one `mob_killed`. Identity may be null if the owning-client RPC did not traverse the server.
5. Encounter/cause a normal spawn; expect one `mob_spawned`, usually with `spawn_source: "unknown"` for `SpawnSystem`.
6. Build one wall; expect one `piece_built` with its raw prefab type.
7. Destroy the wall; expect one `piece_destroyed`; destroyer identity may be null.
8. Build a portal, rename it, then destroy it; expect `portal_built`, `portal_tag_changed`, and `portal_destroyed`. Rename attribution should resolve while the author is connected.
9. Build/destroy a ship through normal play if practical; expect one `ship_built` and one `ship_destroyed`.
10. Tame an animal; expect one `creature_tamed`, method `player_tamed`, with null player identity.
11. Kill a tamed animal; expect both `mob_killed` and `tamed_creature_died` for the same creature.
12. Breed a tamed animal; expect a `mob_spawned` with `spawn_source: "breeding"`, but no `creature_tamed`.
13. Kill a boss through normal play if practical; expect `mob_killed`, `boss_killed`, and resulting `world_key_changed` events.
14. Fell one standing tree; expect one `tree_felled`.
15. Destroy a planted tree sapling; expect one `tree_felled` after a short growth-disambiguation delay.
16. Let a tree sapling grow naturally; expect no `tree_felled` for the replaced sapling ZDO.
17. Chop the resulting `TreeLog`; expect no additional `tree_felled`.
18. Wait for a snapshot with the client connected; expect `entity_count` lines grouped by runtime type.
19. Disconnect; expect one `player_disconnected` with `session_duration_seconds`.
20. Restart again; expect no false lifecycle or `world_key_changed` events from existing state. Snapshot events are allowed.

## Known limitations

- Creature simulation and many damage/taming methods execute on the owning client, not necessarily the dedicated server. The plugin observes persistent server state instead.
- For ordinary persistent mobs, ZDO destruction is treated as a death even when the owning client destroys it before synchronizing zero health. Explicit daytime/event/summon despawn classes still require zero-health or recent-damage evidence. Mod/admin deletion of an ordinary mob is therefore a possible false-positive kill.
- `SpawnSystem` has no persisted source marker, so ordinary spawns are reported as `unknown`. Event, boss, summon, breeding, and connected `CreatureSpawner` cases can be classified.
- Player identity is only present when creator data or a very recent vanilla damage RPC establishes it. When the attacking client owns the target, that RPC is handled locally and is not visible to the dedicated server; environmental, ambiguous, and DOT cases remain null.
- Player death occurrence is server-visible, but the dedicated server does not receive the client's `Player.m_lastHit`, so `death_cause` is deliberately null.
- Portal tag author is resolved from the replicated platform-author identifier only while that author appears in the server's current player list.
- Hammer removal does not reliably identify the remover. Destroy events are still correct, but attribution is normally null.
- A very tightly timed DOT/environmental death following a server-visible direct player hit could retain the recent-hit identity for up to 0.75 seconds; this is the principal remaining false-attribution edge case to test.
- Admin commands or another mod that directly flips `tamed` from false to true can look like normal taming.
- Tree saplings are identified at runtime as `Plant` prefabs whose grown prefab has `TreeBase`. Their destruction is delayed three seconds and suppressed if a grown replacement tree appears nearby. An unhealthy sapling that self-destructs without producing a tree can still look like player destruction because vanilla persists no destruction cause.
- Snapshot definitions are active/loaded scope, never persistent-world totals.

The canonical versioned feature inventory, including deferred ideas, is [FEATURES.md](FEATURES.md). Detailed hook evidence and classifications are in [OBSERVABILITY.md](OBSERVABILITY.md); the complete schema is in [EVENT_SCHEMA.md](EVENT_SCHEMA.md).
