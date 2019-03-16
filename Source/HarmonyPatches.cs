using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;

namespace StorageInfo
{
    public class HarmonyPatches
    {
        /* IL from StorageContainer.OnHandHover(GUIHand hand):
         * IL_0019: call      bool [UnityEngine]UnityEngine.Object::op_Implicit(class [UnityEngine]UnityEngine.Object)
         * IL_001E: brfalse   IL_002E
         * IL_0023: ldloc.0
         * IL_0024: callvirt  instance bool Constructable::get_constructed()
         * IL_0029: brfalse   IL_0068
         * IL_002E: ldsfld    class HandReticle HandReticle::main
         * First IL index we snip ---> IL_0033: ldarg.0
         * IL_0034: ldfld     string StorageContainer::hoverText
         * IL_0039: ldarg.0
         * IL_003A: call      instance bool StorageContainer::IsEmpty()
         * IL_003F: brfalse   IL_004E
         * IL_0044: ldstr     "Empty"
         * IL_0049: br        IL_0053
         * IL_004E: ldsfld    string [mscorlib]System.String::Empty
         * IL_0053: callvirt  instance void HandReticle::SetInteractText(string, string)
         * Last IL index we snip, folding the IL nicely on itself ---> IL_0058: ldsfld    class HandReticle HandReticle::main */
        private static IEnumerable<CodeInstruction> Patch_StorageContainer_OnHandHover_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo handleReticleMain = AccessTools.Field(typeof(HandReticle), nameof(HandReticle.main));
            MethodInfo setInteractText = AccessTools.Method(typeof(HandReticle), nameof(HandReticle.SetInteractText), new Type[] { typeof(string), typeof(string) });
            MethodInfo callCustomInteractText = AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.SetCustomInteractText));

            if (handleReticleMain == null || setInteractText == null || callCustomInteractText == null)
            {
                Debug.Log("[StorageInfo] :: Something went wrong while populating transpiler variables. Aborting.");

                return instructions;
            }

            List<CodeInstruction> codes = instructions.ToList();
            bool foundFirstHandReticle = false;
            int firstIndexToRemove = -1;
            int lastIndexToRemove = -1;

            // Find the IL we need to replace
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction instruction = codes[i];

                // IL_0033 is the first IL we aim to replace
                if (!foundFirstHandReticle)
                {
                    if (instruction.opcode == OpCodes.Ldsfld && instruction.operand == handleReticleMain
                        && codes[i + 1].opcode == OpCodes.Ldarg_0)
                    {
                        // Replace Ldarg_0 IL because the first HandleReticle::main is used as an IL label
                        firstIndexToRemove = i + 1;

                        foundFirstHandReticle = true;
                    }
                }

                // IL_0058 is the final IL we aim to replace
                else if (instruction.opcode == OpCodes.Ldsfld && instruction.operand == handleReticleMain
                    && codes[i - 1].opcode == OpCodes.Callvirt && codes[i - 1].operand == setInteractText)
                {
                    lastIndexToRemove = i;

                    break;
                }
            }

            // If all goes well, we now have a range of IL to remove
            if (firstIndexToRemove != -1 && lastIndexToRemove != -1)
            {
                codes.RemoveRange(firstIndexToRemove, lastIndexToRemove - firstIndexToRemove + 1); // Account for zero-based indexing

                // Insert custom text interaction
                codes.Insert(codes.Count - 1, new CodeInstruction(OpCodes.Ldarg_0, null));
                codes.Insert(codes.Count - 1, new CodeInstruction(OpCodes.Call, callCustomInteractText));

                return codes;
            }

            else
            {
                Debug.Log("[StorageInfo] :: Patch error - could not find IL index to modify! Mod has not been enabled.");
                Debug.Log($"[StorageInfo] :: emptyCheckIndex = {firstIndexToRemove.ToString()}, stringEmptyIndex = {lastIndexToRemove.ToString()}");
            }

            return instructions;
        }

        private static void Patch_SetCurrentLanguage_Postfix()
        {
            Translation.ReloadLanguage();
        }

        internal static void InitializeHarmony()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("dingo.storageinfo");

#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif

            MethodInfo containerOnHandHover = AccessTools.Method(typeof(StorageContainer), nameof(StorageContainer.OnHandHover));
            MethodInfo setLanguage = AccessTools.Method(typeof(Language), nameof(Language.SetCurrentLanguage));

            // Remove original SetInteractText and inject SetCustomInteractText
            harmony.Patch(
                original: containerOnHandHover,
                prefix: null,
                postfix: null,
                transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_StorageContainer_OnHandHover_Transpiler)));

            // Reset language cache upon language change
            harmony.Patch(
                original: setLanguage,
                prefix: null,
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_SetCurrentLanguage_Postfix)),
                transpiler: null);
        }

        public static void SetCustomInteractText(StorageContainer _storage)
        {
            if (_storage != null)
            {
                string customInfoText = string.Empty;
                ItemsContainer container = _storage.container;

                if (container != null)
                {
                    if (container.count <= 0) // replace with container.IsEmpty()
                    {
                        customInfoText = "ContainerEmpty".Translate();
                    }

                    else if (container.count == 1)
                    {
                        customInfoText = "ContainerOneItem".Translate();
                    }

                    else if (!container.HasRoomFor(1, 1)) // replace with container.IsFull()
                    {
                        customInfoText = "ContainerFull".Translate();
                    }

                    else
                    {
                        customInfoText = "ContainerNonempty".FormatSingle(container.count.ToString());
                    }
                }

                HandReticle.main.SetInteractText(_storage.hoverText, customInfoText, true, false, HandReticle.Hand.Left); // From HandReticle.SetInteractText(string, string)                
            }
        }
    }
}
