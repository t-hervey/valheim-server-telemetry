using System.Collections.Generic;
using UnityEngine;
using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;
using ValheimTelemetry.Util;

namespace ValheimTelemetry.Tracking
{
    internal sealed class PortalTripTracker
    {
        private readonly PluginConfig _config;
        private readonly ITelemetrySink _sink;
        private readonly PortalTripDetector _detector = new PortalTripDetector();
        private readonly List<PortalTripDetector.Portal> _portals = new List<PortalTripDetector.Portal>();
        private float _nextSample;
        private float _nextPortalRefresh;

        public PortalTripTracker(PluginConfig config, ITelemetrySink sink)
        {
            _config = config;
            _sink = sink;
        }

        public void Tick(float now)
        {
            if (!_config.PortalTrips || ZNet.instance == null || ZDOMan.instance == null || now < _nextSample) return;
            _nextSample = now + 0.5f;
            if (now >= _nextPortalRefresh)
            {
                RefreshPortals();
                _nextPortalRefresh = now + 10f;
            }

            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (!peer.IsReady() || peer.m_characterID.IsNone()) continue;
                ZDO character = ZDOMan.instance.GetZDO(peer.m_characterID);
                if (character == null) continue;
                Vector3 position = character.GetPosition();
                PortalTripDetector.Trip trip = _detector.Observe(peer.m_uid, position.x, position.y, position.z, now, _portals);
                if (trip != null) Emit(peer, trip);
            }
        }

        public void Disconnected(ZNetPeer peer)
        {
            if (peer != null) _detector.Remove(peer.m_uid);
        }

        private void RefreshPortals()
        {
            _portals.Clear();
            List<ZDO> portals = ZDOMan.instance.GetPortalList();
            for (int i = 0; i < portals.Count; i++)
            {
                ZDO zdo = portals[i];
                PrefabInfo info = PrefabUtil.Describe(zdo);
                if (zdo == null || info == null || !info.IsPortal) continue;
                ZDOID target = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
                Vector3 position = zdo.GetPosition();
                _portals.Add(new PortalTripDetector.Portal
                {
                    Id = zdo.m_uid.ToString(),
                    TargetId = target.IsNone() ? null : target.ToString(),
                    Type = info.Type,
                    Tag = zdo.GetString(ZDOVars.s_tag, string.Empty),
                    X = position.x,
                    Y = position.y,
                    Z = position.z
                });
            }
        }

        private void Emit(ZNetPeer peer, PortalTripDetector.Trip trip)
        {
            var telemetryEvent = new TelemetryEvent();
            PlayerUtil.Add(telemetryEvent, PlayerUtil.FromPeer(peer), _config);
            AddPortal(telemetryEvent, "from_", trip.From);
            AddPortal(telemetryEvent, "to_", trip.To);
            telemetryEvent
                .Add("detection_method", "server_position_jump")
                .Add("confidence", "high_confidence");
            _sink.Emit("portal_travel", telemetryEvent);
        }

        private void AddPortal(TelemetryEvent telemetryEvent, string prefix, PortalTripDetector.Portal portal)
        {
            telemetryEvent
                .Add(prefix + "portal_id", portal.Id)
                .Add(prefix + "portal_type", portal.Type)
                .Add(prefix + "portal_tag", portal.Tag)
                .Add(prefix + "x", _config.IncludePosition ? (object)portal.X : null)
                .Add(prefix + "y", _config.IncludePosition ? (object)portal.Y : null)
                .Add(prefix + "z", _config.IncludePosition ? (object)portal.Z : null)
                .Add(prefix + "biome", PrefabUtil.Biome(new Vector3(portal.X, portal.Y, portal.Z)));
        }
    }
}
