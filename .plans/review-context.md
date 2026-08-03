# Storage Info Mod - Code Review Context Map (v2, post-fix state)

Plaintext reference for mod <-> game <-> Nautilus cross-referencing.
All paths relative to repo root. Line numbers from current working tree.
Scope: Subnautica build only (BZ out of scope). Build verified: `dotnet build SnStorageInfo/Subnautica/StorageInfo.csproj --no-incremental` PASSES.

## 1. Mod Files (SnStorageInfo/)

### SnStorageInfo/Subnautica/ModPlugin.cs - BepInEx entry (47 lines)
- modGUID/modName/modVersion = `Dingo.SN.StorageInfo` / "Storage Info" / `2.8.3031`
- `Awake()` [23-33]: `LanguageHandler.RegisterLocalizationFolder()`; `OptionsPanelHandler.RegisterModOptions<StorageInfoOptions>()`; `HarmonyPatches.InitializeHarmony()`; **new**: `SceneManager.sceneUnloaded += OnSceneUnloaded` (B3 fix)
- `OnDestroy()` [35-39]: unsubscribe scene hook; `StorageDetailUI.Cleanup()`
- `OnSceneUnloaded(Scene)` [41-44]: `StorageDetailUI.Cleanup()` -> frees overlay/sprite/texture/bound container (B3 fix)
- `LogMessage(string)` -> `Debug.Log`

### SnStorageInfo/Shared/ModOptions.cs - Nautilus ConfigFile options (30 lines)
- `enum DisplayMode { DisplayModeDefault, DisplayModeSlotsOnly, DisplayModeDetailedList }`
- `enum PreviewBackground { None, Enabled }` - **NEW FINDING N2**: member names "None"/"Enabled" have no localization keys -> option screen shows raw names
- `class StorageInfoOptions : ConfigFile`, `[Menu("Storage Info")]`
- `[Choice] DisplayMode`, `[Choice] Background` - **NEW FINDING N3**: 2-value enum could be `[Toggle]` (Nautilus idiom)

### SnStorageInfo/Shared/HarmonyPatches.cs - 3 Harmony patches + reticle text (226 lines)
- `private static StorageContainer hoveredStorage` [9] (B4 fix: was public)
- Dirty-flag state [11-18]: `subscribedContainer`, `textDirty`, `lastAppliedMode`, `lastValidatedTarget`
- `Patch_OnHandHover_Postfix(StorageContainer)` [20-30]: sets hoveredStorage; `SubscribeToContainer(container)`; `SetCustomInteractText(container)`
- `Patch_OnHandClick_Prefix(StorageContainer)` [32-37]: hoveredStorage=null; UnsubscribeFromContainer; Hide
- `Patch_GUIHand_OnUpdate_Postfix()` [39-77]: null-guards; `GetActiveTarget()`; **target-cache (O2)**: only `IsHoveringStorage` when activeTarget changed; then DetailedList -> Tick, else -> Hide
- `InitializeHarmony()` [79-100]: patches StorageContainer.OnHandHover (postfix), StorageContainer.OnHandClick (prefix), GUIHand.OnUpdate (postfix)
- `SetCustomInteractText(ItemsContainer)` [~102-138]: **early-return when !textDirty && mode unchanged (B2/O2 fix)**; else switch DisplayMode (Default/SlotsOnly -> text + Hide; DetailedList -> text + Show); `HandReticle.SetText(HandSubscript, text, translate:false)`
- `GetDefaultDisplayText(ItemsContainer)` [~140-155]: IsEmpty -> ContainerEmpty; freeSlots==0 -> ContainerFull; count==1 -> ContainerOneItemSlotsFree.TryFormatTranslate ?? ContainerOneItem; else ContainerNonemptySlotsFree.TryFormatTranslate ?? ContainerNonempty.FormatTranslate
- `GetSlotsOnlyDisplayText(ItemsContainer)` [157-162]: `$"{used}/{total} {"SlotsOccupied".Translate()}"`
- `IsHoveringStorage(StorageContainer, GameObject)` [164-173]: `target.GetComponentInParent<StorageContainer>() == storage`
- `SubscribeToContainer` [177-198]: subscribes vanilla events onAddItem/onRemoveItem/onChangeItemPosition/onResize; sets textDirty=true
- `UnsubscribeFromContainer` [200-213]: unsubscribes all; textDirty=true
- `OnContainerChanged(InventoryItem)` [215-218] / `OnContainerResized(int,int)` [220-223]: set textDirty=true

