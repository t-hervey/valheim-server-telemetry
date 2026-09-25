# Valheim 1.0 server observability

This analysis is based on the installed `assembly_valheim.dll` from Valheim `l-1.0.12`, not historical mod examples. `EXACT` means the observed server-side lifecycle fact is exact when seen; it does not promise that client-owned simulation can never cause a missed event. Field-level limitations are called out separately.

| Event/metric | Hook or data source | Server-visible | Quality |
|---|---|---:|---|
| `mob_spawned` | new `ZDOMan.CreateNewZDO`, classified by prefab `Character` and not `Player` | Yes | **EXACT** new-ZDO detection; source often **INFERRED** |
| `mob_killed` | `ZDOVars.s_health` crosses to zero in `ZDO.Deserialize`/`ZDO.Set`; destruction corroboration | Usually | **HIGH_CONFIDENCE** |
| `active_mob` | union of ready peers' near sectors via `FindSectorObjects` | Yes | **LOADED_ENTITIES_ONLY** |
| `portal_built` | new player-created ZDO whose prefab has `TeleportWorld` | Yes | **EXACT** |
| `portal_destroyed` | `ZDOMan.HandleDestroyedZDO` for that portal ZDO | Yes | **EXACT** |
| `loaded_portal` | ready peers' near sectors | Yes | **LOADED_ENTITIES_ONLY** |
| `piece_built` | new player-created ZDO whose prefab has `Piece`, excluding portal/ship | Yes | **EXACT** |
| `piece_destroyed` | `HandleDestroyedZDO` for that piece ZDO | Yes | **EXACT** |
| `loaded_piece` | ready peers' near sectors | Yes | **LOADED_ENTITIES_ONLY** |
| `ship_built` | new player-created ZDO whose prefab has `Ship` | Yes | **EXACT** |
| `ship_destroyed` | `HandleDestroyedZDO` for that ship ZDO | Yes | **EXACT** |
| `loaded_ship` | ready peers' near sectors | Yes | **LOADED_ENTITIES_ONLY** |
| `creature_tamed` | existing Character ZDO `tamed` false-to-true transition | Usually | **HIGH_CONFIDENCE**; method is **INFERRED** |
| `active_tamed_creature` | tamed Character ZDOs in ready peers' near sectors | Yes | **LOADED_ENTITIES_ONLY** |
| `tree_felled` | standing `TreeBase` ZDO health reaches zero; `TreeLog` is excluded | Usually | **HIGH_CONFIDENCE** |
| persistent world totals | complete chunk store, safely and cheaply classified | Not as a supported runtime metric | **UNAVAILABLE_SERVER_ONLY** |

## Why ZDO lifecycle is used

`ZNetView.Awake` calls `ZDOMan.CreateNewZDO` only for a new network entity. Reconstructing an existing object after a zone load uses the already assigned `ZNetView.m_initZDO`; it does not create a new ZDO. On the server, receiving a genuinely unknown client-created identity also calls the private `CreateNewZDO(ZDOID, Vector3, int)` before deserialization. This makes new ZDO creation the strongest passive signal for builds and spawns without counting runtime GameObject reconstruction.

Disk world loading uses the chunk/load path, not the patched network `ZDO.Deserialize` path. In addition, all lifecycle callbacks remain disarmed until server singletons are ready plus a 20-second grace. Existing objects can participate in snapshots but cannot emit startup lifecycle events.

`ZDOMan.HandleDestroyedZDO` invokes vanilla destroy bookkeeping while the full ZDO is still readable. The plugin observes it in a prefix but never changes the ID, owner, persistence fields, or return flow.

## Ownership and attribution

`Character.RPC_Damage` immediately returns on a non-owner. `Character.ApplyDamage`, `Character.OnDeath`, `Tameable.TamingUpdate`, and most ordinary creature simulation therefore often execute on an owning client rather than the dedicated server. Patching those methods alone would silently miss events.

