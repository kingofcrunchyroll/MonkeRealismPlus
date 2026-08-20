using System.Reflection;
using HarmonyLib;
using UnityEngine;
using MonkeRealism;

[HarmonyPatch]
internal static class VRRigSerializeWriteSharedPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(VRRig),
            "SerializeWriteShared"
        );
    }

    private static void Prefix(VRRig __instance)
    {
        Plugin plugin = Plugin.Instance;

        if (plugin == null ||
            !plugin.ShouldUseElbowTracking.Value ||
            !NetworkSystem.Instance.InRoom)
        {
            return;
        }

        // Only modify our own outgoing rig data.
        if (__instance.Creator == null || !__instance.Creator.IsLocal)
            return;

        GorillaIK ik = __instance.GetComponent<GorillaIK>();
        if (ik == null)
            return;

        ik.usingUpdatedIK = true;

        ApplyTrackedElbow(
            ik,
            __instance,
            plugin,
            true
        );

        ApplyTrackedElbow(
            ik,
            __instance,
            plugin,
            false
        );
    }

    private static void ApplyTrackedElbow(
        GorillaIK ik,
        VRRig rig,
        Plugin plugin,
        bool isLeft)
    {
        GameObject elbowObject =
            isLeft
                ? plugin.LeftElbowObject
                : plugin.RightElbowObject;

        if (elbowObject == null)
            return;

        Transform shoulderParent =
            isLeft
                ? ik.leftUpperArm?.parent
                : ik.rightUpperArm?.parent;

        if (shoulderParent == null)
            return;

        Quaternion trackerRot =
            elbowObject.transform.localRotation *
            (isLeft
                ? plugin.LeftElbowOffset
                : plugin.RightElbowOffset);

        Vector3 elbowDir =
            shoulderParent.InverseTransformDirection(
                elbowObject.transform.parent.TransformDirection(
                    trackerRot * Vector3.down
                )
            );

        if (isLeft)
        {
            ik.leftElbowDirection = elbowDir;
            ik.lerpLeftElbowDirection = elbowDir;
        }
        else
        {
            ik.rightElbowDirection = elbowDir;
            ik.lerpRightElbowDirection = elbowDir;
        }
    }
}