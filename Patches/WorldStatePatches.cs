using System;
using System.Reflection;
using HarmonyLib;

namespace ValheimTelemetry.Patches
{
    [HarmonyPatch]
    internal static class GlobalKeyAddPatch
    {
        internal sealed class State
        {
            public bool Existed;
            public string OldValue;
        }

        private static MethodBase TargetMethod() => AccessTools.Method(typeof(ZoneSystem), "GlobalKeyAdd", new[] { typeof(string), typeof(bool) });

        private static void Prefix(ZoneSystem __instance, string keyStr, out State __state)
        {
            __state = new State();
            try
            {
                string key = BaseKey(keyStr);
                __state.Existed = __instance.GetGlobalKey(key, out string value);
                __state.OldValue = value;
            }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry world-key pre-set observation failed safely: " + ex); }
        }

        private static void Postfix(string keyStr, State __state)
        {
            try
            {
                if (__state != null) Plugin.Runtime?.WorldKeySet(keyStr, __state.Existed, __state.OldValue);
            }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry world-key set observation failed safely: " + ex); }
        }

        internal static string BaseKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string normalized = value.Trim().ToLowerInvariant();
            int separator = normalized.IndexOf(' ');
            return separator < 0 ? normalized : normalized.Substring(0, separator);
        }
    }

    [HarmonyPatch]
    internal static class GlobalKeyRemovePatch
    {
        internal sealed class State
        {
            public string OldValue;
        }

        private static MethodBase TargetMethod() => AccessTools.Method(typeof(ZoneSystem), "GlobalKeyRemove", new[] { typeof(string), typeof(bool) });

        private static void Prefix(ZoneSystem __instance, string keyStr, out State __state)
        {
            __state = new State();
            try { __instance.GetGlobalKey(GlobalKeyAddPatch.BaseKey(keyStr), out __state.OldValue); }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry world-key pre-remove observation failed safely: " + ex); }
        }

        private static void Postfix(string keyStr, bool __result, State __state)
        {
            try
            {
                if (__result) Plugin.Runtime?.WorldKeyRemoved(keyStr, __state?.OldValue);
            }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry world-key remove observation failed safely: " + ex); }
        }
    }
}
