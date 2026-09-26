using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using BepInEx.Logging;
using UnityEngine;
using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;

namespace ValheimTelemetry.MapExport
{
    internal sealed class MapExporter
    {
        private readonly PluginConfig _config;
        private readonly ManualLogSource _log;
        private readonly MapDiscoveryGrid _discovery;
        private readonly byte[] _terrain;
        private readonly List<string> _mapTablePrefabs = new List<string>();
        private bool _initialized;
        private bool _terrainReady;
        private int _terrainPixel;
        private float _nextPlayerSample;
        private float _nextExport;

        public MapExporter(PluginConfig config, ManualLogSource log)
        {
            _config = config;
            _log = log;
            _discovery = new MapDiscoveryGrid(config.MapExportResolution);
            _terrain = new byte[config.MapExportResolution * config.MapExportResolution * 3];
        }

        public void Tick(float now)
        {
            if (!_config.MapExportEnabled) return;
            try
            {
                if (!_initialized) Initialize(now);
                SamplePlayers(now);
                if (!_terrainReady)
                {
                    GenerateTerrainBatch(1024);
                    if (!_terrainReady) return;
                    _nextExport = now;
                }
                if (now >= _nextExport)
                {
                    Export(now);
                }
            }
            catch (Exception ex)
            {
                _nextExport = now + 30f;
                _log.LogError("ValheimTelemetry map export failed safely; gameplay is unaffected: " + ex);
            }
        }

        private void Initialize(float now)
        {
            Directory.CreateDirectory(OutputDirectory());
            string discoveryPath = Path.Combine(OutputDirectory(), "discovery.bin");
            if (File.Exists(discoveryPath)) _discovery.Load(File.ReadAllBytes(discoveryPath));
            FindMapTablePrefabs();
            _initialized = true;
            _nextExport = now;
            _log.LogInfo("ValheimTelemetry map export initialized at " + OutputDirectory() + "; terrain generation is incremental.");
        }

