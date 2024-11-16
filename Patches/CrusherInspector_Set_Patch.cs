using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

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
            //
            // In all three cases, we can take advantage of the pattern "/ currentRecipe.duration * 60f" and insert the multiplier before that.
            int matches = 0;

            CodeMatcher codeMatcher = new CodeMatcher(instructions).Start();

            FieldInfo durationFieldInfo = typeof(SchematicsRecipeData).GetField(nameof(SchematicsRecipeData.duration));
            CodeMatch[] toMatch = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld, durationFieldInfo),
                new CodeMatch(OpCodes.Div),
                new CodeMatch(OpCodes.Ldc_R4, 60f),
                new CodeMatch(OpCodes.Mul),
            };

            FieldInfo fieldInfo = typeof(TechTreeStatePatches).GetField(nameof(TechTreeStatePatches.crusherSpeedMultiplier), BindingFlags.NonPublic | BindingFlags.Static);
            CodeInstruction[] newInstructions = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldsfld, fieldInfo),
                new CodeInstruction(OpCodes.Mul),
            };

            codeMatcher.MatchForward(false, toMatch).Repeat(matchAction: cm => {
                    cm.InsertAndAdvance(newInstructions);
                    cm.Advance(toMatch.Length);
                    matches++;
                });

            // At the moment, the code has exactly three patterns and insertion points so ensure that we made the expected number of updates.
            if (matches == 3)
            {
                CrusherCoreBoostPlugin.Instance.SharedLogger.LogInfo("CrusherInspector.Set() updated.");
                instructions = codeMatcher.Instructions();
            }
            else
            {
                CrusherCoreBoostPlugin.Instance.SharedLogger.LogError("Unable to update CrusherInspector.Set(). The code has likely changed.");
            }

            return instructions;
        }
    }
}
