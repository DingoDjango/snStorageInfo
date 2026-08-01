using HarmonyLib;
using Nautilus.Utility;
using UnityEngine;

namespace StorageInfo
{
    public class HarmonyPatches
    {
        public static StorageContainer hoveredStorage;

            private static void Patch_OnHandHover_Postfix(StorageContainer __instance)
        {
            if (__instance == null || __instance.container == null)
            {
                return;
            }

            hoveredStorage = __instance;
            SetCustomInteractText(__instance.container);
        }

        private static void Patch_OnHandClick_Prefix(StorageContainer __instance)
        {
            hoveredStorage = null;
            StorageDetailUI.Hide();
        }

        private static void Patch_GUIHand_OnUpdate_Postfix()
        {
            if (hoveredStorage == null)
            {
                return;
            }

            if (Player.main == null || Player.main.guiHand == null)
            {
                hoveredStorage = null;
                StorageDetailUI.Hide();
                return;
            }

            GameObject activeTarget = Player.main.guiHand.GetActiveTarget();

            if (!IsHoveringStorage(hoveredStorage, activeTarget))
            {
                hoveredStorage = null;
                StorageDetailUI.Hide();
                return;
            }

            if (ModPlugin.options.DisplayMode == DisplayMode.DisplayModeDetailedList && hoveredStorage.container != null)
            {
                StorageDetailUI.Tick(hoveredStorage.container);
            }
            else
            {
                StorageDetailUI.Hide();
            }
        }

        internal static void InitializeHarmony()
        {
            Harmony harmony = new Harmony("Dingo.Harmony.StorageInfo");

            harmony.Patch(
                original: AccessTools.Method(typeof(StorageContainer), nameof(StorageContainer.OnHandHover)),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_OnHandHover_Postfix)));

            harmony.Patch(
                original: AccessTools.Method(typeof(StorageContainer), "OnHandClick"),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_OnHandClick_Prefix)));

            harmony.Patch(
                original: AccessTools.Method(typeof(GUIHand), "OnUpdate"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_GUIHand_OnUpdate_Postfix)));
        }

        public static void SetCustomInteractText(ItemsContainer itemStorage)
        {
            string customSubscriptText = string.Empty;

            if (itemStorage == null)
            {
                HandReticle.main.SetText(HandReticle.TextType.HandSubscript, customSubscriptText, false);
                return;
            }

            switch (ModPlugin.options.DisplayMode)
            {
                case DisplayMode.DisplayModeDefault:
                    customSubscriptText = GetDefaultDisplayText(itemStorage);
                    StorageDetailUI.Hide();
                    break;
                case DisplayMode.DisplayModeSlotsOnly:
                    customSubscriptText = GetSlotsOnlyDisplayText(itemStorage);
                    StorageDetailUI.Hide();
                    break;
                case DisplayMode.DisplayModeDetailedList:
                    customSubscriptText = GetSlotsOnlyDisplayText(itemStorage);
                    StorageDetailUI.Show(itemStorage);
                    break;
            }

            HandReticle.main.SetText(HandReticle.TextType.HandSubscript, customSubscriptText, false);
        }

        private static string GetDefaultDisplayText(ItemsContainer itemStorage)
        {
            if (itemStorage.IsEmpty())
            {
                return "ContainerEmpty".Translate();
            }

            int freeSlots = StorageSlotInfo.GetFreeSlotCount(itemStorage);

            if (freeSlots == 0)
            {
                return "ContainerFull".Translate();
            }

            if (itemStorage.count == 1)
            {
                string text = "ContainerOneItemSlotsFree".TryFormatTranslate(freeSlots);
                return text ?? "ContainerOneItem".Translate();
            }

            string text2 = "ContainerNonemptySlotsFree".TryFormatTranslate(itemStorage.count, freeSlots);
            return text2 ?? "ContainerNonempty".FormatTranslate(itemStorage.count);
        }

        private static string GetSlotsOnlyDisplayText(ItemsContainer itemStorage)
        {
            int totalSlots = StorageSlotInfo.GetTotalSlotCount(itemStorage);
            int usedSlots = StorageSlotInfo.GetUsedSlotCount(itemStorage);
            return $"{usedSlots}/{totalSlots} {"SlotsOccupied".Translate()}";
        }

        private static bool IsHoveringStorage(StorageContainer storage, GameObject target)
        {
            if (storage == null || target == null)
            {
                return false;
            }

            StorageContainer targetStorage = target.GetComponentInParent<StorageContainer>();
            return targetStorage == storage;
        }
    }
}
