using System;
using System.Reflection;
using HarmonyLib;

namespace ValheimTelemetry.Patches
{
    internal static class NetworkPatchUtil
    {
        public static ZNetPeer FindPeer(ZNet net, ZRpc rpc)
        {
            if (net == null || rpc == null) return null;
            foreach (ZNetPeer peer in net.GetPeers())
            {
                if (ReferenceEquals(peer.m_rpc, rpc)) return peer;
            }
            return null;
        }
    }

    [HarmonyPatch]
    internal static class PeerInfoPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(ZNet), "RPC_PeerInfo", new[] { typeof(ZRpc), typeof(ZPackage) });

        private static void Postfix(ZNet __instance, ZRpc rpc)
        {
            try { Plugin.Runtime?.PeerConnected(NetworkPatchUtil.FindPeer(__instance, rpc)); }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry peer-connect observation failed safely: " + ex); }
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect), new[] { typeof(ZNetPeer) })]
    internal static class PeerDisconnectPatch
    {
        private static void Prefix(ZNetPeer peer)
        {
            try { Plugin.Runtime?.PeerDisconnected(peer); }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry peer-disconnect observation failed safely: " + ex); }
        }
    }

    [HarmonyPatch]
    internal static class CharacterIdPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(ZNet), "RPC_CharacterID", new[] { typeof(ZRpc), typeof(ZDOID) });

        private static void Postfix(ZNet __instance, ZRpc rpc)
        {
            try { Plugin.Runtime?.PeerCharacterChanged(NetworkPatchUtil.FindPeer(__instance, rpc)); }
            catch (Exception ex) { Plugin.Log?.LogError("ValheimTelemetry character-session observation failed safely: " + ex); }
        }
    }
}
