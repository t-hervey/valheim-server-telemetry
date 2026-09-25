using System;
using System.Globalization;
using BepInEx.Logging;
using ValheimTelemetry.Config;

namespace ValheimTelemetry.Telemetry
{
    public sealed class TelemetrySink : ITelemetrySink
    {
        private readonly ITelemetryLog _log;
        private readonly string _prefix;
        private readonly Func<DateTime> _utcNow;
        private readonly Func<string> _worldName;

        public TelemetrySink(ManualLogSource log, PluginConfig config)
            : this(new BepInExTelemetryLog(log), config.Prefix, () => DateTime.UtcNow, GetWorldName)
        {
        }

        internal TelemetrySink(ITelemetryLog log, string prefix, Func<DateTime> utcNow, Func<string> worldName)
        {
            _log = log;
            _prefix = prefix;
            _utcNow = utcNow;
            _worldName = worldName;
        }

        public void Emit(string eventName, Action<TelemetryEvent> populate)
        {
            try
            {
                TelemetryEvent telemetryEvent = new TelemetryEvent()
                    .Add("schema_version", 1)
                    .Add("event", eventName)
                    .Add("timestamp", _utcNow().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture))
                    .Add("world", _worldName());
                populate?.Invoke(telemetryEvent);
                _log.Info(_prefix + " " + TelemetrySerializer.Serialize(telemetryEvent));
            }
            catch (Exception ex)
            {
                _log.Error("ValheimTelemetry sink failed safely: " + ex);
            }
        }

        public void Emit(string eventName, TelemetryEvent telemetryEvent)
        {
            Emit(eventName, target =>
            {
                foreach (var property in telemetryEvent.Properties)
                {
                    target.Add(property.Key, property.Value);
                }
            });
        }

        private static string GetWorldName()
        {
            try
            {
                return ZNet.instance?.GetWorldName();
            }
            catch
            {
                return null;
            }
        }
    }
}
