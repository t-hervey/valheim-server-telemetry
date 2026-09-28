namespace ValheimTelemetry.MapExport
{
    internal readonly struct MapPixelColor
    {
        public MapPixelColor(byte red, byte green, byte blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }
    }

    internal static class MapTerrainPalette
    {
        // Valheim's world water plane and native minimap cutoff are both at Y=30.
        public const float WaterLevel = 30f;

        public static readonly MapPixelColor Ocean = new MapPixelColor(38, 81, 110);
        public static readonly MapPixelColor InlandWater = new MapPixelColor(74, 144, 184);
        public static readonly MapPixelColor SwampWater = new MapPixelColor(75, 120, 141);
        public static readonly MapPixelColor Mistlands = new MapPixelColor(116, 83, 143);

        public static MapPixelColor Select(bool isOceanBiome, bool isSwampBiome, float terrainHeight, MapPixelColor biomeColor)
        {
            if (isOceanBiome) return Ocean;
            if (terrainHeight >= WaterLevel) return biomeColor;
            return isSwampBiome ? SwampWater : InlandWater;
        }
    }
}
