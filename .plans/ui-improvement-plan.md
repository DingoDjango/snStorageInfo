# Storage Info Mod - UI Preview Improvement Plan

## Current State Analysis

### Panel Sprite (Lines 262-287)
- Generates a 32x32 procedural grid texture with dark translucent overlay
- Grid alpha: 0.15f base, doubled on grid lines (every 4th pixel)
- Uses `Image.Type.Simple` - stretches to fill panel
- No border, no rounded corners

### Grid Texture (Lines 238-259)
- Copies from vanilla `uGUI_ItemsContainerView` or `uGUI_ItemsContainer`
- Uses `RawImage` with `uvRect` for tiling
- No custom styling applied

### Border
- **None currently** - panel uses simple sprite stretching
- No outline, no frame, no rounded corners

### Screen Anchor (Lines 16-24)
- Fixed: `PanelAnchorX = 0.75f` (75% from left), `PanelAnchorY = 0.5f` (center)
- `PanelOffset = Vector2.zero` - no fine-tuning
- Hardcoded constants, no mod options

### Animations/Tooltips/Raycasts
- `CanvasGroup.blocksRaycasts = false` (line 180) - good
- `CanvasGroup.interactable = false` (line 181) - good
- `panelImage.raycastTarget = false` (line 192) - good
- `gridImage.raycastTarget = false` (line 224) - good
- `DisableIconRaycasts()` disables raycasts on item icons (lines 337-350)
- **No animations** - panel appears instantly
- **No tooltips** - none implemented

### Additional Details
- Shows full grid with all items
- No container name/header
- No item counts, no extra info

---

## Milestone-Based Plan

### Milestone 1: Panel Visual Redesign
**Goal**: Make panel blend with Subnautica's UI aesthetic

**Tasks**:
1. Replace procedural grid sprite with proper panel background
   - Use vanilla PDA/panel sprite (e.g., `PDABackground.png` from mod assets)
   - Or create 9-slice sprite for proper scaling with borders
2. Add subtle border/frame matching game UI style
3. Add slight rounded corners (Subnautica panels have rounded corners)
4. Adjust opacity/color to match game panels (semi-transparent dark with subtle glow)

**Files**: `StorageDetailUI.cs` - `LoadPanelSprite()`, `ApplyPanelAppearance()`

---

### Milestone 2: Grid Texture Optimization
**Goal**: Clean, consistent grid that matches vanilla container grid

**Tasks**:
1. Ensure grid texture matches vanilla exactly (already copies from vanilla)
2. Add option to tint grid slightly for better contrast
3. Ensure grid lines align perfectly with cell boundaries
4. Consider subtle grid line enhancement for readability

**Files**: `StorageDetailUI.cs` - `ApplyGridAppearance()`

---

### Milestone 3: Dynamic Anchor System
**Goal**: Configurable panel position with mod options

**Tasks**:
1. Add anchor options to `StorageInfoOptions`:
   - `AnchorPreset`: Center, TopLeft, TopRight, BottomLeft, BottomRight, Custom
   - `CustomAnchorX`, `CustomAnchorY` (0-1 range)
   - `OffsetX`, `OffsetY` (pixels)
2. Replace hardcoded `PanelAnchorX`, `PanelAnchorY`, `PanelOffset` with option values
3. Update panel positioning in `EnsureBuilt()` and `LayoutPanel()`

**Files**: 
- `ModOptions.cs` - add new config fields
- `StorageDetailUI.cs` - use options for positioning

---

### Milestone 4: Polish & Non-Interactive Guarantees
**Goal**: Ensure preview is purely visual, no interference

**Tasks**:
1. Verify all raycast blocking is disabled (already done)
2. Add explicit `CanvasGroup.alpha` control for fade-in/out (optional, subtle)
3. Ensure panel doesn't capture input focus
4. Test with various container sizes (small lockers, large lockers, wall lockers)
5. Verify no memory leaks in `Cleanup()`

**Files**: `StorageDetailUI.cs` - `EnsureBuilt()`, `Cleanup()`, `Show()`, `Hide()`

---

### Milestone 5: Integration Testing
**Goal**: Validate all improvements work together

**Tasks**:
1. Test with DisplayModeDetailedList enabled
2. Test hover on: small locker (4x4), medium locker (6x5), large locker (8x6), wall locker (6x8)
3. Test anchor positions at screen edges
4. Test with PDA open (should hide)
5. Test with inventory open (should hide)
6. Test rapid container switching
7. Verify no performance impact

---

## Mod Options Schema (Proposed)

```csharp
public enum AnchorPreset
{
    Center,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Custom
}

[Menu("Storage Info")]
public class StorageInfoOptions : ConfigFile
{
    [Choice]
    public DisplayMode DisplayMode = DisplayMode.DisplayModeDefault;

    // New UI options
    [Choice]
    public AnchorPreset PanelAnchor = AnchorPreset.TopRight;

    [Slider(0f, 1f, 100)]
    public float CustomAnchorX = 0.75f;

    [Slider(0f, 1f, 100)]
    public float CustomAnchorY = 0.5f;

    [Slider(-500f, 500f, 100)]
    public float OffsetX = 0f;

    [Slider(-500f, 500f, 100)]
    public float OffsetY = 0f;

    [Toggle]
    public bool UseGamePanelStyle = true;

    [Slider(0f, 1f, 100)]
    public float PanelOpacity = 0.9f;
}
```

---

## Visual Reference - Subnautica UI Style

Key characteristics to match:
- Dark semi-transparent backgrounds (rgba ~0,0,0,0.8-0.9)
- Subtle cyan/blue accent lines (1-2px)
- Rounded corners (4-8px radius)
- Inner glow/shadow for depth
- 9-slice scaling for panels
- Font: Alterebro (game font) for any text

---

## Implementation Priority

1. **High**: Panel sprite replacement (biggest visual impact)
2. **High**: Dynamic anchor with mod options (user-requested)
3. **Medium**: Border/frame addition
4. **Medium**: Grid texture refinement
5. **Low**: Subtle fade animation (optional)
6. **Low**: Opacity slider

---

## Questions for Clarification

1. **Panel sprite source**: Use existing `PDABackground.png` from mod assets, or create new 9-slice sprite?
2. **Border style**: Thin cyan line (1px) like vanilla panels, or thicker frame?
3. **Anchor presets**: Which presets are most useful? (TopRight seems logical for hover preview)
4. **Fade animation**: Want subtle 100-150ms fade in/out, or instant?
5. **Grid tint**: Slight blue tint to match game, or keep vanilla exact?