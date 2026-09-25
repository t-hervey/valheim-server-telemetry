using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;
using ValheimTelemetry.Util;

namespace ValheimTelemetry.Tracking
{
    internal sealed class PortalTracker
    {
        private readonly PluginConfig _config;
        private readonly ITelemetrySink _sink;
        private readonly RecentHitTracker _hits;
        private readonly BoundedEventCache _built = new BoundedEventCache(4096);
        private readonly BoundedEventCache _destroyed = new BoundedEventCache(4096);

        public PortalTracker(PluginConfig config, ITelemetrySink sink, RecentHitTracker hits)
        {
            _config = config; _sink = sink; _hits = hits;
        }

        public void Built(ZDO zdo, PrefabInfo info)
        {
            if (!_config.Portals || !Qualifies(zdo, info) || !_built.Add(zdo.m_uid)) return;
            Emit("portal_built", zdo, info, PlayerUtil.FromCreator(zdo.GetLong(ZDOVars.s_creator, 0L)));
        }

        public void Destroyed(ZDO zdo, PrefabInfo info)
        {
            if (!_config.Portals || !Qualifies(zdo, info) || !_destroyed.Add(zdo.m_uid)) return;
            Emit("portal_destroyed", zdo, info, _hits.Resolve(zdo.m_uid, 0.75f));
        }

        public void TagChanged(ZDO zdo, PrefabInfo info, string oldTag)
        {
            if (!_config.PortalTagChanges || zdo == null || info == null || !info.IsPortal) return;
            string newTag = zdo.GetString(ZDOVars.s_tag, string.Empty);
            if (string.Equals(oldTag ?? string.Empty, newTag, System.StringComparison.Ordinal)) return;
            string author = zdo.GetString(ZDOVars.s_tagauthor, null);
            var telemetryEvent = new TelemetryEvent()
                .Add("portal_type", info.Type)
                .Add("old_portal_tag", oldTag ?? string.Empty)
                .Add("portal_tag", newTag);
            PlayerUtil.Add(telemetryEvent, PlayerUtil.FromPlatformAuthor(author), _config);
            ZdoUtil.AddPosition(telemetryEvent, zdo.GetPosition(), _config);
            _sink.Emit("portal_tag_changed", telemetryEvent);
        }

        private static bool Qualifies(ZDO zdo, PrefabInfo info) => info != null && info.IsPortal && zdo.GetLong(ZDOVars.s_creator, 0L) != 0L;

        private void Emit(string name, ZDO zdo, PrefabInfo info, PlayerIdentity player)
        {
            var telemetryEvent = new TelemetryEvent()
                .Add("portal_type", info.Type)
                .Add("portal_tag", zdo.GetString(ZDOVars.s_tag, string.Empty));
            PlayerUtil.Add(telemetryEvent, player, _config);
            ZdoUtil.AddPosition(telemetryEvent, zdo.GetPosition(), _config);
            _sink.Emit(name, telemetryEvent);
        }
    }
}
