using System;
using System.Globalization;
using BepInEx.Logging;
using ValheimTelemetry.Config;

namespace ValheimTelemetry.Telemetry
{
    public sealed class TelemetrySink : ITelemetrySink
    {
        private readonly ManualLogSource _log;
        private readonly PluginConfig _config;

        public TelemetrySink(ManualLogSource log, PluginConfig config)
        {
            _log = log;
            _config = config;
        }

        public void Emit(string eventName, Action<TelemetryEvent> populate)
        {
            try
            {
                TelemetryEvent telemetryEvent = new TelemetryEvent()
                    .Add("schema_version", 1)
                    .Add("event", eventName)
                    .Add("timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture))
                    .Add("world", GetWorldName());
                populate?.Invoke(telemetryEvent);
                _log.LogInfo(_config.Prefix + " " + TelemetrySerializer.Serialize(telemetryEvent));
            }
            catch (Exception ex)
            {
                _log.LogError("ValheimTelemetry sink failed safely: " + ex);
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
