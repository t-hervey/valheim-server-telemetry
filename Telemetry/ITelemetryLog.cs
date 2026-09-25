using BepInEx.Logging;

namespace ValheimTelemetry.Telemetry
{
    internal interface ITelemetryLog
    {
        void Info(string message);
        void Error(string message);
    }

    internal sealed class BepInExTelemetryLog : ITelemetryLog
    {
        private readonly ManualLogSource _log;

        public BepInExTelemetryLog(ManualLogSource log)
        {
            _log = log;
        }

        public void Info(string message) => _log.LogInfo(message);
        public void Error(string message) => _log.LogError(message);
    }
}
