using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;
using ValheimTelemetry.Util;

namespace ValheimTelemetry.Tracking
{
    internal sealed class TameTracker
    {
        private readonly PluginConfig _config;
        private readonly ITelemetrySink _sink;
        private readonly BoundedEventCache _tamed = new BoundedEventCache(8192);

        public TameTracker(PluginConfig config, ITelemetrySink sink)
        {
            _config = config; _sink = sink;
        }

        public void Tamed(ZDO zdo, PrefabInfo info)
        {
            if (!_config.Taming || info == null || !info.IsMob || !_tamed.Add(zdo.m_uid)) return;
            var telemetryEvent = new TelemetryEvent()
                .Add("creature_type", info.Type)
                .Add("creature_display_name", info.DisplayName);
            ZdoUtil.AddLevel(telemetryEvent, zdo, "creature_");
            telemetryEvent.Add("tame_method", "player_tamed");
            PlayerUtil.Add(telemetryEvent, null, _config);
            ZdoUtil.AddPosition(telemetryEvent, zdo.GetPosition(), _config);
            _sink.Emit("creature_tamed", telemetryEvent);
        }
    }
}
