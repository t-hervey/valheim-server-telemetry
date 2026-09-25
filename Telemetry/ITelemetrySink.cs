using System;

namespace ValheimTelemetry.Telemetry
{
    public interface ITelemetrySink
    {
        void Emit(string eventName, Action<TelemetryEvent> populate);
        void Emit(string eventName, TelemetryEvent telemetryEvent);
    }
}
