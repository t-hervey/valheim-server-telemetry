using System;
using BepInEx.Logging;
using UnityEngine;
using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;

namespace ValheimTelemetry.Tracking
{
    internal sealed class TelemetryRuntime
    {
        private readonly PluginConfig _config;
        private readonly ManualLogSource _log;
        private readonly LifecycleTracker _lifecycle;
        private readonly SnapshotCollector _snapshots;
        private float _readyAt = -1f;
        private float _nextSnapshot = float.PositiveInfinity;

        public bool Armed { get; private set; }

        public TelemetryRuntime(PluginConfig config, ITelemetrySink sink, ManualLogSource log)
        {
            _config = config;
            _log = log;
            _lifecycle = new LifecycleTracker(config, sink);
            _snapshots = new SnapshotCollector(config, sink, log);
        }

        public void Tick()
        {
            float now = Time.realtimeSinceStartup;
            if (!Armed)
            {
                if (!ServerReady())
                {
                    _readyAt = -1f;
                    return;
                }
                if (_readyAt < 0f)
                {
                    _readyAt = now + 20f;
                    return;
                }
                if (now < _readyAt) return;
                Armed = true;
                _nextSnapshot = now + 5f;
                _log.LogInfo("ValheimTelemetry armed after startup grace; existing ZDOs are baseline only and will not emit lifecycle events.");
            }

            _lifecycle.Tick(now);
            if (now >= _nextSnapshot)
            {
                _nextSnapshot = now + _config.SnapshotIntervalSeconds;
                try { _snapshots.Collect(); }
                catch (Exception ex) { _log.LogError("ValheimTelemetry snapshot failed safely: " + ex); }
            }
        }

        public void Created(ZDO zdo) { if (Armed && ServerReady()) _lifecycle.Created(zdo); }
        public void Destroyed(ZDO zdo) { if (Armed && ServerReady()) _lifecycle.Destroyed(zdo); }
        public void HealthChanged(ZDO zdo, bool hadOldHealth, float oldHealth) { if (Armed && ServerReady()) _lifecycle.HealthChanged(zdo, hadOldHealth, oldHealth); }
        public void TamedChanged(ZDO zdo, bool oldTamed) { if (Armed && ServerReady()) _lifecycle.TamedChanged(zdo, oldTamed); }
        public void RoutedDamage(ZDOID target, HitData hit) { if (Armed && ServerReady()) _lifecycle.RoutedDamage(target, hit); }

        private static bool ServerReady()
        {
            return ZNet.instance != null && ZNet.instance.IsServer() && ZDOMan.instance != null && ZNetScene.instance != null && ZoneSystem.instance != null && WorldGenerator.instance != null;
        }
    }
}
