using HarmonyLib;

namespace CrusherCoreBoost.Patches
{
    /// <summary>
    /// Patches the TechTreeState to handle computing core cluster bonuses when the new 'Core Boost (Crusher)' unlock is active.
    /// </summary>
    [HarmonyPatch(typeof(TechTreeState))]
    public class TechTreeStatePatches
    {
        internal static float freeCoresCrushing = 0f;
        internal static float crusherSpeedMultiplier = 1f;

#if DEBUG
        internal static float lastFreeCoresCrushing = -1f;
        internal static float lastCrusherSpeedMultiplier = -1f;
#endif

        [HarmonyPatch(nameof(TechTreeState.ResetAtStartOfFrame))]
        [HarmonyPrefix]
        public static void ResetAtStartOfFrame_Prefix()
        {
            // For other machines, there is a base speed upgrade through Tech Tree unlocks.
            // Crushers don't have upgrades like this so the base speed multiplier will always be 1.
            // If a mod is created to add progressive upgrades to Crusher base speeds, this will need to be rethought to coexist.
            crusherSpeedMultiplier = 1.0f + freeCoresCrushing;

#if DEBUG
            if (lastCrusherSpeedMultiplier != crusherSpeedMultiplier)
            {
                lastCrusherSpeedMultiplier = crusherSpeedMultiplier;
                CrusherCoreBoostPlugin.Instance.SharedLogger.LogInfo($"crusherSpeedMultiplier updated to {crusherSpeedMultiplier}");
            }
#endif
        }

        [HarmonyPatch(nameof(TechTreeState.HandleEndOfFrame))]
        [HarmonyPostfix]
        public static void HandleEndOfFrame_Postfix()
        {
            if(TechTreeState.instance.IsUnlockActive(CrusherCoreBoostPlugin.UnlockId))
            {
                // TechTreeState.freeCores was updated its HandleEndOfFrame so it will be current here.
                freeCoresCrushing = (float)TechTreeState.instance.freeCores * 0.001f;

#if DEBUG
                if (freeCoresCrushing != lastFreeCoresCrushing)
                {
                    lastFreeCoresCrushing = freeCoresCrushing;
                    CrusherCoreBoostPlugin.Instance.SharedLogger.LogInfo($"freeCoresCrushing updated to {freeCoresCrushing}.");
                }
#endif
            }
            else
            {
                freeCoresCrushing = 0f;
            }
        }
    }
}
