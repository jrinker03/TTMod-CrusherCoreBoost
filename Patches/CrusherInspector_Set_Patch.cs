using HarmonyLib;
using System.Collections.Generic;

namespace CrusherCoreBoost.Patches
{
    /// <summary>
    /// Transpiler to patch CrusherInspector.Set to reflect any core boost bonuses
    /// </summary>
    [HarmonyPatch(typeof(CrusherInspector))]
    [HarmonyPatch("Set")]
    internal class CrusherInspector_Set_Patch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // There are three places where we need to multiply by TechTreeStatePatches.crusherSpeedMultiplier:
            // 1) When computing the input speeds:
            //     resourceRow.speed.SetLocFormatString("{0}/min", (float)currentRecipe.runtimeIngQuantities[k] / currentRecipe.duration * 60f);
            //
            // 2) When computing the output speeds:
            //     resourceRow2.speed.SetLocFormatString("{0}/min", (float)currentRecipe.outputQuantities[l] / currentRecipe.duration * 60f);
            //
            // 3) When computing the generated power/min
            //     float num3 = (float)num2 / currentRecipe.duration * 60f;


#if DEBUG
            CrusherCoreBoostPlugin.Instance.LogIl(instructions);
#endif
            return instructions;
        }
    }
}
