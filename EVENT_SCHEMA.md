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

## `mob_spawned`

`mob_type`, `mob_display_name`, `mob_level`, `mob_stars`, `spawn_source`, `x`, `y`, `z`, `biome`.

`spawn_source` is one of `CreatureSpawner`, `event`, `breeding`, `summon`, `boss`, or `unknown`. `SpawnSystem` cannot be proven from persisted server data and is therefore represented as `unknown`, not guessed.

## `portal_built` and `portal_destroyed`

`portal_type`, `portal_tag`, `player_name`, `player_id`, `x`, `y`, `z`. `portal_type` is the raw prefab identifier. At destruction, the player fields are commonly null.

## `piece_built` and `piece_destroyed`

`piece_type`, `piece_display_name`, `player_name`, `player_id`, `x`, `y`, `z`. Portal and ship prefabs are excluded from generic piece events.

## `ship_built` and `ship_destroyed`

`ship_type`, `ship_display_name`, `player_name`, `player_id`, `x`, `y`, `z`. Types are runtime prefab identifiers rather than a hard-coded raft/karve/longship list.

## `creature_tamed`

`creature_type`, `creature_display_name`, `creature_level`, `creature_stars`, `tame_method`, `player_name`, `player_id`, `x`, `y`, `z`.

`tame_method` is currently `player_tamed` for an existing creature's false-to-true tame transition. The player fields are null because vanilla server-visible state does not persist the responsible player. Already-tamed new/bred creatures do not emit this event.

## `tree_felled`

`tree_type`, `tree_display_name`, `player_name`, `player_id`, `x`, `y`, `z`. Standing `TreeBase` prefabs and destroyed tree-growing `Plant` saplings qualify. `TreeLog` does not. Sapling events are delayed briefly so normal growth into a replacement `TreeBase` can be suppressed.

## `entity_count`

| Property | Type | Meaning |
|---|---|---|
| `category` | string | `active_mob`, `active_tamed_creature`, `loaded_piece`, `loaded_portal`, or `loaded_ship` |
| `entity_type` | string | Raw prefab identifier |
| `count` | integer | Count in the union of ready peers' near simulation areas |

Zero-value groups are omitted; an empty snapshot emits no JSON lines. These categories must not be interpreted as persistent world totals.