### SnStorageInfo/Shared/StorageDetailUI.cs - runtime overlay UI (531 lines)
Constants: `MaxPreviewWidth 320f`, `MaxPreviewHeight 400f`, `PadLeft 15`, `PadRight 5`, `PadTop 15`, `PadBottom 5`, `BackgroundTextureFile "PDABackground_Mod.png"`, `CellSize 71` (must match vanilla), `PanelAnchorX 0.75`, `PanelAnchorY 0.5`, `PanelOpacity 0.85`
Fields [37-49]: `root`, `panelRect`, `contentRect`, `containerView` (uGUI_ItemsContainerView), `boundContainer`, `built`, `panelSprite`, `panelTexture`, `gridImage` (RawImage), `gridTexture`, `backgroundOverlay` (Image)

- `Show(ItemsContainer)` [51-86]: guard CanShowOverlay else Hide; EnsureBuilt; if boundContainer != container: UnbindContainer, PrepareContainerLayout, `((IItemsContainer)container).UpdateContainer()` (vanilla Sort), `containerView.Init(container)`, DisableIconRaycasts, boundContainer=container, ApplyGridAppearance; ApplyPanelAppearance (reapplied each Show - cached sprite now); LayoutPanel; SetAsLastSibling; SetActive(true); DoUpdate. Duplicate `DisableIconRaycasts` removed (O3 fix)
- `Tick(ItemsContainer)` [88-104]: guards; CanShowOverlay else Hide; `containerView.DoUpdate()` only - per-frame DisableIconRaycasts removed (O3 fix; CanvasGroup.blocksRaycasts=false already blocks)
- `Hide()` [106-114]: UnbindContainer; root.SetActive(false)
- `Cleanup()` [116-150]: Hide; Destroy root/panelSprite/panelTexture/gridTexture; null refs; built=false. **Now called from ModPlugin OnSceneUnloaded/OnDestroy (B3 fix)**
- `CanShowOverlay(ItemsContainer)` [152-177]: `!WaitScreen.IsWaiting`, `Player.main != null`, `Player.main.GetPDA()` (cached field), `!pda.isInUse`, `!Inventory.main.IsUsingStorage(container)`
- `EnsureBuilt()` [179-277]: builds Canvas under uGUI.main.screenCanvas; CanvasGroup (blocksRaycasts=false, interactable=false); Panel(Image); BackgroundOverlay(Image, 0.35 alpha, disabled); Content(RectTransform); viewObj + Grid(RawImage); `containerView = viewObj.AddComponent<uGUI_ItemsContainerView>()` + direct field assignment `containerView.rectTransform/grid`. **NEW FINDING N1**: `contentRect.anchoredPosition = Vector2.zero;` duplicated at [241-242]. Note: vanilla uGUI_ItemsContainerView.Awake() self-deactivates; Init() reactivates
- `ApplyGridAppearance()` / `SetGridTexture(...)` [281-~360]: copies vanilla container grid texture/material/color via `uGUI.main.GetComponentsInChildren<uGUI_ItemsContainerView>(true)` (excludes own view); procedural 1-cell fallback tile; shared vanilla texture never destroyed (owned flag)
- `CreateGridTile(int)` [364-384]: 1px border tile, tiled via uvRect
- `LoadPanelSprite()` [386-404]: **cached** via `panelSprite != null` (B1 correction: no per-hover leak); LoadPanelTexture -> CreateProceduralPanelTexture fallback; **D2 applied**: `ImageUtils.LoadSpriteFromTexture(panelTexture)`
- `LoadPanelTexture()` [406-419]: `ImageUtils.LoadTextureFromFile(pluginDir/Images/PDABackground_Mod.png, RGBA32)`; try/catch -> null
- `CreateProceduralPanelTexture()` [421-444]: 32x32 grid alpha fallback
- `ApplyPanelAppearance()` [446-~470]: panelImage sprite/type/color(PanelOpacity); backgroundOverlay.enabled = options.Background == Enabled
- `PrepareContainerLayout(ItemsContainer)` [~472-486]: sizeDelta = grid px; containerView.rectTransform.sizeDelta; grid.uvRect = (0,0,width,height)
- `LayoutPanel(ItemsContainer)` [~488-503]: scale = min(1, MaxPreviewWidth/gridWidth, MaxPreviewHeight/gridHeight); contentRect.localScale + anchoredPosition(PadLeft,-PadTop); panelRect.sizeDelta = scaled grid + pads
- `DisableIconRaycasts()` [~505-518]: GetComponentsInChildren<uGUI_ItemIcon>(true) -> raycastTarget=false (once per bind)
- `UnbindContainer()` [~520-528]: containerView.Uninit() (vanilla: unsubscribes events, destroys icons)

