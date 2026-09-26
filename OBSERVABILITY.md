# Valheim 1.0 server observability

This analysis is based on the installed `assembly_valheim.dll` from Valheim `l-1.0.12`, not historical mod examples. `EXACT` means the observed server-side lifecycle fact is exact when seen; it does not promise that client-owned simulation can never cause a missed event. Field-level limitations are called out separately.

| Event/metric | Hook or data source | Server-visible | Quality |
|---|---|---:|---|
| `mob_spawned` | new `ZDOMan.CreateNewZDO`, classified by prefab `Character` and not `Player` | Yes | **EXACT** new-ZDO detection; source often **INFERRED** |
| `mob_killed` | zero/reduced health plus destruction; ordinary persistent Character destruction with explicit despawn classes excluded | Yes | **HIGH_CONFIDENCE** |
| `boss_killed` | same death evidence, classified by runtime `Character.m_boss` | Yes | **HIGH_CONFIDENCE** |
| `tamed_creature_died` | same death evidence plus persistent `tamed` state | Yes | **HIGH_CONFIDENCE** |
| `active_mob` | union of ready peers' near sectors via `FindSectorObjects` | Yes | **LOADED_ENTITIES_ONLY** |
| `player_connected` / `player_disconnected` | `ZNet.RPC_PeerInfo`, `RPC_CharacterID`, and `Disconnect` | Yes | **EXACT** gameplay-ready session |
| `player_died` | routed vanilla `OnDeath` RPC with replicated `dead` state fallback | Yes | **EXACT** occurrence; cause unavailable |
| `portal_built` | new player-created ZDO whose prefab has `TeleportWorld` | Yes | **EXACT** |
| `portal_destroyed` | `ZDOMan.HandleDestroyedZDO` for that portal ZDO | Yes | **EXACT** |
| `portal_tag_changed` | replicated portal ZDO `tag` transition and `tagauthor` | Yes | **HIGH_CONFIDENCE** |
| `portal_travel` | connected portal ZDO graph plus replicated player-position jump | Inferred | **HIGH_CONFIDENCE** |
| `loaded_portal` | ready peers' near sectors | Yes | **LOADED_ENTITIES_ONLY** |
| `piece_built` | new player-created ZDO whose prefab has `Piece`, excluding portal/ship | Yes | **EXACT** |
| `piece_destroyed` | `HandleDestroyedZDO` for that piece ZDO | Yes | **EXACT** |
| `loaded_piece` | ready peers' near sectors | Yes | **LOADED_ENTITIES_ONLY** |
| `ship_built` | new player-created ZDO whose prefab has `Ship` | Yes | **EXACT** |
| `ship_destroyed` | `HandleDestroyedZDO` for that ship ZDO | Yes | **EXACT** |
| `loaded_ship` | ready peers' near sectors | Yes | **LOADED_ENTITIES_ONLY** |
| `creature_tamed` | existing Character ZDO `tamed` false-to-true transition | Usually | **HIGH_CONFIDENCE**; method is **INFERRED** |
| `active_tamed_creature` | tamed Character ZDOs in ready peers' near sectors | Yes | **LOADED_ENTITIES_ONLY** |
| `tree_felled` | destruction of a standing `TreeBase`, or a tree-growing `Plant` without a nearby grown replacement; `TreeLog` is excluded | Yes | **HIGH_CONFIDENCE** |
| `world_key_changed` | `ZoneSystem.GlobalKeyAdd` / `GlobalKeyRemove` after startup grace | Yes | **EXACT** |
| persistent world totals | complete chunk store, safely and cheaply classified | Not as a supported runtime metric | **UNAVAILABLE_SERVER_ONLY** |
| discovered-map export | replicated player positions plus cartography-table `s_data` | Partial | **HIGH_CONFIDENCE** for included sources; private historical client exploration unavailable |

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

Health zero is the strongest signal, but the owning client can destroy a dead creature before its final health revision reaches the server. The tracker therefore remembers replicated health decreases and treats destruction of an ordinary persistent creature as death. Creatures marked for daytime despawn, event cleanup, or limited-instance summon cleanup require zero-health or recent-damage evidence. This closes the observed client-ordering gap while avoiding known vanilla despawn paths. Admin/mod deletion of an ordinary creature can still be a false positive. Prefab/type, level, position, and biome are server data; attribution is independently nullable.

Boss and tamed-creature deaths reuse that one deduplicated death decision, then classify the last readable ZDO using runtime boss prefab metadata and persistent tame state. A qualifying death can intentionally emit both the generic `mob_killed` event and one specialized event. They share the generic death false-positive/false-negative scenarios and nullable killer attribution.

Player death differs: `Player.OnDeath` sets persistent `dead` and invokes the vanilla `OnDeath` RPC to everybody. The server routes that RPC even when the client owns the player, while replicated `dead` false-to-true is a fallback. A bounded ZDOID cache prevents the two paths from double counting. The detailed final `Player.m_lastHit` stays on the client, so server-only death cause is unavailable rather than guessed.

### Sessions and progression

The server's `RPC_PeerInfo` establishes an accepted/ready peer and `RPC_CharacterID` binds its live character ZDO. `player_connected` waits for that character identity, excluding preliminary sockets that never enter gameplay. `ZNet.Disconnect` is observed before vanilla removes the peer, allowing exact session duration and a last-known position. A process crash cannot emit a graceful disconnect event.

`ZoneSystem.GlobalKeyAdd` and `GlobalKeyRemove` are the authoritative mutations of world progression keys. Identical reassignments are suppressed, and the plugin's startup grace prevents loaded baseline keys from looking new. Runtime admin/mod key operations remain legitimate key-change telemetry even when they were not earned through gameplay.

