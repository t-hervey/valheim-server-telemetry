using System;
using System.Collections.Generic;
using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;
using ValheimTelemetry.Util;

namespace ValheimTelemetry.Tracking
{
    internal sealed class PlayerSessionTracker
    {
        private sealed class Session
        {
            public float StartedAt;
            public bool Emitted;
            public PlayerIdentity Identity;
            public UnityEngine.Vector3 Position;
        }

        private readonly PluginConfig _config;
        private readonly ITelemetrySink _sink;
        private readonly Dictionary<long, Session> _sessions = new Dictionary<long, Session>();

        public PlayerSessionTracker(PluginConfig config, ITelemetrySink sink)
        {
            _config = config;
            _sink = sink;
        }

        public void Connected(ZNetPeer peer)
        {
            if (!_config.PlayerSessions || peer == null || !peer.IsReady() || _sessions.ContainsKey(peer.m_uid)) return;
            if (_sessions.Count >= 32) _sessions.Clear();
            _sessions[peer.m_uid] = new Session { StartedAt = UnityEngine.Time.realtimeSinceStartup };
            Refresh(peer);
        }

        public void CharacterChanged(ZNetPeer peer)
        {
            if (!_config.PlayerSessions || peer == null || !peer.IsReady()) return;
            if (!_sessions.ContainsKey(peer.m_uid)) Connected(peer);
            Refresh(peer);
        }

        public void Tick()
        {
            if (!_config.PlayerSessions || ZNet.instance == null) return;
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer.IsReady())
                {
                    if (!_sessions.ContainsKey(peer.m_uid)) Connected(peer);
                    Refresh(peer);
                }
            }
        }

        public void Disconnected(ZNetPeer peer)
        {
            if (!_config.PlayerSessions || peer == null || !_sessions.TryGetValue(peer.m_uid, out Session session)) return;
            Refresh(peer);
            _sessions.Remove(peer.m_uid);
            if (!session.Emitted) return;
            float duration = Math.Max(0f, UnityEngine.Time.realtimeSinceStartup - session.StartedAt);
            var telemetryEvent = new TelemetryEvent();
            PlayerUtil.Add(telemetryEvent, session.Identity, _config);
            telemetryEvent.Add("session_duration_seconds", duration);
            ZdoUtil.AddPosition(telemetryEvent, session.Position, _config);
            _sink.Emit("player_disconnected", telemetryEvent);
        }

        private void Refresh(ZNetPeer peer)
        {
            if (!_sessions.TryGetValue(peer.m_uid, out Session session)) return;
            PlayerIdentity identity = PlayerUtil.FromPeer(peer);
            if (identity != null) session.Identity = identity;
            if (!peer.m_characterID.IsNone() && ZDOMan.instance != null)
            {
                ZDO character = ZDOMan.instance.GetZDO(peer.m_characterID);
                if (character != null)
                {
                    session.Position = character.GetPosition();
                    if (!session.Emitted && session.Identity != null && session.Identity.Id != 0L)
                    {
                        session.Emitted = true;
                        session.StartedAt = UnityEngine.Time.realtimeSinceStartup;
                        var telemetryEvent = new TelemetryEvent();
                        PlayerUtil.Add(telemetryEvent, session.Identity, _config);
                        ZdoUtil.AddPosition(telemetryEvent, session.Position, _config);
                        _sink.Emit("player_connected", telemetryEvent);
                    }
                }
            }
        }
    }
}
