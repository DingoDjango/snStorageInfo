# Storage Info Mod - UI Preview Improvement Plan

## Current State Analysis

### Panel Sprite
- Generates a 32x32 procedural grid texture with dark translucent overlay
- Grid alpha: 0.15f base, doubled on grid lines (every 4th pixel)

### Grid Texture
- Copies from vanilla `uGUI_ItemsContainerView` or `uGUI_ItemsContainer`
- No custom styling applied
- Possibly PDABackground.png as preview UI border and background

### Border
- Simple sleek border with rounded corners
- If using PDABackground, no border needed

### Screen Anchor
- Fixed: `PanelAnchorX = 0.75f` (75% from left), `PanelAnchorY = 0.5f` (center)
- `PanelOffset = Vector2.zero` - no fine-tuning
- Hardcoded constants, no mod options

### Animations/Tooltips/Raycasts
- **No animations** - panel appears instantly
- **No tooltips** - none implemented

### Additional Details
- Shows full grid with all items
- No container name/header
- No item counts, no extra info (provided by hand reticle patch)

---

## Milestone-Based Plan

### Milestone 1: Panel Visual Redesign
**Goal**: Make panel blend with Subnautica's UI aesthetic

**Tasks**:
1. Replace procedural grid sprite with proper panel background
   - Use vanilla PDA/panel sprite (e.g., `PDABackground.png` from mod assets)
   - Or create 9-slice sprite for proper scaling with borders
2. Subtle rounded border/frame matching game UI style or from sprite/texture
3. Opacity/color match game panels (semi-transparent dark with subtle glow)

---

### Milestone 2: Grid Texture Optimization
**Goal**: Clean, consistent grid that matches vanilla container grid

**Tasks**:
1. Ensure grid lines align perfectly with cell boundaries

---

### Milestone 3: Polish & Non-Interactive Guarantees
**Goal**: Ensure preview is purely visual, no interference

**Tasks**:
1. Verify all raycast blocking is disabled (already done)
2. Ensure panel doesn't capture input focus
3. Test with various container sizes (small lockers, large lockers, wall lockers) - with user assistance
4. Verify no memory leaks

---

### Milestone 5: Integration Testing
**Goal**: Validate all improvements work together

**Tasks**:
1. Test all DisplayMode options
2. Test hover on: small locker (4x4), medium locker (6x5), large locker (8x6), wall locker (6x8)
3. Test anchor positions
4. Test with PDA open (should hide)
5. Test with inventory open (should hide)
6. Test rapid container switching
7. Test UI disappears correctly if no storage target