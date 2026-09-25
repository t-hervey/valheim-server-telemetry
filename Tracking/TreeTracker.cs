using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;
using ValheimTelemetry.Util;

namespace ValheimTelemetry.Tracking
{
    internal sealed class TreeTracker
    {
        private readonly PluginConfig _config;
        private readonly ITelemetrySink _sink;
        private readonly RecentHitTracker _hits;
        private readonly BoundedEventCache _felled = new BoundedEventCache(8192);

        public TreeTracker(PluginConfig config, ITelemetrySink sink, RecentHitTracker hits)
        {
            _config = config; _sink = sink; _hits = hits;
        }

        public void Felled(ZDO zdo, PrefabInfo info)
        {
            if (!_config.Trees || info == null || !info.IsTreeBase || !_felled.Add(zdo.m_uid)) return;
            var telemetryEvent = new TelemetryEvent()
                .Add("tree_type", info.Type)
                .Add("tree_display_name", info.DisplayName);
            PlayerUtil.Add(telemetryEvent, _hits.Resolve(zdo.m_uid, 0.75f), _config);
            ZdoUtil.AddPosition(telemetryEvent, zdo.GetPosition(), _config);
            _sink.Emit("tree_felled", telemetryEvent);
        }
    }
}
