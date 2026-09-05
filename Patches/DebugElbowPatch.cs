using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(VRRig), "SerializeWriteShared")]
internal static class DebugSerializeCheck
{
    private static void Prefix(VRRig __instance)
    {
        if (__instance.Creator == null || !__instance.Creator.IsLocal) return;

        var ik = __instance.GetComponent<GorillaIK>();
        if (ik == null) return;

        Debug.Log($"[MRDEBUG] usingNewIK-input left={ik.leftElbowDirection} right={ik.rightElbowDirection} bodyRot={ik.targetBodyRot}");
    }
}