        private void FindMapTablePrefabs()
        {
            _mapTablePrefabs.Clear();
            if (ZNetScene.instance == null) return;
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab != null && prefab.GetComponent<MapTable>() != null) _mapTablePrefabs.Add(prefab.name);
            }
        }

        private void SamplePlayers(float now)
        {
            if (now < _nextPlayerSample || ZNet.instance == null || ZDOMan.instance == null) return;
            _nextPlayerSample = now + 2f;
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (!peer.IsReady() || peer.m_characterID.IsNone()) continue;
                ZDO player = ZDOMan.instance.GetZDO(peer.m_characterID);
                if (player == null) continue;
                Vector3 position = player.GetPosition();
                _discovery.Explore(position.x, position.z, 100f);
            }
        }

        private void GenerateTerrainBatch(int maximumPixels)
        {
            WorldGenerator generator = WorldGenerator.instance;
            if (generator == null) return;
            int resolution = _config.MapExportResolution;
            int end = Math.Min(_terrainPixel + maximumPixels, resolution * resolution);
            float span = MapDiscoveryGrid.WorldMaximum - MapDiscoveryGrid.WorldMinimum;
            while (_terrainPixel < end)
            {
                int x = _terrainPixel % resolution;
                int y = _terrainPixel / resolution;
                float worldX = MapDiscoveryGrid.WorldMinimum + (x + 0.5f) * span / resolution;
                float worldZ = MapDiscoveryGrid.WorldMaximum - (y + 0.5f) * span / resolution;
                SetBiomeColor(_terrainPixel * 3, generator.GetBiome(worldX, worldZ));
                _terrainPixel++;
            }
            _terrainReady = _terrainPixel == resolution * resolution;
        }

        private void SetBiomeColor(int offset, Heightmap.Biome biome)
        {
            byte r;
            byte g;
            byte b;
            switch (biome)
            {
                case Heightmap.Biome.Meadows: r = 83; g = 145; b = 70; break;
                case Heightmap.Biome.BlackForest: r = 29; g = 83; b = 46; break;
                case Heightmap.Biome.Swamp: r = 76; g = 75; b = 61; break;
                case Heightmap.Biome.Mountain: r = 205; g = 215; b = 220; break;
                case Heightmap.Biome.Plains: r = 181; g = 165; b = 75; break;
                case Heightmap.Biome.Mistlands: r = 78; g = 71; b = 92; break;
                case Heightmap.Biome.AshLands: r = 111; g = 45; b = 39; break;
                case Heightmap.Biome.DeepNorth: r = 180; g = 211; b = 221; break;
                default: r = 38; g = 81; b = 110; break;
            }
            _terrain[offset] = r;
            _terrain[offset + 1] = g;
            _terrain[offset + 2] = b;
        }

        private void Export(float now)
        {
            Stopwatch timer = Stopwatch.StartNew();
            int tableCount = ImportCartographyTables();
            int resolution = _config.MapExportResolution;
            byte[] mask = new byte[_terrain.Length];
            byte[] discoveredMap = new byte[_terrain.Length];
            int explored = 0;
            for (int i = 0; i < _discovery.Pixels.Length; i++)
            {
                bool visible = _discovery.Pixels[i] != 0;
                if (visible) explored++;
                int offset = i * 3;
                byte value = visible ? (byte)255 : (byte)0;
                mask[offset] = value;
                mask[offset + 1] = value;
                mask[offset + 2] = value;
                discoveredMap[offset] = visible ? _terrain[offset] : (byte)12;
                discoveredMap[offset + 1] = visible ? _terrain[offset + 1] : (byte)15;
                discoveredMap[offset + 2] = visible ? _terrain[offset + 2] : (byte)18;
            }

            WriteAtomic("terrain.png", PngEncoder.EncodeRgb(resolution, resolution, _terrain));
            WriteAtomic("discovery.png", PngEncoder.EncodeRgb(resolution, resolution, mask));
            WriteAtomic("discovered-map.png", PngEncoder.EncodeRgb(resolution, resolution, discoveredMap));
            WriteAtomic("discovery.bin", _discovery.Pixels);
            WriteAtomic("metadata.json", System.Text.Encoding.UTF8.GetBytes(BuildMetadata(explored, tableCount)));
            _nextExport = now + _config.MapExportIntervalSeconds;
            timer.Stop();
            _log.LogInfo("ValheimTelemetry map export completed in " + timer.ElapsedMilliseconds + " ms; resolution=" + resolution + ", explored_pixels=" + explored + ", cartography_tables=" + tableCount + ".");
        }

        private int ImportCartographyTables()
        {
            if (ZDOMan.instance == null) return 0;
            int tableCount = 0;
            for (int prefabIndex = 0; prefabIndex < _mapTablePrefabs.Count; prefabIndex++)
            {
                var tables = new List<ZDO>();
                int sectorIndex = 0;
                while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(_mapTablePrefabs[prefabIndex], tables, ref sectorIndex)) sectorIndex++;
                for (int i = 0; i < tables.Count; i++)
                {
                    byte[] compressed = tables[i].GetByteArray(ZDOVars.s_data);
                    if (compressed == null || compressed.Length == 0) continue;
                    tableCount++;
                    ImportSharedMap(Utils.Decompress(compressed));
                }
            }
            return tableCount;
        }

        private void ImportSharedMap(byte[] data)
        {
            var package = new ZPackage(data);
            int version = package.ReadInt();
            int count = package.ReadInt();
            int resolution = (int)Math.Sqrt(count);
            if (version < 1 || version > 3 || resolution <= 0 || resolution * resolution != count || resolution > 4096) return;
            byte[] explored = new byte[count];
            for (int i = 0; i < count; i++) explored[i] = package.ReadBool() ? (byte)1 : (byte)0;
            float pixelSize = Minimap.instance != null && Minimap.instance.m_pixelSize > 0f
                ? Minimap.instance.m_pixelSize
                : (MapDiscoveryGrid.WorldMaximum - MapDiscoveryGrid.WorldMinimum) / resolution;
            _discovery.MergeNative(explored, resolution, pixelSize);
        }

        private string BuildMetadata(int explored, int tableCount)
        {
            int resolution = _config.MapExportResolution;
            double coverage = explored * 100d / (resolution * resolution);
            string world = null;
            try { world = ZNet.instance?.GetWorldName(); } catch { }
            return TelemetrySerializer.Serialize(new TelemetryEvent()
                .Add("schema_version", 1)
                .Add("generated_at", DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture))
                .Add("world", world)
                .Add("resolution", resolution)
                .Add("world_min_x", MapDiscoveryGrid.WorldMinimum)
                .Add("world_max_x", MapDiscoveryGrid.WorldMaximum)
                .Add("world_min_z", MapDiscoveryGrid.WorldMinimum)
                .Add("world_max_z", MapDiscoveryGrid.WorldMaximum)
                .Add("image_top", "north_positive_z")
                .Add("explored_pixels", explored)
                .Add("coverage_percent", coverage)
                .Add("cartography_tables", tableCount)
                .Add("discovery_scope", "server_observed_plus_cartography_tables"));
        }

        private string OutputDirectory() => Path.GetFullPath(_config.MapExportDirectory);

        private void WriteAtomic(string name, byte[] data)
        {
            string path = Path.Combine(OutputDirectory(), name);
            string temporary = path + ".tmp";
            File.WriteAllBytes(temporary, data);
            if (File.Exists(path))
            {
                try { File.Replace(temporary, path, null); }
                catch (PlatformNotSupportedException)
                {
                    File.Delete(path);
                    File.Move(temporary, path);
                }
            }
            else
            {
                File.Move(temporary, path);
            }
        }
    }
}
