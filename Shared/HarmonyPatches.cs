using HarmonyLib;
using Nautilus.Utility;

namespace StorageInfo
{
    public class HarmonyPatches
    {
        private static void Patch_OnHandHover_Postfix(StorageContainer __instance)
        {
            ItemsContainer itemStorage = __instance.container;

            if (itemStorage != null)
            {
                SetCustomInteractText(itemStorage);
            }
        }

        internal static void InitializeHarmony()
        {
            Harmony harmony = new Harmony("Dingo.Harmony.StorageInfo");

            /* Remove original SetInteractText and inject SetCustomInteractText */
            // Patch: StorageContainer.OnHandHover
            harmony.Patch(
                original: AccessTools.Method(typeof(StorageContainer), nameof(StorageContainer.OnHandHover)),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_OnHandHover_Postfix)));
        }

        public static void SetCustomInteractText(ItemsContainer itemStorage)
        {
            string customSubscriptText = string.Empty;

            if (itemStorage != null)
            {
                if (itemStorage.IsEmpty())
                {
                    customSubscriptText = "ContainerEmpty".Translate();
                }

                else if (itemStorage.IsFull())
                {
                    customSubscriptText = "ContainerFull".Translate();
                }

                else if (itemStorage.count == 1)
                {
                    customSubscriptText = "ContainerOneItem".Translate();
                }

                else
                {
                    customSubscriptText = "ContainerNonempty".FormatTranslate(itemStorage.count.ToString());
                }
            }

            HandReticle.main.SetText(HandReticle.TextType.HandSubscript, customSubscriptText, false);
        }
    }
}