The server does observe ZDO replication and many routed vanilla RPCs. The plugin copies (without advancing the original package) an incoming `ZRoutedRpc.RPC_RoutedRPC` payload when its method is `RPC_Damage`. It resolves the attacker only if that ZDO is a `Player`, using the player ZDO and current `ZNetPeer` identity. The association expires after 0.75 seconds. No attacker means null identity; nearest-player guessing is never used.

Valheim's burn/poison status-effect ticks construct damage without an attacker. Those ticks are not attributed. The short recent-hit window still leaves a narrow edge case where a DOT/environmental final tick immediately follows a direct hit; manual testing should assess whether the desired policy is to make attribution even more conservative.

Player-built objects persist an exact creator ID in `ZDOVars.s_creator`, so build-time player ID is reliable even after disconnect. A current peer is required to recover the name. Destruction/removal generally does not persist an actor; player identity is emitted only if a recent direct damage RPC provides it.

## Event-specific evidence and gaps

### Spawns

The event is an exact newly created Character ZDO. `eventCreature`, boss prefab metadata, `MaxInstances`, initial tamed state, and a `CreatureSpawner` `Spawned` connection allow source classification. Ordinary `SpawnSystem` execution depends on `Player.m_localPlayer` and is not authoritative on the dedicated server; it has no persisted source marker, so normal spawns are `unknown`. A created-then-immediately-destroyed spawn can still be reported because it was genuinely created.

### Deaths

Health zero is stronger than mere ZDO destruction: cleanup/despawn destruction alone is not called a kill. A death can be missed if the object is destroyed before the server receives zero health. When seen, prefab/type, level, position, and biome are server data; attribution is independently nullable.

### Pieces, portals, and ships

Runtime components classify the actual prefab, avoiding hard-coded name lists. `creator != 0` excludes world-generated Piece components. Portal/ship objects are removed from generic piece events to avoid duplicate categories. A mod/admin-created ZDO that supplies a player creator could look like a normal build. ZDO destruction is exact, but hammer remover identity normally remains unknown.

### Taming

`Tameable.TamingUpdate` is owner-only, but completion ultimately changes persistent `ZDOVars.s_tamed`. Only an existing ZDO's false-to-true transition emits `creature_tamed`. A newly created already-tamed ZDO is suppressed, so bred offspring are not falsely reported as having undergone player taming. Vanilla state does not persist the responsible player, so identity is null. Direct admin/mod state changes are indistinguishable and are a possible false positive.

### Trees

A standing tree prefab has `TreeBase`; lethal damage sets its health to zero, spawns the fallen log/stub, and destroys the standing-tree ZDO. The resulting log has `TreeLog`, so later log chopping cannot generate `tree_felled`. A rapid network destroy without the health transition can be missed rather than guessed.

### Snapshots

The dedicated server should not use its own origin-based runtime GameObject set as a world count. The collector reads every ready peer's near `SimulationDistance`, unions ZDOs from those sectors, and de-duplicates overlapping areas by `ZDOID`. Collection runs every five minutes by default and performs no frame-by-frame world scan. Counts may change as players move or disconnect even if nothing is created/destroyed.

## Deduplication and failure modes

- Created, killed, built, destroyed, tamed, and felled identities use bounded FIFO-backed `ZDOID` sets (4,096–16,384 entries).
- Pending creations and recent-hit data are capped and expire.
- Zone load/unload, GameObject recreation, network ownership transfer, and repeated synchronization do not create a new ZDO and therefore do not emit build/spawn events.
- Health/tame transitions compare old and new values; repeated identical updates do not emit again.
- All Harmony callbacks catch exceptions. They are observers only and never return false or modify arguments/state.

Known missing events are decisive client-side state never replicated before destruction, lifecycle changes during startup grace, and events in code paths that bypass vanilla ZDO lifecycle. Known possible false positives are external mods/admin operations that deliberately create/destroy or mutate otherwise vanilla-classified ZDOs, plus the narrow recent-hit attribution race described above.
