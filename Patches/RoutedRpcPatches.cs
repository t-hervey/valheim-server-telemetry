using System;
using System.Reflection;
using HarmonyLib;

namespace ValheimTelemetry.Patches
{
    [HarmonyPatch]
    internal static class RoutedDamagePatch
    {
        private static readonly int DamageHash = "RPC_Damage".GetStableHashCode();

        private static MethodBase TargetMethod() => AccessTools.Method(typeof(ZRoutedRpc), "RPC_RoutedRPC", new[] { typeof(ZRpc), typeof(ZPackage) });

        private static void Prefix(ZPackage pkg)
        {
            try
            {
                if (Plugin.Runtime == null || !Plugin.Runtime.Armed || pkg == null) return;
                int position = pkg.GetPos();
                ZPackage copy = new ZPackage(pkg.GetArray());
                copy.SetPos(position);
                var data = new ZRoutedRpc.RoutedRPCData();
                data.Deserialize(copy);
                if (data.m_methodHash != DamageHash || data.m_targetZDO.IsNone()) return;
                ZPackage parameters = new ZPackage(data.m_parameters.GetArray());
                parameters.SetPos(0);
                var hit = new HitData();
                hit.Deserialize(ref parameters);
                Plugin.Runtime.RoutedDamage(data.m_targetZDO, hit);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError("ValheimTelemetry routed damage observation failed safely: " + ex);
            }
        }
    }
}
