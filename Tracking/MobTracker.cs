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
            if (info == null || !info.IsMob || (!_config.MobKills && !_config.TamedCreatureDeaths && !_config.BossKills) || !_killed.Add(zdo.m_uid))
            {
                return;
            }
            PlayerIdentity player = _hits.Resolve(zdo.m_uid, 0.75f);
            if (_config.MobKills)
            {
                var telemetryEvent = new TelemetryEvent();
                PlayerUtil.Add(telemetryEvent, player, _config);
                telemetryEvent.Add("mob_type", info.Type).Add("mob_display_name", info.DisplayName);
                ZdoUtil.AddLevel(telemetryEvent, zdo, "mob_");
                ZdoUtil.AddPosition(telemetryEvent, zdo.GetPosition(), _config);
                telemetryEvent.Add("biome", PrefabUtil.Biome(zdo.GetPosition()));
                _sink.Emit("mob_killed", telemetryEvent);
            }

            if (_config.TamedCreatureDeaths && zdo.GetBool(ZDOVars.s_tamed, false))
            {
                var tamedEvent = new TelemetryEvent()
                    .Add("creature_type", info.Type)
                    .Add("creature_display_name", info.DisplayName);
                ZdoUtil.AddLevel(tamedEvent, zdo, "creature_");
                PlayerUtil.Add(tamedEvent, player, _config);
                ZdoUtil.AddPosition(tamedEvent, zdo.GetPosition(), _config);
                tamedEvent.Add("biome", PrefabUtil.Biome(zdo.GetPosition()));
                _sink.Emit("tamed_creature_died", tamedEvent);
            }

            if (_config.BossKills && info.Character != null && info.Character.m_boss)
            {
                var bossEvent = new TelemetryEvent()
                    .Add("boss_type", info.Type)
                    .Add("boss_display_name", info.DisplayName);
                ZdoUtil.AddLevel(bossEvent, zdo, "boss_");
                PlayerUtil.Add(bossEvent, player, _config);
                ZdoUtil.AddPosition(bossEvent, zdo.GetPosition(), _config);
                bossEvent.Add("biome", PrefabUtil.Biome(zdo.GetPosition()));
                _sink.Emit("boss_killed", bossEvent);
            }
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
