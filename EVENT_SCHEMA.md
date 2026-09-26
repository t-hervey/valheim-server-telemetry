# Telemetry event schema

Every physical log line contains the configured prefix, one ASCII space, and one valid flat JSON object. JSON numbers use invariant culture. Optional or unavailable values are `null`, not fabricated.

## Common properties

| Property | Type | Meaning |
|---|---|---|
| `schema_version` | integer | Always `1` |
| `event` | string | Event name below |
| `timestamp` | string | UTC ISO-8601 with millisecond precision |
| `world` | string/null | Valheim world name from `ZNet` |
| `player_name` | string/null | Reliably resolved character name, subject to privacy config |
| `player_id` | string/null | Valheim character/player ID serialized as a string, subject to privacy config |
| `x`, `y`, `z` | number/null | World position, subject to privacy config |

## `mob_killed`

`player_name`, `player_id`, `mob_type`, `mob_display_name`, `mob_level`, `mob_stars`, `x`, `y`, `z`, `biome`. `mob_type` is the raw prefab identifier. Stars are `max(level - 1, 0)`.

## `boss_killed`

`player_name`, `player_id`, `boss_type`, `boss_display_name`, `boss_level`, `boss_stars`, `x`, `y`, `z`, `biome`. The event is emitted in addition to `mob_killed` for a runtime prefab whose `Character.m_boss` flag is true. Killer identity remains null unless a reliable recent routed damage event exists.

## `tamed_creature_died`

`player_name`, `player_id`, `creature_type`, `creature_display_name`, `creature_level`, `creature_stars`, `x`, `y`, `z`, `biome`. The player fields describe a reliably observed killer, not the owner/tamer, and are commonly null. This event is emitted in addition to `mob_killed`.

## `mob_spawned`

`mob_type`, `mob_display_name`, `mob_level`, `mob_stars`, `spawn_source`, `x`, `y`, `z`, `biome`.

`spawn_source` is one of `CreatureSpawner`, `event`, `breeding`, `summon`, `boss`, or `unknown`. `SpawnSystem` cannot be proven from persisted server data and is therefore represented as `unknown`, not guessed.

## `portal_built` and `portal_destroyed`

`portal_id`, `portal_type`, `portal_tag`, `player_name`, `player_id`, `x`, `y`, `z`. `portal_id` is the stable ZDOID string and `portal_type` is the raw prefab identifier. At destruction, the player fields are commonly null.

## `portal_tag_changed`

`portal_id`, `portal_type`, `old_portal_tag`, `portal_tag`, `player_name`, `player_id`, `x`, `y`, `z`. The actor fields are populated only when the replicated platform tag-author ID exactly matches a connected player. The initial tag associated with a newly built portal is included in `portal_built` and suppressed as a separate tag-change event.

## `portal_travel`

`player_name`, `player_id`, `from_portal_id`, `from_portal_type`, `from_portal_tag`, `from_x`, `from_y`, `from_z`, `from_biome`, `to_portal_id`, `to_portal_type`, `to_portal_tag`, `to_x`, `to_y`, `to_z`, `to_biome`, `detection_method`, `confidence`.

`detection_method` is `server_position_jump` and `confidence` is `high_confidence`. The portal IDs are stable ZDOID strings; tags are mutable and need not be unique. Coordinates become null when position privacy is disabled. This is a conservative inference from replicated positions and the actual portal connection graph, not observation of the client-only portal trigger.

## `piece_built` and `piece_destroyed`

`piece_type`, `piece_display_name`, `player_name`, `player_id`, `x`, `y`, `z`. Portal and ship prefabs are excluded from generic piece events.

## `ship_built` and `ship_destroyed`

`ship_type`, `ship_display_name`, `player_name`, `player_id`, `x`, `y`, `z`. Types are runtime prefab identifiers rather than a hard-coded raft/karve/longship list.

## `creature_tamed`

`creature_type`, `creature_display_name`, `creature_level`, `creature_stars`, `tame_method`, `player_name`, `player_id`, `x`, `y`, `z`.

`tame_method` is currently `player_tamed` for an existing creature's false-to-true tame transition. The player fields are null because vanilla server-visible state does not persist the responsible player. Already-tamed new/bred creatures do not emit this event.

## `tree_felled`

`tree_type`, `tree_display_name`, `player_name`, `player_id`, `x`, `y`, `z`. Standing `TreeBase` prefabs and destroyed tree-growing `Plant` saplings qualify. `TreeLog` does not. Sapling events are delayed briefly so normal growth into a replacement `TreeBase` can be suppressed.

## `player_connected`

`player_name`, `player_id`, `x`, `y`, `z`. Emitted once an accepted peer has a live character ZDO, rather than when a preliminary socket first appears.

## `player_disconnected`

`player_name`, `player_id`, `session_duration_seconds`, `x`, `y`, `z`. Duration is a non-negative number measured from the emitted gameplay connection to graceful peer disconnection. Position is the last server-observed character position.

## `player_died`

`player_name`, `player_id`, `death_cause`, `x`, `y`, `z`, `biome`. `death_cause` is currently always `null`: Valheim retains the authoritative final-hit details on the owning client, so the plugin does not invent a cause.

## `world_key_changed`

| Property | Type | Meaning |
|---|---|---|
| `action` | string | `added`, `updated`, or `removed` |
| `key` | string | Normalized global-key name |
| `value` | string/null | New optional key value; null for removal or a valueless key |
| `previous_value` | string/null | Prior optional value; null when absent/valueless |

Startup baseline keys and identical value reassignments are not emitted. Runtime changes made through gameplay, administration, or another mod are all represented because vanilla state does not persist a change origin.

## `entity_count`

| Property | Type | Meaning |
|---|---|---|
| `category` | string | `active_mob`, `active_tamed_creature`, `loaded_piece`, `loaded_portal`, or `loaded_ship` |
| `entity_type` | string | Raw prefab identifier |
| `count` | integer | Count in the union of ready peers' near simulation areas |

Zero-value groups are omitted; an empty snapshot emits no JSON lines. These categories must not be interpreted as persistent world totals.
