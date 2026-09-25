using System;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace ValheimTelemetry.Config
{
    public sealed class PluginConfig
    {
        public bool Enabled { get; private set; }
        public bool DebugLogging { get; private set; }
        public int SnapshotIntervalSeconds { get; private set; }

        public bool MobKills { get; private set; }
        public bool MobSpawns { get; private set; }
        public bool BuildingPieces { get; private set; }
        public bool Portals { get; private set; }
        public bool Ships { get; private set; }
        public bool Taming { get; private set; }
        public bool Trees { get; private set; }
        public bool PlayerSessions { get; private set; }
        public bool PlayerDeaths { get; private set; }
        public bool PortalTagChanges { get; private set; }
        public bool TamedCreatureDeaths { get; private set; }
        public bool BossKills { get; private set; }
        public bool WorldKeys { get; private set; }

        public bool SnapshotActiveMobs { get; private set; }
        public bool SnapshotPortals { get; private set; }
        public bool SnapshotBuildingPieces { get; private set; }
        public bool SnapshotShips { get; private set; }
        public bool SnapshotTamedCreatures { get; private set; }

        public bool IncludePlayerName { get; private set; }
        public bool IncludePlayerId { get; private set; }
        public bool IncludePosition { get; private set; }
        public string Prefix { get; private set; }

        private PluginConfig()
        {
        }

        public static PluginConfig Load(ConfigFile file, ManualLogSource log)
        {
            PluginConfig result = new PluginConfig
            {
                Enabled = Bind(file, log, "General", "Enabled", true, "Enable passive telemetry."),
                DebugLogging = Bind(file, log, "General", "DebugLogging", false, "Log snapshot timing and tracker diagnostics."),
                SnapshotIntervalSeconds = Math.Max(10, Bind(file, log, "General", "SnapshotIntervalSeconds", 300, "Interval between active-area aggregate snapshots.")),

                MobKills = Bind(file, log, "Events", "MobKills", true, "Emit corroborated mob death events."),
                MobSpawns = Bind(file, log, "Events", "MobSpawns", true, "Emit newly-created creature ZDO events."),
                BuildingPieces = Bind(file, log, "Events", "BuildingPieces", true, "Emit player-built piece create/destroy events."),
                Portals = Bind(file, log, "Events", "Portals", true, "Emit portal create/destroy events."),
                Ships = Bind(file, log, "Events", "Ships", true, "Emit ship create/destroy events."),
                Taming = Bind(file, log, "Events", "Taming", true, "Emit existing-creature tame state transitions."),
                Trees = Bind(file, log, "Events", "Trees", true, "Emit standing-tree felling events."),
                PlayerSessions = Bind(file, log, "Events", "PlayerSessions", true, "Emit player gameplay-session connect/disconnect events."),
                PlayerDeaths = Bind(file, log, "Events", "PlayerDeaths", true, "Emit server-visible player death events."),
                PortalTagChanges = Bind(file, log, "Events", "PortalTagChanges", true, "Emit changes to existing portal tags."),
                TamedCreatureDeaths = Bind(file, log, "Events", "TamedCreatureDeaths", true, "Emit deaths of tamed creatures."),
                BossKills = Bind(file, log, "Events", "BossKills", true, "Emit boss death events."),
                WorldKeys = Bind(file, log, "Events", "WorldKeys", true, "Emit server world-key additions, updates, and removals."),

                SnapshotActiveMobs = Bind(file, log, "Snapshots", "ActiveMobs", true, "Count wild mobs in connected peers' active simulation areas."),
                SnapshotPortals = Bind(file, log, "Snapshots", "Portals", true, "Count portals in connected peers' active simulation areas."),
                SnapshotBuildingPieces = Bind(file, log, "Snapshots", "BuildingPieces", true, "Count player-built pieces in connected peers' active simulation areas."),
                SnapshotShips = Bind(file, log, "Snapshots", "Ships", true, "Count ships in connected peers' active simulation areas."),
                SnapshotTamedCreatures = Bind(file, log, "Snapshots", "TamedCreatures", true, "Count tamed creatures in connected peers' active simulation areas."),

                IncludePlayerName = Bind(file, log, "Privacy", "IncludePlayerName", true, "Include reliably resolved character names."),
                IncludePlayerId = Bind(file, log, "Privacy", "IncludePlayerId", true, "Include reliably resolved Valheim character IDs."),
                IncludePosition = Bind(file, log, "Privacy", "IncludePosition", true, "Include world coordinates."),
                Prefix = Bind(file, log, "Logging", "Prefix", "VALHEIM_TELEMETRY", "Prefix placed before each one-line JSON document.")
            };

            result.Prefix = SanitizePrefix(result.Prefix);
            return result;
        }

        private static T Bind<T>(ConfigFile file, ManualLogSource log, string section, string key, T fallback, string description)
        {
            try
            {
                return file.Bind(section, key, fallback, description).Value;
            }
            catch (Exception ex)
            {
                log.LogWarning("ValheimTelemetry config value " + section + "." + key + " failed; using default: " + ex.Message);
                return fallback;
            }
        }

        private static string SanitizePrefix(string value)
        {
            string safe = (value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
            return safe.Length == 0 ? "VALHEIM_TELEMETRY" : safe;
        }
    }
}
