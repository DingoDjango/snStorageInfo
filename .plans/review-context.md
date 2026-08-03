# Storage Info Mod - Code Review Context Map

Plaintext reference for mod <-> game <-> Nautilus cross-referencing.
All paths relative to repo root. Line numbers from current working tree.

## 1. Mod Files (SnStorageInfo/)

### SnStorageInfo/Subnautica/ModPlugin.cs - BepInEx entry, SN build
- modGUID/modName/modVersion = `Dingo.SN.StorageInfo` / "Storage Info" / `2.8.3031`
- `Awake()`: `LanguageHandler.RegisterLocalizationFolder()`; `OptionsPanelHandler.RegisterModOptions<StorageInfoOptions>()`; `HarmonyPatches.InitializeHarmony()`
- `LogMessage(string)` -> `Debug.Log`

### SnStorageInfo/Below Zero/ModPlugin.cs - BZ entry (mirror of above)

### SnStorageInfo/Shared/ModOptions.cs - Nautilus ConfigFile options
- `enum DisplayMode { DisplayModeDefault, DisplayModeSlotsOnly, DisplayModeDetailedList }`
- `enum PreviewBackground { None, Enabled }`
- `class StorageInfoOptions : ConfigFile`, `[Menu("Storage Info")]`
- `[Choice] DisplayMode`, `[Choice] Background`

### SnStorageInfo/Shared/HarmonyPatches.cs - 3 Harmony patches + reticle text
- `public static StorageContainer hoveredStorage`
- `Patch_OnHandHover_Postfix(StorageContainer __instance)` [11-20]: sets hoveredStorage; `SetCustomInteractText(__instance.container)`
- `Patch_OnHandClick_Prefix(StorageContainer)` [22-26]: `hoveredStorage = null`; `StorageDetailUI.Hide()`
- `Patch_GUIHand_OnUpdate_Postfix()` [28-59]: per-frame null-guards, `GetActiveTarget()`, `IsHoveringStorage` check, then DetailedList -> `StorageDetailUI.Tick(container)`, else -> `StorageDetailUI.Hide()`
- `InitializeHarmony()` [61-76]: patches `StorageContainer.OnHandHover` (postfix), `StorageContainer.OnHandClick` (prefix), `GUIHand.OnUpdate` (postfix)
- `SetCustomInteractText(ItemsContainer)` [78-105]: switch DisplayMode; Default -> GetDefaultDisplayText + Hide; SlotsOnly -> GetSlotsOnlyDisplayText + Hide; DetailedList -> GetSlotsOnlyDisplayText + Show; then `HandReticle.SetText(HandSubscript, text, translate:false)` [104]
- `GetDefaultDisplayText(ItemsContainer)` [107-129]: IsEmpty -> `ContainerEmpty.Translate()`; freeSlots==0 -> `ContainerFull.Translate()`; count==1 -> `ContainerOneItemSlotsFree.TryFormatTranslate(freeSlots)` ?? `ContainerOneItem.Translate()`; else -> `ContainerNonemptySlotsFree.TryFormatTranslate(count,freeSlots)` ?? `ContainerNonempty.FormatTranslate(count)`
- `GetSlotsOnlyDisplayText(ItemsContainer)` [131-136]: `$"{usedSlots}/{totalSlots} {"SlotsOccupied".Translate()}"` using StorageSlotInfo counts
- `IsHoveringStorage(StorageContainer, GameObject target)` [138-147]: `target.GetComponentInParent<StorageContainer>() == storage`

### SnStorageInfo/Shared/StorageDetailUI.cs - runtime overlay UI (532 lines)
Constants: `MaxPreviewWidth 320f`, `MaxPreviewHeight 400f`, `PadLeft 15`, `PadRight 5`, `PadTop 15`, `PadBottom 5`, `BackgroundTextureFile "PDABackground_Mod.png"`, `CellSize 71` (must match vanilla), `PanelAnchorX 0.75`, `PanelAnchorY 0.5`, `PanelOpacity 0.85`
Fields: `root`, `panelRect`, `contentRect`, `containerView` (uGUI_ItemsContainerView), `boundContainer`, `built`, `panelSprite`, `panelTexture`, `gridImage` (RawImage), `gridTexture`, `backgroundOverlay` (Image)

