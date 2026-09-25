using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;
using ValheimTelemetry.Tracking;

namespace ValheimTelemetry
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "dev.deepnorth.valheimtelemetry";
        public const string PluginName = "ValheimTelemetry";
        public const string PluginVersion = "1.0.1";

        internal static TelemetryRuntime Runtime { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            try
            {
                PluginConfig settings = PluginConfig.Load(Config, Logger);
                if (!settings.Enabled)
                {
                    Logger.LogInfo("ValheimTelemetry is disabled by configuration.");
                    return;
                }

                ITelemetrySink sink = new TelemetrySink(Logger, settings);
                Runtime = new TelemetryRuntime(settings, sink, Logger);
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll(typeof(Plugin).Assembly);
                Logger.LogInfo("ValheimTelemetry initialized; passive server-only patches applied. Telemetry will arm after world startup grace.");
            }
            catch (Exception ex)
            {
                Runtime = null;
                Logger.LogError("ValheimTelemetry initialization failed safely; vanilla server execution will continue: " + ex);
            }
        }

        private void Update()
        {
            try
            {
                Runtime?.Tick();
            }
            catch (Exception ex)
            {
                Logger.LogError("ValheimTelemetry update failed safely: " + ex);
            }
        }

        private void OnDestroy()
        {
            try
            {
                _harmony?.UnpatchSelf();
            }
            catch (Exception ex)
            {
                Logger.LogError("ValheimTelemetry unpatch failed safely: " + ex);
            }
            finally
            {
                Runtime = null;
            }
        }
    }
}
