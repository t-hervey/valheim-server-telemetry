using UnityEngine;

namespace ValheimTelemetry.Util
{
    internal sealed class PrefabInfo
    {
        public GameObject Prefab;
        public string Type;
        public string DisplayName;
        public Character Character;
        public Piece Piece;
        public Plant Plant;
        public bool IsPlayer;
        public bool IsMob;
        public bool IsPortal;
        public bool IsShip;
        public bool IsTreeBase;
        public bool IsTreeSapling;
        public bool IsPiece;
    }

    internal static class PrefabUtil
    {
        public static PrefabInfo Describe(ZDO zdo)
        {
            if (zdo == null || ZNetScene.instance == null)
            {
                return null;
            }

            GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
            if (prefab == null)
            {
                return null;
            }

            Character character = prefab.GetComponent<Character>();
            Piece piece = prefab.GetComponent<Piece>();
            Plant plant = prefab.GetComponent<Plant>();
            bool isPlayer = prefab.GetComponent<Player>() != null;
            PrefabInfo result = new PrefabInfo
            {
                Prefab = prefab,
                Type = NormalizeName(prefab.name),
                Character = character,
                Piece = piece,
                Plant = plant,
                IsPlayer = isPlayer,
                IsMob = character != null && !isPlayer,
                IsPortal = prefab.GetComponent<TeleportWorld>() != null,
                IsShip = prefab.GetComponent<Ship>() != null,
                IsTreeBase = prefab.GetComponent<TreeBase>() != null,
                IsTreeSapling = IsTreeSapling(plant),
                IsPiece = piece != null
            };
            result.DisplayName = Localize(character != null ? character.m_name : piece != null ? piece.m_name : plant != null ? plant.m_name : result.Type);
            return result;
        }

        public static int Level(ZDO zdo)
        {
            return zdo == null ? 1 : Mathf.Max(1, zdo.GetInt(ZDOVars.s_level, 1));
        }

        public static string Biome(Vector3 position)
        {
            try
            {
                return WorldGenerator.instance == null ? null : WorldGenerator.instance.GetBiome(position).ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeName(string value)
        {
            const string clone = "(Clone)";
            if (value != null && value.EndsWith(clone))
            {
                return value.Substring(0, value.Length - clone.Length);
            }
            return value;
        }

        private static bool IsTreeSapling(Plant plant)
        {
            if (plant == null || plant.m_grownPrefabs == null)
            {
                return false;
            }
            for (int i = 0; i < plant.m_grownPrefabs.Length; i++)
            {
                GameObject grown = plant.m_grownPrefabs[i];
                if (grown != null && grown.GetComponent<TreeBase>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static string Localize(string value)
        {
            try
            {
                return Localization.instance == null ? value : Localization.instance.Localize(value);
            }
            catch
            {
                return value;
            }
        }
    }
}
