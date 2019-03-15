using System;
using System.Reflection;
using Harmony;
using UnityEngine;

namespace StorageInfo
{
    public class Entry
    {
        private static void InitializeHarmony()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("dingo.storageinfo");

#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif

            MethodInfo containerOnHandHover = AccessTools.Method(typeof(StorageContainer), nameof(StorageContainer.OnHandHover));

            // Removes original SetInteractText and injects SetCustomInteractText
            harmony.Patch(
                original: containerOnHandHover,
                prefix: null,
                postfix: null,
                transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_StorageContainer_OnHandHover_Transpiler)));
        }

        public static void Initialize()
        {
            try
            {
                InitializeHarmony();
            }

            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
                Debug.Log("[StorageInfo] :: Error during mod initialization!");
            }
        }
    }
}
