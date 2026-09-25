using System;
using System.Reflection;
using HarmonyLib;

namespace ValheimTelemetry.Patches
{
    [HarmonyPatch]
    internal static class ZdoCreatePatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(ZDOMan), "CreateNewZDO", new[] { typeof(ZDOID), typeof(UnityEngine.Vector3), typeof(int) });

        private static void Postfix(ZDO __result)
        {
            try { Plugin.Runtime?.Created(__result); }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry create observation failed safely: " + ex); }
        }
    }

    [HarmonyPatch]
    internal static class ZdoDestroyPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(ZDOMan), "HandleDestroyedZDO", new[] { typeof(ZDOID) });

        private static void Prefix(ZDOMan __instance, ZDOID uid)
        {
            try { Plugin.Runtime?.Destroyed(__instance.GetZDO(uid)); }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry destroy observation failed safely: " + ex); }
        }
    }

    [HarmonyPatch(typeof(ZDO), nameof(ZDO.Deserialize))]
    internal static class ZdoDeserializePatch
    {
        internal sealed class State
        {
            public bool HadHealth;
            public float Health;
            public bool Tamed;
        }

        private static void Prefix(ZDO __instance, out State __state)
        {
            __state = new State();
            try
            {
                __state.HadHealth = __instance.GetFloat(ZDOVars.s_health, out __state.Health);
                __state.Tamed = __instance.GetBool(ZDOVars.s_tamed, false);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError("ValheimTelemetry ZDO pre-deserialize observation failed safely: " + ex);
            }
        }

        private static void Postfix(ZDO __instance, State __state)
        {
            try
            {
                if (__state == null) return;
                Plugin.Runtime?.HealthChanged(__instance, __state.HadHealth, __state.Health);
                Plugin.Runtime?.TamedChanged(__instance, __state.Tamed);
            }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry ZDO deserialize observation failed safely: " + ex); }
        }
    }

    [HarmonyPatch]
    internal static class ZdoFloatSetPatch
    {
        internal sealed class State
        {
            public bool Observe;
            public bool HadValue;
            public float Value;
        }

        private static MethodBase TargetMethod() => AccessTools.Method(typeof(ZDO), nameof(ZDO.Set), new[] { typeof(int), typeof(float) });

        private static void Prefix(ZDO __instance, int hash, out State __state)
        {
            __state = new State { Observe = hash == ZDOVars.s_health };
            if (!__state.Observe) return;
            try { __state.HadValue = __instance.GetFloat(hash, out __state.Value); }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry health pre-set observation failed safely: " + ex); }
        }

        private static void Postfix(ZDO __instance, State __state)
        {
            try
            {
                if (__state != null && __state.Observe) Plugin.Runtime?.HealthChanged(__instance, __state.HadValue, __state.Value);
            }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry health observation failed safely: " + ex); }
        }
    }

    [HarmonyPatch]
    internal static class ZdoIntSetPatch
    {
        internal sealed class State
        {
            public bool Observe;
            public bool Tamed;
        }

        private static MethodBase TargetMethod() => AccessTools.Method(typeof(ZDO), nameof(ZDO.Set), new[] { typeof(int), typeof(int), typeof(bool) });

        private static void Prefix(ZDO __instance, int hash, out State __state)
        {
            __state = new State { Observe = hash == ZDOVars.s_tamed };
            if (!__state.Observe) return;
            try { __state.Tamed = __instance.GetBool(hash, false); }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry tame pre-set observation failed safely: " + ex); }
        }

        private static void Postfix(ZDO __instance, State __state)
        {
            try
            {
                if (__state != null && __state.Observe) Plugin.Runtime?.TamedChanged(__instance, __state.Tamed);
            }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry tame observation failed safely: " + ex); }
        }
    }
}