### Pieces, portals, and ships

Runtime components classify the actual prefab, avoiding hard-coded name lists. `creator != 0` excludes world-generated Piece components. Portal/ship objects are removed from generic piece events to avoid duplicate categories. A mod/admin-created ZDO that supplies a player creator could look like a normal build. ZDO destruction is exact, but hammer remover identity normally remains unknown.

Portal renaming changes persistent `tag`; repeated identical values and the initial tag written during recent portal creation are suppressed. Vanilla also persists `tagauthor` as a platform user identifier. The plugin only assigns an actor when that identifier exactly matches a currently connected player and can then resolve the character profile ID. A tag change still emits with null identity when the author is unavailable.

`TeleportWorldTrigger.OnTriggerEnter` explicitly acts only on `Player.m_localPlayer`, and `TeleportWorld.Teleport` calls `Player.TeleportTo` on that client. There is no dedicated portal-use RPC for the server to observe. The server does receive the client-owned player ZDO's resulting position. `portal_travel` therefore requires all of the following: a player was within 7.5 metres of a connected source portal, the next sampled server position arrived within five seconds and moved at least 12 metres, and the new position is within 7.5 metres of the source portal's actual connected ZDO target. Samples run twice per second; the persistent portal graph is refreshed every ten seconds.

This is **HIGH_CONFIDENCE**, not exact. A non-portal teleport that happens to begin and end beside the two connected portals can be a false positive. Trips between connected portals less than 12 metres apart, trips whose position replication is delayed more than five seconds, very short visits that occur wholly between samples, and trips during the first portal-cache refresh after a new connection can be missed. A five-second per-player cooldown prevents repeated or immediate reverse-trip duplicates. Player state is discarded on disconnect and bounded to 64 entries.

### Taming

`Tameable.TamingUpdate` is owner-only, but completion ultimately changes persistent `ZDOVars.s_tamed`. Only an existing ZDO's false-to-true transition emits `creature_tamed`. A newly created already-tamed ZDO is suppressed, so bred offspring are not falsely reported as having undergone player taming. Vanilla state does not persist the responsible player, so identity is null. Direct admin/mod state changes are indistinguishable and are a possible false positive.

### Trees

A standing tree prefab has `TreeBase`; lethal damage spawns the fallen log/stub and destroys the standing-tree ZDO. Because the client can send that destruction before the terminal health revision, destruction of a `TreeBase` is the event signal. A sapling uses `Plant` plus `Destructible`; tree saplings are identified without a name list by a grown prefab containing `TreeBase`. Natural growth also destroys the sapling, so the event is delayed three seconds and suppressed when a newly created standing tree appears at the same position. The resulting log has `TreeLog`, so later log chopping cannot generate `tree_felled`. Admin/mod deletion of a standing tree, or an unhealthy tree sapling that self-destructs without growing, can look like felling.

### Snapshots

The dedicated server should not use its own origin-based runtime GameObject set as a world count. The collector reads every ready peer's near `SimulationDistance`, unions ZDOs from those sectors, and de-duplicates overlapping areas by `ZDOID`. Collection runs every five minutes by default and performs no frame-by-frame world scan. Counts may change as players move or disconnect even if nothing is created/destroyed.

### Map export

`Minimap.UpdateExplore` and `PlayerProfile.m_mapData` are client-owned, so the dedicated server cannot reconstruct every player's historical private fog-of-war. A vanilla cartography table is different: `MapTable.RPC_MapData` persists the compressed shared exploration array in the table ZDO's `ZDOVars.s_data`, which is server-visible. When enabled, the exporter merges those arrays with 100-metre discovery circles around server-observed connected-player positions and persists that union locally across restarts.

Terrain colors come deterministically from the installed `WorldGenerator.GetBiome` over the playable `-10500..10500` coordinate range. Generation is divided into batches of 1,024 pixels per frame, then PNG and metadata writes occur at most once per configured interval. The output is a visualization rather than a byte-for-byte capture of the client's shaded minimap. It does not include private exploration performed before plugin installation unless a player uploaded it to a cartography table.

### Crafting

Valheim 1.0.12 completes crafting in `InventoryGui.DoCrafting` on `Player.m_localPlayer`. That method removes resources, adds or upgrades the item in the local `Inventory`, writes crafter identity, updates the local profile statistics, and does not invoke a vanilla RPC that describes the craft. The dedicated server can later receive player-save/inventory state, but a delta cannot distinguish crafting from pickup, transfer, spawning, restoration, or other inventory changes. Exact or high-confidence passive `item_crafted` telemetry is therefore **UNAVAILABLE_SERVER_ONLY**. The plugin will not infer it from ambiguous inventory changes; reliable support would require a client mod or a future authoritative vanilla server event.

## Deduplication and failure modes

- Created, killed, built, destroyed, tamed, felled, and player-death identities use bounded FIFO-backed `ZDOID` sets (4,096–16,384 entries).
- Pending creations and recent-hit data are capped and expire.
- Portal-trip player state is capped at 64 peers; its portal snapshot is replaced rather than accumulated.
- Zone load/unload, GameObject recreation, network ownership transfer, and repeated synchronization do not create a new ZDO and therefore do not emit build/spawn events.
- Health/tame transitions compare old and new values; repeated identical updates do not emit again.
- All Harmony callbacks catch exceptions. They are observers only and never return false or modify arguments/state.

Known missing events are lifecycle changes during startup grace, explicitly despawning creatures whose damage state never reaches the server, and events in code paths that bypass vanilla ZDO lifecycle. Known possible false positives are external mods/admin operations that deliberately create/destroy or mutate otherwise vanilla-classified ZDOs, plus the narrow recent-hit attribution race described above.