### SnStorageInfo/Shared/StorageSlotInfo.cs - slot counting helpers (33 lines, FIXED)
- `GetUsedSlotCount(ItemsContainer)`: foreach item, sum width*height - **CUSTOM** (no Nautilus/game equivalent; item count != used slots)
- `GetTotalSlotCount(ItemsContainer)`: **delegates to Nautilus `ItemStorageHelper.GetTotalSlots(container)`** (D1; static call - NOT an extension method in pre.52)
- `GetFreeSlotCount(ItemsContainer)`: `ItemStorageHelper.GetTotalSlots(container)` - used

### SnStorageInfo/Shared/Translation.cs - language helpers (82 lines, FIXED)
- `Translate(this string)`: `Language.main.TryGet` -> value; else LogMissingKey + return key
- `FormatTranslate(this string, params object[])`: Translate + string.Format, try/catch, falls back to key
- `TryFormatTranslate(...)`: TryGet -> null if missing; format, catch -> null
- `LogMissingKey` (B5 fix): HashSet<string> logs each missing key once

### Localization (SnStorageInfo/Subnautica/BepInEx/plugins/StorageInfo/Localization/English.json + 8 languages)
- Keys: ContainerEmpty, ContainerFull, ContainerOneItem, ContainerNonempty, ContainerOneItemSlotsFree, ContainerNonemptySlotsFree, SlotsOccupied, DisplayMode, DisplayModeDefault, DisplayModeSlotsOnly, DisplayModeDetailedList
- **N2**: NO keys for PreviewBackground members (None/Enabled) - add or rename

## 2. Vanilla Game Source (SN_Source/#Subnautica_Assembly/Assembly-CSharp/)

### ItemsContainer.cs (853 lines)
- `public int sizeX { get; private set; }` [113], `sizeY` [115], `count` [117] (item count, NOT slots)
- ItemGroup internal grouping by TechType; itemsMap[width,height]
- `IEnumerable<InventoryItem>` enumeration; vanilla pattern: `foreach (InventoryItem item in container)`
- `void IItemsContainer.UpdateContainer() { this.Sort(); }` [418-421]
- NO vanilla GetTotalSlots / used-slots methods
- Events: `onAddItem` (OnAddItem), `onRemoveItem` (OnRemoveItem), `onChangeItemPosition` (ItemsContainer.OnChangeItemPosition(InventoryItem)), `onResize` (ItemsContainer.OnResize(int,int)) [103-109] - used by the mod's dirty-flag

### StorageContainer.cs (199 lines)
- `OnHandHover(GUIHand)` [94-107]: every frame while hovered; sets HandReticle Hand text (hoverText, verbose:true, LeftHand) + HandSubscript "Empty" if IsEmpty() else ""; SetIcon(Hand, 1f)
- `OnHandClick(GUIHand)` [109-~125]: Open() + onUse.Invoke()
- `container { get; private set; }` created in Awake/CreateContainer

### uGUI_ItemsContainerView.cs (201 lines)
- `Awake()`: gameObject.SetActive(false) [9-12] - self-deactivates!
- `Init(ItemsContainer)` [14-28]: Uninit; container=c; OnResize(sizeX,sizeY); foreach item -> OnAddItem; subscribe 4 events; SetActive(true)
- `DoUpdate()` [30-36]: per-frame SetBarValue(TooltipFactory.GetBarValue(item)) for every icon
- `Uninit()` [38-52]: unsubscribe all events; Destroy all icon GameObjects

### GUIHand.cs
- `OnUpdate` [~40-100]: Targeting.GetTarget fills activeTarget; walks up to IHandTarget ancestor [61-68]; `GUIHand.Send(activeTarget, Hover, this)` [247] fires OnHandHover once per frame while hovering (validates dirty-flag approach)

