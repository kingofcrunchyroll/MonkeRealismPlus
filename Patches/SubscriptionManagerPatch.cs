using GorillaTagScripts;
using HarmonyLib;
using MonkeRealism;

[HarmonyPatch(typeof(SubscriptionManager))]
internal static class SubscriptionManagerPatch
{
    // Patches the check in GorillaIK.SkeletonUpdate() — this is the
    // primary gate that enables the body tracking pipeline locally.
    [HarmonyPrefix]
    [HarmonyPatch("IsLocalSubscribed")]
    private static bool IsLocalSubscribed(ref bool __result)
    {
        var plugin = Plugin.Instance;
        if (plugin != null && plugin.ShouldUseElbowTracking.Value)
        {
            __result = true;
            return false;
        }
        return true;
    }

    // Patches the setting bool so the skeleton GameObject actually
    // gets SetActive(true) inside SkeletonUpdate().
    [HarmonyPrefix]
    [HarmonyPatch("GetSubscriptionSettingBool")]
    private static bool GetSubscriptionSettingBool(ref bool __result, SubscriptionManager.SubscriptionFeatures feature)
    {
        var plugin = Plugin.Instance;
        if (plugin != null && plugin.ShouldUseElbowTracking.Value && feature == SubscriptionManager.SubscriptionFeatures.IOBT)
        {
            __result = true;
            return false;
        }
        return true;
    }

    // Patches the flag check in GorillaIKMgr.CopyInput() — this controls
    // whether usingNewIK is true, which enables elbow direction input
    // and body rotation in the IK job.
    [HarmonyPrefix]
    [HarmonyPatch("GetSubscriptionDetails", new[] { typeof(VRRig) })]
    private static bool GetSubscriptionDetails_VRRig(ref SubscriptionManager.SubscriptionDetails __result, VRRig rig)
    {
        var plugin = Plugin.Instance;
        if (plugin != null && plugin.ShouldUseElbowTracking.Value && rig == VRRig.LocalRig)
        {
            __result = new SubscriptionManager.SubscriptionDetails { active = true };
            return false;
        }
        return true;
    }

    // Same as above but for the NetPlayer overload — CopyInput() uses
    // the VRRig path via gorillaIK.myRig, so this one is lower priority
    // but keeps things consistent if anything else calls it.
    [HarmonyPrefix]
    [HarmonyPatch("GetSubscriptionDetails", new[] { typeof(NetPlayer) })]
    private static bool GetSubscriptionDetails_NetPlayer(ref SubscriptionManager.SubscriptionDetails __result, NetPlayer np)
    {
        var plugin = Plugin.Instance;
        if (plugin != null && plugin.ShouldUseElbowTracking.Value
            && VRRig.LocalRig != null && np == VRRig.LocalRig.Creator)
        {
            __result = new SubscriptionManager.SubscriptionDetails { active = true };
            return false;
        }
        return true;
    }

    //[HarmonyPrefix]
    [HarmonyPatch(typeof(NetworkSystemPUN), nameof(NetworkSystemPUN.ConnectToRoom))]
    public class ConnectToRoom
    {
        public static bool Prefix(string roomName, RoomConfig opts, int regionIndex = -1)
        {
            if (opts.MaxPlayers == 20)
                opts.MaxPlayers = 10;
            if ((string)opts.CustomProps["fan_club"] == "true")
                opts.CustomProps["fan_club"] = "false";
            return true;
        }
    }
}