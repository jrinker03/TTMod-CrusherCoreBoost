using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CrusherCoreBoost.Patches
{
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
            //  We want to change it to execute this instead: progress += dt * crusherSpeedMultiplier / currentRecipe.duration;

            // Since it's a nice landmark, search for Label 6.  Once we find it, ensure the opcodes at and following are as above.  If not, don't update.

            CodeMatcher codeMatcher = new CodeMatcher(instructions).Start();
            bool foundInsertPoint = false;

            while (codeMatcher.IsValid && 
                codeMatcher.Instruction.labels != null && 
                (codeMatcher.Instruction.labels.Count == 0 || 
                 codeMatcher.Instruction.labels[0].GetHashCode() != 6))
            {
                codeMatcher.Advance(1);
            }

            if (codeMatcher.IsValid && codeMatcher.Instruction.opcode == OpCodes.Ldarg_0)
            {
                codeMatcher.Advance(1);

                if (codeMatcher.IsValid && codeMatcher.Instruction.opcode == OpCodes.Ldarg_0)
                {
                    codeMatcher.Advance(1);
                    if (codeMatcher.IsValid && codeMatcher.Instruction.opcode == OpCodes.Ldfld)
                    {
                        codeMatcher.Advance(1);
                        if (codeMatcher.IsValid && codeMatcher.Instruction.opcode == OpCodes.Ldarg_1)
                        {
                            codeMatcher.Advance(1);
                            // At this point, we're very likely where we want to update the code.
                            foundInsertPoint = true;


                            FieldInfo fieldInfo = typeof(TechTreeStatePatches).GetField(nameof(TechTreeStatePatches.crusherSpeedMultiplier), BindingFlags.NonPublic | BindingFlags.Static);

                            CodeInstruction[] newInstructions = new CodeInstruction[2];
                            newInstructions[0] = new CodeInstruction(OpCodes.Ldsfld, fieldInfo);
                            newInstructions[1] = new CodeInstruction(OpCodes.Mul);

                            codeMatcher.Insert(newInstructions);
                        }
                    }
                }
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

#if DEBUG
            CrusherCoreBoostPlugin.Instance.LogIl(instructions);
#endif

            return instructions;
        }
    }
}
