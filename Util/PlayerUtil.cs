using System.Globalization;
using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;

namespace ValheimTelemetry.Util
{
    internal sealed class PlayerIdentity
    {
        public string Name;
        public long Id;
    }

    internal static class PlayerUtil
    {
        public static PlayerIdentity FromCharacter(ZDOID characterId)
        {
            if (characterId.IsNone())
            {
                return null;
            }

            PlayerIdentity peer = FromPeerCharacter(characterId);
            if (peer != null)
            {
                return peer;
            }

            ZDO zdo = ZDOMan.instance?.GetZDO(characterId);
            PrefabInfo info = PrefabUtil.Describe(zdo);
            if (zdo == null || info == null || !info.IsPlayer)
            {
                return null;
            }

            long id = zdo.GetLong(ZDOVars.s_playerID, 0L);
            string name = zdo.GetString(ZDOVars.s_playerName, null);
            return new PlayerIdentity
            {
                Id = id,
                Name = name
            };
        }

        public static PlayerIdentity FromCreator(long creator)
        {
            if (creator == 0L || ZNet.instance == null)
            {
                return null;
            }
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer.IsReady() && peer.m_playerID == creator)
                {
                    return new PlayerIdentity { Id = peer.m_playerID, Name = peer.m_playerName };
                }
            }
            return new PlayerIdentity { Id = creator, Name = null };
        }

        public static TelemetryEvent Add(TelemetryEvent telemetryEvent, PlayerIdentity identity, PluginConfig config)
        {
            telemetryEvent.Add("player_name", config.IncludePlayerName && !string.IsNullOrEmpty(identity?.Name) ? identity.Name : null);
            telemetryEvent.Add("player_id", config.IncludePlayerId && identity != null && identity.Id != 0L ? identity.Id.ToString(CultureInfo.InvariantCulture) : null);
            return telemetryEvent;
        }

        private static PlayerIdentity FromPeerCharacter(ZDOID characterId)
        {
            if (ZNet.instance == null)
            {
                return null;
            }
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer.IsReady() && peer.m_characterID == characterId)
                {
                    return new PlayerIdentity { Id = peer.m_playerID, Name = peer.m_playerName };
                }
            }
            return null;
        }
    }
}
