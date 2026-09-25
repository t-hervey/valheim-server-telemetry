using System.Collections.Generic;

namespace ValheimTelemetry.Telemetry
{
    public sealed class TelemetryEvent
    {
        private readonly List<KeyValuePair<string, object>> _properties = new List<KeyValuePair<string, object>>(20);

        public IList<KeyValuePair<string, object>> Properties => _properties;

        public TelemetryEvent Add(string name, object value)
        {
            _properties.Add(new KeyValuePair<string, object>(name, value));
            return this;
        }
    }
}

