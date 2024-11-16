using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CrusherCoreBoost.Patches
{
    /// <summary>
    /// Transpiler to patch CrusherInstance.UpdateCrafting to add a potential bonus for core clusters.
    /// </summary>
    [HarmonyPatch(typeof(CrusherInstance))]
    [HarmonyPatch("UpdateCrafting")]
    public class CrusherInstance_UpdateCrafting_Patch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // The code we want to change is: progress += dt / currentRecipe.duration;
            // In IL this is currently:
            /*
                ldarg.0 [Label6]
                ldarg.0
                ldfld float32 CrusherInstance::progress
                ldarg.1
                ldarg.0
                ldfld class SchematicsRecipeData CrusherInstance::currentRecipe
                ldfld float32 SchematicsRecipeData::duration
                div
                add
                stfld float32 CrusherInstance::progress
            */
            //  We want to change it to execute this instead: progress += dt * TechTreeStatePatches.crusherSpeedMultiplier / currentRecipe.duration;

            CodeMatcher codeMatcher = new CodeMatcher(instructions).Start();
            bool foundInsertPoint = false;

            FieldInfo progressFieldInfo = typeof(CrusherInstance).GetField(nameof(CrusherInstance.progress));
            CodeMatch[] toMatch = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, progressFieldInfo),
                new CodeMatch(OpCodes.Ldarg_1),
            };

            codeMatcher.MatchForward(true, toMatch).Advance(1);

            if (codeMatcher.IsValid)
            { 
                // At this point, we're very likely where we want to update the code.
                foundInsertPoint = true;

                FieldInfo fieldInfo = typeof(TechTreeStatePatches).GetField(nameof(TechTreeStatePatches.crusherSpeedMultiplier), BindingFlags.NonPublic | BindingFlags.Static);
                CodeInstruction[] newInstructions = new CodeInstruction[]
                {
                    new CodeInstruction(OpCodes.Ldsfld, fieldInfo),
                    new CodeInstruction(OpCodes.Mul),
                };

                codeMatcher.Insert(newInstructions);
            }

            if (foundInsertPoint)
            {
                CrusherCoreBoostPlugin.Instance.SharedLogger.LogInfo("CrusherInstance.UpdateCrafting() updated.");
                instructions = codeMatcher.Instructions();
            }
            else
            {
                CrusherCoreBoostPlugin.Instance.SharedLogger.LogError("Unable to update CrusherInstance.UpdateCrafting(). The code has likely changed.");
            }

            return instructions;
        }
    }
}
