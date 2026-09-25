using ValheimTelemetry.Telemetry;

namespace ValheimTelemetry.Tracking
{
    internal sealed class WorldStateTracker
    {
        private readonly bool _enabled;
        private readonly ITelemetrySink _sink;

        public WorldStateTracker(bool enabled, ITelemetrySink sink)
        {
            _enabled = enabled;
            _sink = sink;
        }

        public void KeySet(string fullKey, bool existed, string oldValue)
        {
            if (!_enabled || string.IsNullOrEmpty(fullKey)) return;
            Split(fullKey, out string key, out string value);
            if (existed && string.Equals(value ?? string.Empty, oldValue ?? string.Empty, System.StringComparison.OrdinalIgnoreCase)) return;
            _sink.Emit("world_key_changed", new TelemetryEvent()
                .Add("action", existed ? "updated" : "added")
                .Add("key", key)
                .Add("value", EmptyToNull(value))
                .Add("previous_value", EmptyToNull(oldValue)));
        }

        public void KeyRemoved(string fullKey, string oldValue)
        {
            if (!_enabled || string.IsNullOrEmpty(fullKey)) return;
            Split(fullKey, out string key, out _);
            _sink.Emit("world_key_changed", new TelemetryEvent()
                .Add("action", "removed")
                .Add("key", key)
                .Add("value", null)
                .Add("previous_value", EmptyToNull(oldValue)));
        }

        private static void Split(string fullKey, out string key, out string value)
        {
            string normalized = fullKey.Trim().ToLowerInvariant();
            int separator = normalized.IndexOf(' ');
            if (separator == -1)
            {
                key = normalized;
                value = null;
                return;
            }
            key = normalized.Substring(0, separator);
            value = normalized.Substring(separator + 1).Trim();
        }

        private static object EmptyToNull(string value) => string.IsNullOrEmpty(value) ? null : value;
    }
}
