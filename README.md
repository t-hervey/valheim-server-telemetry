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
dotnet build -c Release
```

For another installation, override `LocalReferenceDir`, or set `ValheimManagedDir` and `BepInExCoreDir` as MSBuild properties. The target is `netstandard2.1`, determined from this Valheim 1.0 installation. The release artifact is:

```text
/home/codex/valheim-development/ValheimTelemetry/bin/Release/netstandard2.1/ValheimTelemetry.dll
```

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