### HandReticle.cs
- `public void SetText(TextType type, string text, bool translate, GameInput.Button button = None)` [177] - 3rd param is TRANSLATE flag, not verbose. Mod passes false with pre-translated strings -> CORRECT
- `SetTextRaw(type, text)` [189]

### Player.cs
- `public PDA GetPDA() { return this.pda; }` [227-230] cached field, cheap

## 3. Nautilus Library (Nautilus/Nautilus/) - pre.52 release, matches installed DLL

### Nautilus/Utility/StorageHelperExtensions.cs
- `HasRoomCached(int,int)` / `(Vector2int)` (extensions)
- `IsEmpty()` / `IsFull()` (extensions, [44-59])
- `GetTotalSlots(container)` [66-69] - **NOT an extension** (no `this`); delegates to ItemStorageHelper
- `GetStorageLabel(container)` [78-81] (not extension), `GetAllowedTechTypes()` (extension)

### Nautilus/Utility/ItemStorageHelper.cs
- `GetTotalSlots(ItemsContainer)` [200-203] -> `container.sizeX * container.sizeY` - **the correct D1 call**
- HasRoomCacheCollection, HasRoomForCached, IsEmpty, IsFull, GetStorageLabel, ClearContainerCache

### Nautilus/Utility/ImageUtils.cs
- `LoadTextureFromFile(path, format=BC7)` [27] (used by mod)
- `LoadSpriteFromFile(path, format=BC7)` [62]
- `LoadSpriteFromTexture(texture2D)` [76] (used by mod - D2 applied)

### Nautilus/Handlers/LanguageHandler.cs - RegisterLocalizationFolder() (used)
### Nautilus/Handlers/OptionsPanelHandler.cs + Nautilus/Json/ConfigFile.cs
- RegisterModOptions<T> / [Menu] / [Choice] (used). Choice labels resolve via Language.main.Get(enum member name)

## 4. Review Snapshot (status of prior findings + new findings)

### Prior findings - RESOLVED (build-verified)
- **B1** - panel sprite/texture: was a misdiagnosis; LoadPanelSprite already cached. No per-hover leak. Residue (never freed until exit) covered by B3 fix
- **B2 [MED]** per-frame recompute -> **FIXED**: dirty-flag via container events + target-cache in GUIHand.OnUpdate
- **B3 [LOW]** Cleanup() never called -> **FIXED**: ModPlugin OnSceneUnloaded/OnDestroy hooks
- **B4 [LOW]** public static hoveredStorage -> **FIXED**: private
- **B5 [LOW]** translation log spam -> **FIXED**: HashSet one-time logging
- **O1** cache panel sprite -> already present (verified)
- **O2** dirty-flag text -> **APPLIED**
- **O3** drop per-frame DisableIconRaycasts -> **APPLIED** (Show duplicate + Tick call)
- **O4** GetFreeSlotCount via Nautilus -> **APPLIED** (ItemStorageHelper.GetTotalSlots)
- **D1** GetTotalSlotCount -> Nautilus ItemStorageHelper.GetTotalSlots (static) -> **APPLIED**
- **D2** Sprite.Create -> ImageUtils.LoadSpriteFromTexture -> **APPLIED**
- **D3** correct game/Nautilus patterns kept (UpdateContainer/Sort, Init/Uninit/DoUpdate, TryGet, GetPDA, WaitScreen, SetText translate:false, RegisterLocalizationFolder, RegisterModOptions, LoadTextureFromFile)
- **D4** GetUsedSlotCount stays custom (documented)

### NEW findings (v2 review)
- **N1 [LOW]** StorageDetailUI.cs [241-242]: `contentRect.anchoredPosition = Vector2.zero;` duplicated - copy-paste artifact, clean up
- **N2 [LOW]** ModOptions PreviewBackground enum (None/Enabled) has no localization keys -> options screen shows raw member names; add "None"/"Enabled" keys or rename members + add keys
- **N3 [LOW]** [Choice] on 2-value enum PreviewBackground could be [Toggle] bool (Nautilus idiom); also enum member prefixes (DisplayModeX) are redundant naming
- **N4 [INFO]** ApplyGridAppearance uses GetComponentsInChildren<uGUI_ItemsContainerView>(true) per bind (one alloc per hover) - acceptable; could cache vanilla view reference
