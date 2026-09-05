using GorillaTagScripts;
using HarmonyLib;

namespace MonkeRealismPlus.Patches
{
    [HarmonyPatch(typeof(SubscriptionManager), nameof(SubscriptionManager.GetSubscriptionSettingBool))]
    internal static class SubscriptionFeaturePatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            SubscriptionManager.SubscriptionFeatures feature,
            ref bool __result)
        {
            if (feature == SubscriptionManager.SubscriptionFeatures.IOBT)
            {
                __result = true;
            }
        }
    }
}