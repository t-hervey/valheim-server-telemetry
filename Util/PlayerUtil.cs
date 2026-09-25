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
                if (!peer.IsReady())
                {
                    continue;
                }

                long peerPlayerId = peer.m_playerID;
                if (!peer.m_characterID.IsNone() && ZDOMan.instance != null)
                {
                    ZDO character = ZDOMan.instance.GetZDO(peer.m_characterID);
                    if (character != null)
                    {
                        peerPlayerId = character.GetLong(ZDOVars.s_playerID, peerPlayerId);
                    }
                }

                if (peerPlayerId == creator)
                {
                    return new PlayerIdentity { Id = creator, Name = peer.m_playerName };
                }
            }
            return new PlayerIdentity { Id = creator, Name = null };
        }

        public static PlayerIdentity FromPeer(ZNetPeer peer)
        {
            if (peer == null || !peer.IsReady())
            {
                return null;
            }
            if (!peer.m_characterID.IsNone())
            {
                PlayerIdentity character = FromCharacter(peer.m_characterID);
                if (character != null)
                {
                    return character;
                }
            }
            return new PlayerIdentity { Id = peer.m_playerID, Name = peer.m_playerName };
        }

        public static PlayerIdentity FromPlatformAuthor(string authorId)
        {
            if (string.IsNullOrEmpty(authorId) || ZNet.instance == null)
            {
                return null;
            }
            foreach (ZNet.PlayerInfo player in ZNet.instance.GetPlayerList())
            {
                string platformId = player.m_userInfo.m_id.ToString();
                if (string.Equals(platformId, authorId, System.StringComparison.Ordinal))
                {
                    PlayerIdentity identity = FromCharacter(player.m_characterID);
                    return identity ?? new PlayerIdentity { Id = 0L, Name = player.m_name };
                }
            }
            return null;
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
