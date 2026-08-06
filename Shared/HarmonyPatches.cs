using System;
using HarmonyLib;
using Nautilus.Utility;
using UnityEngine;

namespace StorageInfo
{
    public class HarmonyPatches
    {
        private static StorageContainer hoveredStorage;

        // Reticle text dirty-flag via vanilla ItemsContainer events.
        private static ItemsContainer subscribedContainer;
        private static bool textDirty = true;
        private static DisplayMode lastAppliedMode;

        private static GameObject lastValidatedTarget;

        // The game is not interactive until WaitScreen (loading) is dismissed.
        // OnHandHover fires every frame while looking at a container. First frame after the load screen picks the hover back up.
        private static bool IsGameInteractive()
        {
            return !WaitScreen.IsWaiting;
        }

        // Mirrors vanilla StorageContainer.OnHandHover/OnHandClick
        private static bool IsStorageInteractable(StorageContainer storage)
        {
            if (storage == null || !storage.enabled)
            {
                return false;
            }

            Constructable constructable = storage.GetComponent<Constructable>();
            return constructable == null || constructable.constructed;
        }

        private static void Patch_OnHandHover_Postfix(StorageContainer __instance)
        {
            if (!IsGameInteractive())
            {
                return;
            }

            if (__instance == null || __instance.container == null || !IsStorageInteractable(__instance))
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

            if (ModPlugin.options.PreviewUI && hoveredStorage.container != null && IsStorageInteractable(hoveredStorage))
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
                string oneItemText = freeSlots == 1
                    ? "ContainerOneItemOneSlotFree".TryFormatTranslate()
                    : "ContainerOneItemSlotsFree".TryFormatTranslate(freeSlots);
                return oneItemText ?? "ContainerOneItem".Translate();
            }

            string nonemptyText = freeSlots == 1
                ? "ContainerNonemptyOneSlotFree".TryFormatTranslate(itemStorage.count)
                : "ContainerNonemptySlotsFree".TryFormatTranslate(itemStorage.count, freeSlots);
            return nonemptyText ?? "ContainerNonempty".FormatTranslate(itemStorage.count);
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

        // Clears scene-sensitive state on unload.
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
            try
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
            catch (Exception ex)
            {
                ModPlugin.LogMessage($"Harmony patch initialization FAILED: {ex}");
            }
        }

        public static void SetCustomInteractText(ItemsContainer itemStorage)
        {
            // Reticle text must be re-applied every frame, no dirty flag here
            // Vanilla StorageContainer.OnHandHover sets HandSubscript to "" each frame, which would otherwise wipe our text
            string customSubscriptText = string.Empty;

            if (itemStorage != null)
            {
                switch (ModPlugin.options.DisplayMode)
                {
                    case DisplayMode.DisplayModeDefault:
                        customSubscriptText = GetDefaultDisplayText(itemStorage);
                        break;
                    case DisplayMode.DisplayModeSlotsOnly:
                        customSubscriptText = GetSlotsOnlyDisplayText(itemStorage);
                        break;
                }
            }

            if (HandReticle.main != null)
            {
                HandReticle.main.SetText(HandReticle.TextType.HandSubscript, customSubscriptText, false);
            }

            if (!textDirty && ModPlugin.options.DisplayMode == lastAppliedMode)
            {
                return;
            }

            textDirty = false;
            lastAppliedMode = ModPlugin.options.DisplayMode;

            if (itemStorage == null)
            {
                StorageDetailUI.Hide();
            }
            else if (ModPlugin.options.PreviewUI && IsStorageInteractable(hoveredStorage))
            {
                StorageDetailUI.Show(itemStorage);
            }
            else
            {
                StorageDetailUI.Hide();
            }
        }
    }
}
