using System.Collections.Generic;
using System.Diagnostics;
using BepInEx.Logging;
using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;
using ValheimTelemetry.Util;

namespace ValheimTelemetry.Tracking
{
    internal sealed class SnapshotCollector
    {
        private readonly PluginConfig _config;
        private readonly ITelemetrySink _sink;
        private readonly ManualLogSource _log;
        private readonly HashSet<ZDOID> _seen = new HashSet<ZDOID>();
        private readonly List<ZDO> _sector = new List<ZDO>(4096);

        public SnapshotCollector(PluginConfig config, ITelemetrySink sink, ManualLogSource log)
        {
            _config = config; _sink = sink; _log = log;
        }

        public void Collect()
        {
            Stopwatch timer = Stopwatch.StartNew();
            int peers = 0;
            int objects = 0;
            try
            {
                var activeMobs = new Dictionary<string, int>();
                var tamed = new Dictionary<string, int>();
                var pieces = new Dictionary<string, int>();
                var portals = new Dictionary<string, int>();
                var ships = new Dictionary<string, int>();
                _seen.Clear();

                if (ZNet.instance != null && ZDOMan.instance != null && ZoneSystem.instance != null)
                {
                    foreach (ZNetPeer peer in ZNet.instance.GetPeers())
                    {
                        if (!peer.IsReady()) continue;
                        peers++;
                        _sector.Clear();
                        var near = new SimulationDistance(peer.m_simulationDistance.NearSimulationDistance, 0, peer.m_simulationDistance.IsClassic);
                        ZDOMan.instance.FindSectorObjects(ZoneSystem.GetZone(peer.GetRefPos()), near, _sector);
                        foreach (ZDO zdo in _sector)
                        {
                            if (zdo == null || !_seen.Add(zdo.m_uid)) continue;
                            objects++;
                            PrefabInfo info = PrefabUtil.Describe(zdo);
                            if (info == null) continue;
                            if (info.IsMob)
                            {
                                if (zdo.GetBool(ZDOVars.s_tamed, false)) Increment(tamed, info.Type);
                                else Increment(activeMobs, info.Type);
                            }
                            if (zdo.GetLong(ZDOVars.s_creator, 0L) != 0L)
                            {
                                if (info.IsPortal) Increment(portals, info.Type);
                                else if (info.IsShip) Increment(ships, info.Type);
                                else if (info.IsPiece) Increment(pieces, info.Type);
                            }
                        }
                    }
                }

                if (_config.SnapshotActiveMobs) Emit("active_mob", activeMobs);
                if (_config.SnapshotTamedCreatures) Emit("active_tamed_creature", tamed);
                if (_config.SnapshotBuildingPieces) Emit("loaded_piece", pieces);
                if (_config.SnapshotPortals) Emit("loaded_portal", portals);
                if (_config.SnapshotShips) Emit("loaded_ship", ships);
            }
            finally
            {
                timer.Stop();
                if (_config.DebugLogging)
                {
                    _log.LogInfo("ValheimTelemetry snapshot completed safely in " + timer.ElapsedMilliseconds + " ms; ready_peers=" + peers + ", unique_active_zdos=" + objects + ".");
                }
            }
        }

        private void Emit(string category, Dictionary<string, int> counts)
        {
            foreach (var pair in counts)
            {
                _sink.Emit("entity_count", new TelemetryEvent().Add("category", category).Add("entity_type", pair.Key).Add("count", pair.Value));
            }
        }

        private static void Increment(Dictionary<string, int> counts, string type)
        {
            if (string.IsNullOrEmpty(type)) type = "unknown";
            counts.TryGetValue(type, out int count);
            counts[type] = count + 1;
        }
    }
}
