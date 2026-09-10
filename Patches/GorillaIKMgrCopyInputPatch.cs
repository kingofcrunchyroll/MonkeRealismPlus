using HarmonyLib;
using MonkeRealism;
using MonkeRealism.Core;
using UnityEngine;

[HarmonyPatch(typeof(GorillaIKMgr), "CopyInput")]
internal static class GorillaIKMgrCopyInputPatch
{
    private static void Prefix()
    {
        var plugin = Plugin.Instance;
        if (plugin == null)
            return;

        var rig = VRRig.LocalRig;
        if (rig == null)
            return;

        var gorillaIK = rig.GetComponent<GorillaIK>();
        if (gorillaIK == null)
            return;

        if (plugin.ShouldUseTracker.Value)
        {
            FeedBody(gorillaIK, plugin);
        }

        if (plugin.ShouldUseElbowTracking.Value)
        {
            FeedElbow(gorillaIK, plugin, rig, true);
            FeedElbow(gorillaIK, plugin, rig, false);
        }
    }

    private static void FeedBody(GorillaIK gorillaIK, Plugin plugin)
    {
        if (plugin.TrackerFollower == null)
            return;

        if (gorillaIK.bodyBone == null || gorillaIK.bodyBone.parent == null)
            return;

        Quaternion trackerRotation = plugin.TrackerFollower.transform.rotation;

        Quaternion bodyRotation =
            Quaternion.Inverse(gorillaIK.bodyBone.parent.rotation) * trackerRotation;

        gorillaIK.targetBodyRot = bodyRotation;
        gorillaIK.lerpBodyRot = bodyRotation;

        // GetShoulderLocalTargetPos_Left/Right read from this when usingUpdatedIK is true.
        // Vanilla only updates it from real OVRSkeleton data, which we're not feeding —
        // so we have to drive it ourselves or it stays frozen and hands drift with yaw.
        if (gorillaIK.projectedBodyRotation != null)
        {
            gorillaIK.projectedBodyRotation.localRotation = bodyRotation;
        }
    }

    private static void FeedElbow(
        GorillaIK gorillaIK,
        Plugin plugin,
        VRRig rig,
        bool isLeft)
    {
        var elbowObject =
            isLeft
                ? plugin.LeftElbowObject
                : plugin.RightElbowObject;

        if (elbowObject == null)
            return;

        Quaternion trackerRot =
            elbowObject.transform.localRotation;

        trackerRot *=
            isLeft
                ? plugin.LeftElbowOffset
                : plugin.RightElbowOffset;

        Transform shoulderParent =
            isLeft
                ? gorillaIK.leftUpperArm?.parent
                : gorillaIK.rightUpperArm?.parent;

        if (shoulderParent == null)
            return;

        Vector3 elbowDir =
            shoulderParent.InverseTransformDirection(
                elbowObject.transform.parent.TransformDirection(
                    trackerRot * Vector3.down
                )
            );

        if (isLeft)
        {
            gorillaIK.leftElbowDirection = elbowDir;
            gorillaIK.lerpLeftElbowDirection = elbowDir;
        }
        else
        {
            gorillaIK.rightElbowDirection = elbowDir;
            gorillaIK.lerpRightElbowDirection = elbowDir;
        }
    }
}