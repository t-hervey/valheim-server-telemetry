# Telemetry coverage and version history

This is the canonical inventory of implemented and considered telemetry. Update this file whenever an event is added, changed, deferred, or removed. “Introduced” is the first plugin version containing the event; later hardening is recorded in Notes.

## Implemented events and metrics

| Event or category | Status | Introduced | Quality | Notes |
|---|---|---:|---|---|
| `mob_spawned` | Implemented | 1.0.0 | EXACT event; source often INFERRED | v1.0.1 hardened client-created ZDO handling. |
| `mob_killed` | Implemented | 1.0.0 | HIGH_CONFIDENCE | v1.0.1 handles client destruction arriving before terminal health. Killer identity remains nullable by design. |
| `entity_count: active_mob` | Implemented | 1.0.0 | LOADED_ENTITIES_ONLY | Counts ready peers' active areas, not the persistent world. |
| `portal_built` / `portal_destroyed` | Implemented | 1.0.0 | EXACT | Destruction actor is usually unavailable. |
| `portal_tag_changed` | Implemented | 1.1.0 | HIGH_CONFIDENCE | Exact replicated tag transition; actor is identified only when tag-author platform ID matches a connected player. |
| `portal_travel` | Implemented | 1.2.0 | HIGH_CONFIDENCE | Inferred only when a player moves abruptly from a portal to its actual connected ZDO target. Not a client-side trigger event. |
| `entity_count: loaded_portal` | Implemented | 1.0.0 | LOADED_ENTITIES_ONLY | Not a world total. |
| `piece_built` / `piece_destroyed` | Implemented | 1.0.0 | EXACT | v1.0.2 added connected-character lookup for build-time player names. |
| `entity_count: loaded_piece` | Implemented | 1.0.0 | LOADED_ENTITIES_ONLY | Not a world total. |
| `ship_built` / `ship_destroyed` | Implemented | 1.0.0 | EXACT | Uses actual runtime prefab classification. |
| `entity_count: loaded_ship` | Implemented | 1.0.0 | LOADED_ENTITIES_ONLY | Not a world total. |
| `creature_tamed` | Implemented | 1.0.0 | HIGH_CONFIDENCE | Existing false-to-true tame transition only; bred tame births are excluded. |
| `tamed_creature_died` | Implemented | 1.1.0 | HIGH_CONFIDENCE | Uses the same conservative death evidence as `mob_killed`. |
| `entity_count: active_tamed_creature` | Implemented | 1.0.0 | LOADED_ENTITIES_ONLY | Not a world total. |
| `tree_felled` | Implemented | 1.0.0 | HIGH_CONFIDENCE | v1.0.1 hardened client-owned destruction; v1.0.2 added destroyed tree saplings and natural-growth suppression. Fallen-log chopping is excluded. |
| `player_connected` / `player_disconnected` | Implemented | 1.1.0 | EXACT | Gameplay-ready peer session; disconnect includes duration and last observed position. |
| `player_died` | Implemented | 1.1.0 | EXACT occurrence | Death cause is unavailable server-only and remains null. |
| `boss_killed` | Implemented | 1.1.0 | HIGH_CONFIDENCE | Runtime `Character.m_boss` classification; killer identity has the same limitations as mob kills. |
| `world_key_changed` | Implemented | 1.1.0 | EXACT | Added, updated, and removed global keys after startup grace; covers boss/progression state changes without emitting the startup baseline. |
| Static map export | Implemented | 1.2.0 | HIGH_CONFIDENCE discovery scope | Outputs terrain/discovery PNGs and metadata. Discovery combines post-install server-observed player positions with vanilla cartography-table data. Disabled by default. |

## Considered but not implemented

| Candidate | Status | Expected quality | Reason / next condition |
|---|---|---|---|
| Player sleep started/ended | Deferred | HIGH_CONFIDENCE | Interesting for session/world activity, but lower value than the 1.1.0 group. Inspect replicated bed/sleep state before adding. |
| Resource object destroyed | Deferred | HIGH_CONFIDENCE | Rocks, ore deposits, stumps, and similar destructibles could be classified, but event volume and category semantics need design first. |
| Ship boarded/left | Deferred | INFERRED | Vanilla attach/detach state may be client-owned; needs server-path validation and deduplication. |
| Player biome presence/transition | Deferred | LOADED_ENTITIES_ONLY | Periodic character-ZDO location can support presence, but an exact transition may be missed between samples. |
| Building repair | Deferred | INFERRED | Repair intent/action is primarily client-side; replicated health increase may be caused by other mechanics. |
| Structure damage | Deferred | HIGH_CONFIDENCE occurrence | Technically observable through health changes, but high volume and attacker attribution is usually unavailable. |
| Chat messages / commands | Deferred | EXACT | Server-visible but privacy-sensitive and outside the current gameplay telemetry scope. |
| Crafting and upgrades | Unavailable server-only | UNAVAILABLE_SERVER_ONLY | Confirmed in 1.0.12: `InventoryGui.DoCrafting` mutates the local player's inventory and profile directly and sends no craft RPC to the dedicated server. |
| Item pickups and inventory changes | Unavailable server-only | UNAVAILABLE_SERVER_ONLY | No complete passive server-side action stream. |
| Skill gains | Unavailable server-only | UNAVAILABLE_SERVER_ONLY | Player skill progression is client-owned. |
| Ordinary attacks/hits | Unavailable as complete stream | UNAVAILABLE_SERVER_ONLY | Only routed damage that traverses the server is visible; client-owned target handling bypasses it. |
| Guaranteed final-hit killer attribution | Unavailable server-only | UNAVAILABLE_SERVER_ONLY | The dedicated server often never sees the owning client's final damage call. Nearest/sole-player guessing is intentionally forbidden. |
| Persistent world-total entity counts | Not implemented | UNAVAILABLE_SERVER_ONLY | The supported passive runtime view covers loaded/active peer sectors, not a cheap authoritative classified world total. |

## Release history

- **1.0.0** — initial passive lifecycle telemetry, snapshots, configuration, JSON sink, and startup baseline suppression.
- **1.0.1** — hardened client-owned mob deaths and standing-tree felling against destroy-before-health replication ordering.
- **1.0.2** — added tree-sapling destruction with natural-growth suppression and connected-player name resolution for built pieces.
- **1.1.0** — added gameplay sessions, player deaths, tamed-creature deaths, boss kills, world-key changes, and portal tag changes.
- **1.1.1** — added the automated unit-test project and deterministic cache/sink test seams; telemetry schema and event coverage are unchanged.
- **1.1.2** — added reproducible Stryker mutation testing and strengthened boundary tests; telemetry schema and event coverage are unchanged.
- **1.2.0** — added conservative server-side portal-travel inference, stable portal ZDO IDs for graphing, and optional static map/discovery exports.