- `Show(ItemsContainer)` [51-85]: guard CanShowOverlay -> else Hide; EnsureBuilt(); if boundContainer != container: UnbindContainer, PrepareContainerLayout, `((IItemsContainer)container).UpdateContainer()` (vanilla Sort), `containerView.Init(container)`, DisableIconRaycasts, boundContainer = container, ApplyGridAppearance; then ApplyPanelAppearance (**reloads panel sprite EVERY Show - leak suspect**), LayoutPanel, SetAsLastSibling, SetActive(true), DoUpdate, DisableIconRaycasts
- `Tick(ItemsContainer)` [87-102]: guards + CanShowOverlay else Hide; `containerView.DoUpdate()`; `DisableIconRaycasts()` (**per frame**)
- `Hide()` [104-112]: UnbindContainer; root.SetActive(false)
- `Cleanup()` [114-148]: Hide; Destroy root; Destroy panelSprite/panelTexture/gridTexture - **NEVER CALLED anywhere in mod**
- `CanShowOverlay(ItemsContainer)` [150-175]: `!WaitScreen.IsWaiting`, `Player.main != null`, `Player.main.GetPDA()` (cached field, cheap), `!pda.isInUse`, `!Inventory.main.IsUsingStorage(container)`
- `EnsureBuilt()` [177-~230]: builds Canvas `StorageInfoOverlay` under `uGUI.main.transform`, CanvasGroup (blocksRaycasts=false, interactable=false), Panel (Image), BackgroundOverlay (Image, alpha 0.35, disabled by default), Content (RectTransform), `containerView = AddComponent<uGUI_ItemsContainerView>()`. Note: vanilla uGUI_ItemsContainerView.Awake() deactivates self; Init() reactivates
- `ApplyGridAppearance()` / `SetGridTexture(...)` [~285-336]: steals vanilla grid texture via `uGUI.main.GetComponentInChildren<uGUI_ItemsContainer>(true)`; procedural 1-cell fallback tile; never destroys shared vanilla texture (owned flag)
- `LoadPanelTexture()` [407-420]: `ImageUtils.LoadTextureFromFile(pluginDir/Images/PDABackground_Mod.png, TextureFormat.RGBA32)`; try/catch -> null
- `CreateProceduralPanelTexture()` [422-445]: 32x32 grid alpha fallback
- `LoadPanelSprite()` [~385-405]: Texture2D -> manual `Sprite.Create` (Nautilus `LoadSpriteFromFile` exists)
- `ApplyPanelAppearance()` [447-471]: panelImage enabled/sprite/type/color(PanelOpacity); backgroundOverlay.enabled = options.Background == Enabled
- `PrepareContainerLayout(ItemsContainer)` [473-487]: sizeDelta = grid px; containerView.rectTransform.sizeDelta; grid.uvRect = (0,0,width,height)
- `LayoutPanel(ItemsContainer)` [489-504]: scale = min(1, MaxPreviewWidth/gridWidth, MaxPreviewHeight/gridHeight); contentRect.localScale, anchoredPosition(PadLeft,-PadTop), panelRect.sizeDelta = scaled grid + pads
- `DisableIconRaycasts()` [506-519]: `GetComponentsInChildren<uGUI_ItemIcon>(true)` each call -> raycastTarget=false
- `UnbindContainer()` [521-529]: `containerView.Uninit()` (vanilla: unsubscribes events, destroys icons)

### SnStorageInfo/Shared/StorageSlotInfo.cs - slot counting helpers
- `GetUsedSlotCount(ItemsContainer)`: foreach item, sum width*height - **CUSTOM**
- `GetTotalSlotCount(ItemsContainer)`: sizeX * sizeY - **== Nautilus GetTotalSlots**
- `GetFreeSlotCount(ItemsContainer)`: total - used

### SnStorageInfo/Shared/Translation.cs - language helpers
- `Translate(this string)`: `Language.main.TryGet` -> value, else log + return key
- `FormatTranslate(this string, params object[])`: Translate + string.Format, try/catch, falls back to key
- `TryFormatTranslate(...)`: TryGet -> null if missing, format, catch -> null

## 2. Vanilla Game Source (SN_Source/#Subnautica_Assembly/Assembly-CSharp/)

### ItemsContainer.cs (853 lines)
- `public int sizeX { get; private set; }` [113], `sizeY` [115], `count` [117] (item count, NOT slots)
- ItemGroup internal grouping by TechType; itemsMap[width,height]
- `IEnumerable<InventoryItem>` enumeration; vanilla pattern: `foreach (InventoryItem item in container)` -> ItemGroup.items
- `void IItemsContainer.UpdateContainer() { this.Sort(); }` [418-421]
- NO vanilla GetTotalSlots / used-slots methods
- `StorageContainer.IsEmpty()` (container.count <= 0) [StorageContainer.cs:90-92]

### StorageContainer.cs (199 lines)
- `OnHandHover(GUIHand)` [94-107]: every frame while hovered; sets HandReticle Hand text (hoverText, verbose:true, LeftHand button) and HandSubscript to "Empty" if IsEmpty() else ""; SetIcon(Hand, 1f)
- `OnHandClick(GUIHand)` [109-~125]: Open() + onUse.Invoke()
- `container { get; private set; }` created in Awake/CreateContainer

