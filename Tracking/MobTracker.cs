using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;
using ValheimTelemetry.Util;

namespace ValheimTelemetry.Tracking
{
    internal sealed class MobTracker
    {
        private readonly PluginConfig _config;
        private readonly ITelemetrySink _sink;
        private readonly RecentHitTracker _hits;
        private readonly BoundedEventCache _spawned = new BoundedEventCache(16384);
        private readonly BoundedEventCache _killed = new BoundedEventCache(16384);

        public MobTracker(PluginConfig config, ITelemetrySink sink, RecentHitTracker hits)
        {
            _config = config;
            _sink = sink;
            _hits = hits;
        }

        public void Spawned(ZDO zdo, PrefabInfo info)
        {
            if (!_config.MobSpawns || info == null || !info.IsMob || !_spawned.Add(zdo.m_uid))
            {
                return;
            }
            var telemetryEvent = new TelemetryEvent()
                .Add("mob_type", info.Type)
                .Add("mob_display_name", info.DisplayName);
            ZdoUtil.AddLevel(telemetryEvent, zdo, "mob_");
            telemetryEvent.Add("spawn_source", DetermineSpawnSource(zdo, info));
            ZdoUtil.AddPosition(telemetryEvent, zdo.GetPosition(), _config);
            telemetryEvent.Add("biome", PrefabUtil.Biome(zdo.GetPosition()));
            _sink.Emit("mob_spawned", telemetryEvent);
        }

        public void Killed(ZDO zdo, PrefabInfo info)
        {
            if (!_config.MobKills || info == null || !info.IsMob || !_killed.Add(zdo.m_uid))
            {
                return;
            }
            PlayerIdentity player = _hits.Resolve(zdo.m_uid, 0.75f);
            var telemetryEvent = new TelemetryEvent();
            PlayerUtil.Add(telemetryEvent, player, _config);
            telemetryEvent.Add("mob_type", info.Type).Add("mob_display_name", info.DisplayName);
            ZdoUtil.AddLevel(telemetryEvent, zdo, "mob_");
            ZdoUtil.AddPosition(telemetryEvent, zdo.GetPosition(), _config);
            telemetryEvent.Add("biome", PrefabUtil.Biome(zdo.GetPosition()));
            _sink.Emit("mob_killed", telemetryEvent);
        }

        private static string DetermineSpawnSource(ZDO zdo, PrefabInfo info)
        {
            if (zdo.GetBool(ZDOVars.s_eventCreature, false))
            {
                return "event";
            }
            if (info.Character != null && info.Character.m_boss)
            {
                return "boss";
            }
            if (zdo.GetInt(ZDOVars.s_maxInstances, 0) > 0)
            {
                return "summon";
            }
            if (zdo.GetBool(ZDOVars.s_tamed, false))
            {
                return "breeding";
            }
            try
            {
                foreach (ZDOID sourceId in ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Spawned))
                {
                    ZDO source = ZDOMan.instance.GetZDO(sourceId);
                    if (source != null && source.GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned) == zdo.m_uid)
                    {
                        return "CreatureSpawner";
                    }
                }
            }
            catch
            {
                // Source classification is optional; the spawn event remains valid.
            }
            return "unknown";
        }
    }
}
