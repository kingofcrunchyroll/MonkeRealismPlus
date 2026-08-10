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
        if (plugin == null || !plugin.ShouldUseElbowTracking.Value) return;

        var rig = VRRig.LocalRig;
        if (rig == null) return;

        var gorillaIK = rig.GetComponent<GorillaIK>();
        if (gorillaIK == null) return;

        // === FORCE THE IK MODE ===
        gorillaIK.usingUpdatedIK = true;
        gorillaIK.canUseUpdatedIK = true;

        if (GorillaIKMgr.playerIK == gorillaIK)
            GorillaIKMgr.playerIK.usingUpdatedIK = true;

        var scale = rig.scaleFactor;
        var bodyRot = plugin.TrackerFollower?.transform.rotation ?? rig.transform.rotation;

        Vector3? chestPos = plugin.TrackerObject?.transform.position;
        Quaternion? chestRot = plugin.TrackerObject?.transform.rotation;

        FeedElbow(gorillaIK, plugin, rig, scale, bodyRot, chestPos, chestRot, true);
        FeedElbow(gorillaIK, plugin, rig, scale, bodyRot, chestPos, chestRot, false);
    }

    private static void FeedElbow(GorillaIK gorillaIK, Plugin plugin, VRRig rig,
    float scale, Quaternion bodyRot, Vector3? chestPos, Quaternion? chestRot, bool isLeft)
    {
        var elbowObject = isLeft ? plugin.LeftElbowObject : plugin.RightElbowObject;
        if (elbowObject == null) return;

        // Read localRotation from the turnParent-childed GameObject
        // This automatically handles smooth/snap turning the same way the waist tracker does
        Quaternion trackerRot = elbowObject.transform.localRotation;

        // Apply user offset
        trackerRot = trackerRot * (isLeft ? plugin.LeftElbowOffset : plugin.RightElbowOffset);

        var shoulderParent = isLeft
            ? gorillaIK.leftUpperArm?.parent
            : gorillaIK.rightUpperArm?.parent;

        if (shoulderParent == null) return;

        // Transform from turnParent local space into shoulder parent space
        Vector3 elbowDir = shoulderParent.InverseTransformDirection(
            elbowObject.transform.parent.TransformDirection(
                trackerRot * Vector3.down)); // replace Vector3.down with whatever axis you found works

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

    private static Vector3 GetHandWorldPosition(VRRig rig, bool isLeft)
    {
        var hand = isLeft ? rig.leftHand : rig.rightHand;
        return hand.overrideTarget != null ? hand.overrideTarget.position :
               hand.rigTarget != null ? hand.rigTarget.position :
               rig.transform.TransformPoint(hand.syncPos);
    }

    // Simple but effective elbow direction solver
    private static Vector3 CalculateElbowDirection(Vector3 shoulder, Vector3 hand, Quaternion trackerRot)
    {
        Vector3 armVector = hand - shoulder;
        float armLength = armVector.magnitude;

        if (armLength < 0.01f) return Vector3.forward;

        Vector3 midPoint = shoulder + armVector * 0.5f;

        // Pull elbow slightly toward tracker forward direction
        Vector3 trackerForward = trackerRot * Vector3.forward;
        Vector3 elbowTarget = midPoint + trackerForward * (armLength * 0.35f);

        Vector3 elbowDir = (elbowTarget - shoulder).normalized;

        return elbowDir;
    }
}