### uGUI_ItemsContainerView.cs (201 lines)
- `Awake()`: `gameObject.SetActive(false)` [9-12] - self-deactivates!
- `Init(ItemsContainer)` [14-28]: Uninit(); container = c; OnResize(sizeX,sizeY); foreach item -> OnAddItem; subscribe onAddItem/onRemoveItem/onChangeItemPosition/onResize; SetActive(true)
- `DoUpdate()` [30-36]: per-frame SetBarValue(TooltipFactory.GetBarValue(item)) for every icon (battery/food bars)
- `Uninit()` [38-52]: unsubscribe all container events; Destroy all icon GameObjects; clear lists

### HandReticle.cs
- `public void SetText(TextType type, string text, bool translate, GameInput.Button button = None)` [177] - NOTE: 3rd param is TRANSLATE flag, not verbose. Mod passes false with pre-translated strings -> CORRECT
- `SetTextRaw(type, text)` [189]

### Player.cs
- `public PDA GetPDA() { return this.pda; }` [227-230] cached field, cheap

## 3. Nautilus Library (Nautilus/Nautilus/)

### Nautilus/Utility/StorageHelperExtensions.cs (extensions on ItemsContainer)
- `HasRoomCached(int width, int height)` / `(Vector2int)` -> ItemStorageHelper
- `IsEmpty()` [44-47]
- `IsFull()` [56-59]
- `GetTotalSlots(container)` [66-69] - **replaces StorageSlotInfo.GetTotalSlotCount()**
- `GetStorageLabel(container)` [78-81]
- `GetAllowedTechTypes()` [~89]

### Nautilus/Utility/ItemStorageHelper.cs (backing static class)
- HasRoomCacheCollection (per-container cached has-room lookups)
- HasRoomForCached, ClearContainerCache, CacheNewHasRoomData, TryGetCachedHasRoom, IsEmpty, IsFull, GetTotalSlots, GetStorageLabel

### Nautilus/Utility/ImageUtils.cs
- `LoadTextureFromFile(path, format=BC7)` [27] (used by mod)
- `LoadSpriteFromFile(path, format=BC7)` [62] (mod could use)
- `LoadSpriteFromTexture(texture2D)` [76]

### Nautilus/Handlers/LanguageHandler.cs
- `RegisterLocalizationFolder()` (used by mod)

### Nautilus/Handlers/OptionsPanelHandler.cs + Nautilus/Json/ConfigFile.cs
- `RegisterModOptions<T>()` / `[Menu]` / `[Choice]` (used by mod)

## 4. Key Review Snapshot

### Bugs
- **B1 [HIGH]** Panel sprite+texture recreated every Show() hover -> leak (ApplyPanelAppearance -> LoadPanelSprite -> LoadPanelTexture; never Destroy'ed; Cleanup() never called)
- **B2 [MED]** Per-frame recompute while hovering: SetCustomInteractText every OnHandHover frame (full container iteration for used slots + string alloc + SetText); IsHoveringStorage GetComponentInParent every frame; Tick -> DoUpdate + DisableIconRaycasts (alloc) every frame
- **B3 [LOW]** Cleanup() never called (no scene-change hook)
- **B4 [LOW]** hoveredStorage public static mutable field
- **B5 [LOW]** Translation log spam for missing keys (per hover frame)

### Optimizations
- **O1** Cache panelSprite/panelTexture (lazy init once; reload on null)
- **O2** Dirty-flag reticle text via container events (onAddItem/onRemoveItem/onChangeItemPosition/onResize) instead of recompute per frame
- **O3** Drop per-frame DisableIconRaycasts in Tick (CanvasGroup.blocksRaycasts=false already blocks all raycasts on subtree)
- **O4** GetFreeSlotCount via `container.GetTotalSlots()` - used

### Defer to Nautilus / Game
- **D1** StorageSlotInfo.GetTotalSlotCount -> Nautilus `container.GetTotalSlots()`
- **D2** LoadPanelSprite manual Sprite.Create -> `ImageUtils.LoadSpriteFromFile()`
- **D3** Already correct game/Nautilus patterns - keep: UpdateContainer()/Sort(), uGUI_ItemsContainerView.Init/Uninit/DoUpdate, Language.main.TryGet, Player.GetPDA(), WaitScreen, HandReticle.SetText, LanguageHandler/OptionsPanelHandler/ImageUtils
- **D4** GetUsedSlotCount has NO Nautilus equivalent (area sum, not count) - keep custom, document why
