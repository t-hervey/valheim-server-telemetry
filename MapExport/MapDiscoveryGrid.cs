using System;

namespace ValheimTelemetry.MapExport
{
    internal sealed class MapDiscoveryGrid
    {
        public const float WorldMinimum = -10500f;
        public const float WorldMaximum = 10500f;
        private readonly byte[] _pixels;

        public int Resolution { get; }
        public byte[] Pixels => _pixels;

        public MapDiscoveryGrid(int resolution)
        {
            if (resolution <= 0) throw new ArgumentOutOfRangeException(nameof(resolution));
            Resolution = resolution;
            _pixels = new byte[resolution * resolution];
        }

        public bool Explore(float worldX, float worldZ, float radius)
        {
            float scale = Resolution / (WorldMaximum - WorldMinimum);
            int centerX = (int)Math.Round((worldX - WorldMinimum) * scale);
            int centerY = (int)Math.Round((WorldMaximum - worldZ) * scale);
            int pixelRadius = Math.Max(1, (int)Math.Ceiling(radius * scale));
            bool changed = false;
            for (int y = centerY - pixelRadius; y <= centerY + pixelRadius; y++)
            {
                if (y < 0 || y >= Resolution) continue;
                int dy = y - centerY;
                for (int x = centerX - pixelRadius; x <= centerX + pixelRadius; x++)
                {
                    if (x < 0 || x >= Resolution) continue;
                    int dx = x - centerX;
                    if (dx * dx + dy * dy > pixelRadius * pixelRadius) continue;
                    int index = y * Resolution + x;
                    if (_pixels[index] != 0) continue;
                    _pixels[index] = 1;
                    changed = true;
                }
            }
            return changed;
        }

        public bool MergeNative(byte[] native, int nativeResolution, float nativePixelSize)
        {
            if (native == null || nativeResolution <= 0 || native.Length != nativeResolution * nativeResolution || nativePixelSize <= 0f) return false;
            bool changed = false;
            int half = nativeResolution / 2;
            for (int y = 0; y < nativeResolution; y++)
            {
                for (int x = 0; x < nativeResolution; x++)
                {
                    if (native[y * nativeResolution + x] == 0) continue;
                    float worldX = (x - half) * nativePixelSize;
                    float worldZ = (y - half) * nativePixelSize;
                    int outputX = WorldToPixel(worldX);
                    int outputY = WorldToPixel(-worldZ);
                    if (outputX < 0 || outputY < 0 || outputX >= Resolution || outputY >= Resolution) continue;
                    int index = outputY * Resolution + outputX;
                    if (_pixels[index] != 0) continue;
                    _pixels[index] = 1;
                    changed = true;
                }
            }
            return changed;
        }

        public void Load(byte[] pixels)
        {
            if (pixels == null || pixels.Length != _pixels.Length) return;
            Buffer.BlockCopy(pixels, 0, _pixels, 0, pixels.Length);
        }

        private int WorldToPixel(float coordinate)
        {
            return (int)Math.Round((coordinate - WorldMinimum) * Resolution / (WorldMaximum - WorldMinimum));
        }
    }
}
