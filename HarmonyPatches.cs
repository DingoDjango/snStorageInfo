using HarmonyLib;
using SMLHelper.V2.Utility;

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

        private static void Patch_SetCurrentLanguage_Postfix()
        {
            Translation.ReloadLanguage();
        }

        internal static void InitializeHarmony()
        {
            Harmony harmony = new Harmony("Dingo.Harmony.StorageInfo");

            /* Remove original SetInteractText and inject SetCustomInteractText */
            // Patch: StorageContainer.OnHandHover
            harmony.Patch(
                original: AccessTools.Method(typeof(StorageContainer), nameof(StorageContainer.OnHandHover)),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_OnHandHover_Postfix)));

            /* Reset language cache on game language change */
            // Patch: Language.SetCurrentLanguage
            harmony.Patch(
                original: AccessTools.Method(typeof(Language), nameof(Language.SetCurrentLanguage)),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_SetCurrentLanguage_Postfix)));
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

                else
                {
                    customSubscriptText = "ContainerNonempty".FormatTranslate(itemStorage.count.ToString());
                }
            }

            HandReticle.main.SetText(HandReticle.TextType.HandSubscript, customSubscriptText, false);
        }
    }
}
