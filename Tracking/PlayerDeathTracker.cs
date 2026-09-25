using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;
using ValheimTelemetry.Util;

namespace ValheimTelemetry.Tracking
{
    internal sealed class PlayerDeathTracker
    {
        private readonly PluginConfig _config;
        private readonly ITelemetrySink _sink;
        private readonly BoundedEventCache _deaths = new BoundedEventCache(4096);

        public PlayerDeathTracker(PluginConfig config, ITelemetrySink sink)
        {
            _config = config;
            _sink = sink;
        }

        public void Died(ZDOID characterId)
        {
            if (!_config.PlayerDeaths || characterId.IsNone()) return;
            ZDO zdo = ZDOMan.instance?.GetZDO(characterId);
            PrefabInfo info = PrefabUtil.Describe(zdo);
            if (zdo == null || info == null || !info.IsPlayer || !_deaths.Add(characterId)) return;
            var telemetryEvent = new TelemetryEvent();
            PlayerUtil.Add(telemetryEvent, PlayerUtil.FromCharacter(characterId), _config);
            telemetryEvent.Add("death_cause", null);
            ZdoUtil.AddPosition(telemetryEvent, zdo.GetPosition(), _config);
            telemetryEvent.Add("biome", PrefabUtil.Biome(zdo.GetPosition()));
            _sink.Emit("player_died", telemetryEvent);
        }
    }
}
