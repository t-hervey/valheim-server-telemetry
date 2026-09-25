using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;
using ValheimTelemetry.Util;

namespace ValheimTelemetry.Tracking
{
    internal sealed class PieceTracker
    {
        private readonly PluginConfig _config;
        private readonly ITelemetrySink _sink;
        private readonly RecentHitTracker _hits;
        private readonly BoundedEventCache _built = new BoundedEventCache(16384);
        private readonly BoundedEventCache _destroyed = new BoundedEventCache(16384);

        public PieceTracker(PluginConfig config, ITelemetrySink sink, RecentHitTracker hits)
        {
            _config = config;
            _sink = sink;
            _hits = hits;
        }

        public void Built(ZDO zdo, PrefabInfo info)
        {
            if (!_config.BuildingPieces || !Qualifies(zdo, info) || !_built.Add(zdo.m_uid)) return;
            Emit("piece_built", zdo, info, PlayerUtil.FromCreator(zdo.GetLong(ZDOVars.s_creator, 0L)));
        }

        public void Destroyed(ZDO zdo, PrefabInfo info)
        {
            if (!_config.BuildingPieces || !Qualifies(zdo, info) || !_destroyed.Add(zdo.m_uid)) return;
            Emit("piece_destroyed", zdo, info, _hits.Resolve(zdo.m_uid, 0.75f));
        }

        private static bool Qualifies(ZDO zdo, PrefabInfo info)
        {
            return info != null && info.IsPiece && !info.IsPortal && !info.IsShip && zdo.GetLong(ZDOVars.s_creator, 0L) != 0L;
        }

        private void Emit(string name, ZDO zdo, PrefabInfo info, PlayerIdentity player)
        {
            var telemetryEvent = new TelemetryEvent()
                .Add("piece_type", info.Type)
                .Add("piece_display_name", info.DisplayName);
            PlayerUtil.Add(telemetryEvent, player, _config);
            ZdoUtil.AddPosition(telemetryEvent, zdo.GetPosition(), _config);
            _sink.Emit(name, telemetryEvent);
        }
    }
}
