# Code Style Refactor Plan

## Guiding Principles

1. **Variable/method names should be self-explanatory** — if someone needs a comment to understand what a field does, the name is wrong.
2. **Rename unclear variables FIRST** — once names are clear, many comments become unnecessary.
3. **Delete comments where the code is self-evident** — e.g. `// Max grid size before scaling down` next to `MaxPreviewWidth`.
4. **Keep (as concise as possible) comments only for genuinely complex logic** — e.g. explaining why we subscribe to container events, or why we're releasing vanilla vs owned assets.
5. **Use block comments `/* */` for multi-line explanations** that apply to a group of fields; use line comments `//` for single-line notes.

---

## Phase 1: Variable Renames (Priority)

### StorageDetailUI.cs — Fields

| Current | Proposed | Reason |
|---------|----------|--------|
| `containerView` | `itemsContainerView` | `uGUI_ItemsContainerView` is not self-explanatory; prefix clarifies |
| `cornerImages` | `cornerImageElements` | "Images" alone is ambiguous (could be sprites); "elements" clarifies these are the 4 corner Image components |
| `gridImage` | `gridRawImage` | Distinguish from `gridTexture` — one is RawImage, other is Texture2D |
| `built` | `isBuilt` | Boolean state flags should have `is` prefix |

### StorageDetailUI.cs — Local Variables (Loop Counters)

| Current | Proposed | Reason |
|---------|----------|--------|
| `c` (corner loops) | `cornerIndex` | Single letter unclear; describes array index |
| `i` (canvas/container loops) | `index` or `canvasIndex`/`containerIndex` | Single letter unclear |
| `x`, `y` (pixel/grid coordinate loops) | `pixelX`, `pixelY` or `gridX`, `gridY` | Distinguish from container size variables |

### ModOptions.cs

| Current | Proposed | Reason |
|---------|----------|--------|
| `backgroundOptionObject` | `previewUIBackgroundOptionObject` | Align with `PreviewUIBackround` option (this is the toggle's row) |
| `backgroundOpacityOptionObject` | `previewUIBackgroundOpacityOptionObject` | Align with `PreviewUIBackgroundOpacity` option |

---

## Phase 2: Comment Cleanup (After Renames)

### StorageDetailUI.cs — Delete/Shorten

| Location | Current | Proposed |
|----------|---------|----------|
| Lines 13-15 | `// Max grid size before scaling down...` | DELETE — `MaxPreviewWidth` is self-explanatory |
| Lines 17-21 | Block comment on padding | Shorten to: `/* Padding between item grid and border texture. */` |
| Lines 27-28 | `// Vanilla cell/grid texture...` | KEEP — short comment useful for vanilla asset reference |
| Lines 30-34 | Verbose comment on corner texture | Shorten to: `// Vanilla grid corner L-shape sprites` |

### HarmonyPatches.cs — Local Variables

| Current | Proposed | Reason |
|---------|----------|--------|
| `c` (corner loop in Cleanup) | `cornerIndex` | Single letter unclear |
| Lines 36-37 | `// Corners sit OUTSIDE...` | DELETE — `CornerPadding` explains itself |
| Lines 39-40 | `// Must match vanilla...` | Shorten to: `// Must match vanilla uGUI_ItemsContainer cell size` |
| Lines 42-44 | Verbose panel placement comment | Shorten to: `// Fixed panel placement` |
| Lines 46-47 | `// Panel styling...` | DELETE — `PanelOpacity` is clear |
| Lines 49-57 | Verbose rect comment | Shorten to: `/* Sprite rects in InventoryGridCorners.png. BL, BR, TL, TR order. */` |
| Lines 67-69 | Verbose on panelTextureOwned | Shorten to: `// True when mod created the texture (procedural fallback), false for shared vanilla` |
| Lines 71-72 | Verbose on gridTexture | DELETE — `gridTexture` is clear |
| Lines 73-74 | Verbose on cornerImages | DELETE — name + usage is clear |
| Lines 75-77 | Verbose on cornerSprites | Shorten to: `// Owned only when built from mod fallback file (vanilla sprites are shared)` |
| Lines 79-80 | Verbose on backgroundOverlay | DELETE — name is clear |
| Lines 81-84 | Verbose on cachedVanillaContainer | DELETE — name is clear |
| Lines 405-407 | `// 1-cell tile mimicking...` | Keep (explains non-obvious tile pattern) |
| Lines 430-432 | Verbose on ApplyCornerAppearance | Shorten to: `// Apply corner L-shapes from vanilla or fallback` |
| Lines 440-441 | Verbose on releasing fallback | Shorten to: `// Release owned fallback sprites before swapping` |
| Lines 646-647 | `// Extra dark overlay...` | DELETE — code is clear |
| Lines 834-835 | Verbose on corner cleanup | Shorten to: `// Corner sprites are shared vanilla assets unless owned by fallback` |

### ModOptions.cs — Delete/Shorten

| Location | Current | Proposed |
|----------|---------|----------|
| Lines 19-22 | Verbose class-level comment | Shorten to: `// Option row GameObjects, refreshed each menu open` |
| Lines 30-34 | Verbose on PreviewUIBackround | Shorten to: `// Only shown in Preview mode` |
| Lines 40-42 | Verbose on BackgroundOpacity | Shorten to: `// Only shown in Preview mode with background enabled` |

### HarmonyPatches.cs — Delete/Shorten

| Location | Current | Proposed |
|----------|---------|----------|
| Lines 11-13 | Verbose on `subscribedContainer` | DELETE |
| Lines 17-18 | Verbose on `lastValidatedTarget` | DELETE |
| Lines 20-28 | Verbose on `IsGameInteractive()` | KEEP (explains game-load timing, complex) |
| Lines 145-148 | Verbose on dirty-flag | Shorten to: `// Reticle text dirty-flag via vanilla ItemsContainer events` |
| Lines 195-197 | Verbose on ResetSceneState | Shorten to: `// Clears scene-sensitive state on unload` |

---

## Files With No Changes Needed

- `StorageSlotInfo.cs` — comments are appropriate
- `Translation.cs` — comments are appropriate
- `ModPlugin.cs` — comments are appropriate

---

## Summary

**Renames (6 total):**
- `containerView` → `itemsContainerView`
- `cornerImages` → `cornerImageElements`
- `gridImage` → `gridRawImage`
- `built` → `isBuilt`
- `backgroundOptionObject` → `previewUIBackgroundOptionObject`
- `backgroundOpacityOptionObject` → `previewUIBackgroundOpacityOptionObject`

**Comment deletions:** ~15

**Comment shortenings:** ~10

**Comments kept (genuinely complex):** ~5