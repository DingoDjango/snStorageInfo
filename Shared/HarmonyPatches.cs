using HarmonyLib;
using Nautilus.Utility;
using UnityEngine;

namespace StorageInfo
{
    public class HarmonyPatches
    {
        private static StorageContainer hoveredStorage;

        // Reticle text dirty-flag: vanilla StorageContainer.OnHandHover fires every frame,
        // so the full container slot scan would otherwise run per frame while hovering.
        private static ItemsContainer subscribedContainer;
        private static bool textDirty = true;
        private static DisplayMode lastAppliedMode;

        // Only re-validate the hover target when the active target object changes.
        private static GameObject lastValidatedTarget;

        // The game is not interactive until the load screen (WaitScreen) is dismissed.
        // Save/new-game loading runs hover logic against a half-built world, so gate
        // the patches on it. OnHandHover fires every frame while looking at a
        // container, so the first frame after the load screen clears picks the hover
        // back up automatically - no re-trigger needed.
        private static bool IsGameInteractive()
        {
            return !WaitScreen.IsWaiting;
        }

        private static void Patch_OnHandHover_Postfix(StorageContainer __instance)
        {
            if (!IsGameInteractive())
            {
                return;
            }

            if (__instance == null || __instance.container == null)
            {
                return;
            }

            hoveredStorage = __instance;
            SubscribeToContainer(__instance.container);
            SetCustomInteractText(__instance.container);
        }

        private static void Patch_OnHandClick_Prefix(StorageContainer __instance)
        {
            hoveredStorage = null;
            UnsubscribeFromContainer();
            StorageDetailUI.Hide();
        }

        private static void Patch_GUIHand_OnUpdate_Postfix()
        {
            if (!IsGameInteractive())
            {
                return;
            }

            if (hoveredStorage == null)
            {
                return;
            }

            if (Player.main == null || Player.main.guiHand == null)
            {
                hoveredStorage = null;
                UnsubscribeFromContainer();
                StorageDetailUI.Hide();
                return;
            }

            GameObject activeTarget = Player.main.guiHand.GetActiveTarget();

            if (activeTarget != lastValidatedTarget)
            {
                lastValidatedTarget = activeTarget;

                if (!IsHoveringStorage(hoveredStorage, activeTarget))
                {
                    hoveredStorage = null;
                    UnsubscribeFromContainer();
                    StorageDetailUI.Hide();
                    return;
                }
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
                string text = freeSlots == 1
                    ? "ContainerOneItemOneSlotFree".TryFormatTranslate()
                    : "ContainerOneItemSlotsFree".TryFormatTranslate(freeSlots);
                return text ?? "ContainerOneItem".Translate();
            }

            string text2 = freeSlots == 1
                ? "ContainerNonemptyOneSlotFree".TryFormatTranslate(itemStorage.count)
                : "ContainerNonemptySlotsFree".TryFormatTranslate(itemStorage.count, freeSlots);
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

        // --- Reticle text dirty-flag via vanilla ItemsContainer events ---

        private static void SubscribeToContainer(ItemsContainer container)
        {
            if (subscribedContainer == container)
            {
                return;
            }

            UnsubscribeFromContainer();

            subscribedContainer = container;

            if (container == null)
            {
                return;
            }

            container.onAddItem += OnContainerChanged;
            container.onRemoveItem += OnContainerChanged;
            container.onChangeItemPosition += OnContainerChanged;
            container.onResize += OnContainerResized;
            textDirty = true;
        }

        private static void UnsubscribeFromContainer()
        {
            if (subscribedContainer == null)
            {
                return;
            }

            subscribedContainer.onAddItem -= OnContainerChanged;
            subscribedContainer.onRemoveItem -= OnContainerChanged;
            subscribedContainer.onChangeItemPosition -= OnContainerChanged;
            subscribedContainer.onResize -= OnContainerResized;
            subscribedContainer = null;
            textDirty = true;
        }

        private static void OnContainerChanged(InventoryItem item)
        {
            textDirty = true;
        }

        private static void OnContainerResized(int width, int height)
        {
            textDirty = true;
        }

        // Clears scene-sensitive hover state on scene unload so stale references from
        // the previous scene can't linger (e.g. lastValidatedTarget/hoveredStorage
        // pointing at destroyed objects) and re-opens the reticle dirty-flag gate.
        internal static void ResetSceneState()
        {
            hoveredStorage = null;
            UnsubscribeFromContainer();
            subscribedContainer = null;
            lastValidatedTarget = null;
            textDirty = true;
            lastAppliedMode = (DisplayMode)(-1);
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
            if (!textDirty && ModPlugin.options.DisplayMode == lastAppliedMode)
            {
                return;
            }

            textDirty = false;
            lastAppliedMode = ModPlugin.options.DisplayMode;

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
    }
}
