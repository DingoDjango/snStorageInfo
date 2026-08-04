using HarmonyLib;
using Nautilus.Utility;
using UnityEngine;
using UnityEngine.UI;

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

        // Fires when the PDA storage screen binds a container (storage opened through
        // the PDA). The vanilla uGUI_ItemsContainer (background + grid) only exists in
        // the hierarchy while the PDA storage screen is active, so this is the only
        // reliable moment to dump its asset names.
        private static void Patch_ItemsContainerInit_Postfix(uGUI_ItemsContainer __instance)
        {
            LogVanillaStorageAssets(__instance);
        }

        // Debug dump of the exact vanilla asset names the storage PDA screen uses
        // (uGUI_ItemsContainer.background + .grid in the PDA Inventory tab). Lets the
        // author locate/export them from the game's AssetBundles (e.g. via AssetStudio)
        // to replace the mod's own PDABackground_Mod.png fallback.
        // Dumps up to a few times (assetDumpLimit) in case a dump is missed.
        private static int assetDumpCount;
        private const int assetDumpLimit = 5;

        private static void LogVanillaStorageAssets(uGUI_ItemsContainer target = null)
        {
            if (assetDumpCount >= assetDumpLimit)
            {
                return;
            }

            bool dumped = false;

            if (target != null && target.background != null)
            {
                DumpContainerAssets(target);
                dumped = true;
            }

            if (!dumped && uGUI.main != null)
            {
                uGUI_ItemsContainer[] containers = uGUI.main.GetComponentsInChildren<uGUI_ItemsContainer>(true);
                for (int i = 0; i < containers.Length; i++)
                {
                    if (containers[i] != null && containers[i].background != null)
                    {
                        DumpContainerAssets(containers[i]);
                        dumped = true;
                        break;
                    }
                }
            }

            if (dumped)
            {
                assetDumpCount++;
            }
        }

        private static void DumpContainerAssets(uGUI_ItemsContainer container)
        {
            Graphic bg = container.background;
            Image bgImage = bg as Image;
            if (bgImage != null && bgImage.sprite != null)
            {
                ModPlugin.LogMessage($"Vanilla storage container.background: Image sprite=\"{bgImage.sprite.name}\" tex=\"{bgImage.sprite.texture.name}\"({bgImage.sprite.texture.width}x{bgImage.sprite.texture.height}) color={bgImage.color}");
            }
            else if (bg is RawImage bgRaw && bgRaw.texture != null)
            {
                ModPlugin.LogMessage($"Vanilla storage container.background: RawImage tex=\"{bgRaw.texture.name}\"({bgRaw.texture.width}x{bgRaw.texture.height}) color={bgRaw.color}");
            }
            else
            {
                ModPlugin.LogMessage($"Vanilla storage container.background: {(bg != null ? bg.GetType().Name : "null")}");
            }

            RawImage gridRaw = container.grid;
            if (gridRaw != null && gridRaw.texture != null)
            {
                ModPlugin.LogMessage($"Vanilla storage container.grid: RawImage tex=\"{gridRaw.texture.name}\"({gridRaw.texture.width}x{gridRaw.texture.height} wrap={gridRaw.texture.wrapMode}) color={gridRaw.color}");
            }
            else
            {
                ModPlugin.LogMessage($"Vanilla storage container.grid: {(gridRaw != null ? "RawImage(null tex)" : "null")}");
            }

            // Full walk of the storage panel prefab subtree + ancestors up to 10 hops.
            // Captures every Graphic (corner Ls, border, overlay, background, anything).
            ModPlugin.LogMessage("=== Vanilla storage full hierarchy dump ===");
            DumpAncestors(container.transform);
            DumpGraphicsRecursive(container.transform, 0);
            ModPlugin.LogMessage("=== end dump ===");
        }

        private static void DumpAncestors(Transform node)
        {
            Transform p = node.parent;
            int hops = 0;
            while (p != null && hops < 10)
            {
                string prefix = $"  ancestor[{hops}] {BuildPath(p)}";
                Graphic[] g = p.GetComponents<Graphic>();
                for (int i = 0; i < g.Length; i++)
                {
                    prefix += " | " + DescribeGraphic(g[i]);
                }
                ModPlugin.LogMessage(prefix);
                p = p.parent;
                hops++;
            }
        }

        private static void DumpGraphicsRecursive(Transform node, int depth)
        {
            // Item icons are instantiated per item - not part of the panel prefab.
            if (node.name == "Item Icon")
            {
                return;
            }

            Graphic[] graphics = node.GetComponents<Graphic>();
            if (graphics.Length > 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append(BuildPath(node));
                for (int i = 0; i < graphics.Length; i++)
                {
                    sb.Append(" | ").Append(DescribeGraphic(graphics[i]));
                }
                ModPlugin.LogMessage(sb.ToString());
            }

            for (int i = 0; i < node.childCount; i++)
            {
                DumpGraphicsRecursive(node.GetChild(i), depth + 1);
            }
        }

        private static string DescribeGraphic(Graphic g)
        {
            Image img = g as Image;
            if (img != null)
            {
                string spritePart = img.sprite != null
                    ? $" sprite=\"{img.sprite.name}\" tex=\"{img.sprite.texture.name}\"({img.sprite.texture.width}x{img.sprite.texture.height}) type={img.type}"
                    : " (no sprite)";
                return $"Image{spritePart} color={img.color}";
            }
            if (g is RawImage raw)
            {
                string texPart = raw.texture != null
                    ? $" tex=\"{raw.texture.name}\"({raw.texture.width}x{raw.texture.height} wrap={raw.texture.wrapMode})"
                    : " (no texture)";
                return $"RawImage{texPart} color={raw.color}";
            }
            return g.GetType().Name;
        }

        private static string BuildPath(Transform node)
        {
            string path = node.name;
            Transform parent = node.parent;
            int hops = 0;
            while (parent != null && hops < 6)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
                hops++;
            }
            return path;
        }

        // Samples the 71x71 cell texture as a 10x10 alpha map to reveal where the
        // cell content is (border lines, corner L-shapes, fill).
        private static void DumpCellAlphaMap(Texture texture)
        {
            Texture2D tex2d = texture as Texture2D;
            if (tex2d == null || !tex2d.isReadable)
            {
                ModPlugin.LogMessage($"Vanilla storage cell alpha map: texture not readable (isReadable={tex2d != null && tex2d.isReadable})");
                return;
            }

            int cols = 10;
            int rows = 10;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Vanilla storage cell alpha map (10x10 sampled, alpha 0-255): ");
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int px = Mathf.Clamp((int)((c + 0.5f) * tex2d.width / cols), 0, tex2d.width - 1);
                    int py = Mathf.Clamp((int)((r + 0.5f) * tex2d.height / rows), 0, tex2d.height - 1);
                    sb.Append(tex2d.GetPixel(px, py).a);
                    if (c < cols - 1)
                    {
                        sb.Append(',');
                    }
                }
                if (r < rows - 1)
                {
                    sb.Append(" | ");
                }
            }
            ModPlugin.LogMessage(sb.ToString());
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

            harmony.Patch(
                original: AccessTools.Method(typeof(uGUI_ItemsContainer), "Init"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Patch_ItemsContainerInit_Postfix)));
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
    }
}
