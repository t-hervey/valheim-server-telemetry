using UnityEngine;
using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;

namespace ValheimTelemetry.Util
{
    internal static class ZdoUtil
    {
        public static TelemetryEvent AddPosition(TelemetryEvent telemetryEvent, Vector3 position, PluginConfig config)
        {
            telemetryEvent.Add("x", config.IncludePosition ? (object)position.x : null);
            telemetryEvent.Add("y", config.IncludePosition ? (object)position.y : null);
            telemetryEvent.Add("z", config.IncludePosition ? (object)position.z : null);
            return telemetryEvent;
        }

        public static TelemetryEvent AddLevel(TelemetryEvent telemetryEvent, ZDO zdo, string prefix)
        {
            int level = PrefabUtil.Level(zdo);
            telemetryEvent.Add(prefix + "level", level);
            telemetryEvent.Add(prefix + "stars", level - 1);
            return telemetryEvent;
        }
    }
}
