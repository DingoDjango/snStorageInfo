# Code Style Refactor Plan

## Guiding Principles

1. **Variable/method names should be self-explanatory** — if someone needs a comment to understand what `PadLeft` means, the name is wrong.
2. **Delete comments where the code is self-evident** — e.g. `// Max grid size before scaling down` next to `MaxPreviewWidth`.
3. **Keep comments only for genuinely complex logic** — e.g. explaining why we subscribe to container events, or why we're releasing vanilla vs owned assets.
4. **Use block comments `/* */` for multi-line explanations** that apply to a group of fields; use line comments `//` for single-line notes on complex items.

---

## Files to Process (priority order)

1. [`Shared/StorageDetailUI.cs`](snStorageInfo/Shared/StorageDetailUI.cs) — largest, most verbose comments
2. [`Shared/ModOptions.cs`](snStorageInfo/Shared/ModOptions.cs) — verbose class-level comments
3. [`Shared/HarmonyPatches.cs`](snStorageInfo/Shared/HarmonyPatches.cs) — some verbose method comments
4. [`Shared/StorageSlotInfo.cs`](snStorageInfo/Shared/StorageSlotInfo.cs) — minimal, keep as-is
5. [`Shared/Translation.cs`](snStorageInfo/Shared/Translation.cs) — minimal, keep as-is
6. [`Subnautica/ModPlugin.cs`](snStorageInfo/Subnautica/ModPlugin.cs) — minimal, keep as-is

---

## Detailed Changes by File

### 1. StorageDetailUI.cs

| Current | Proposed | Reason |
|---------|----------|--------|
| `MaxPreviewWidth` / `MaxPreviewHeight` | `MaxPreviewWidth` / `MaxPreviewHeight` | Keep — names are clear, delete comment |
| `/* Background border padding... */` (lines 17-21) | `/* Padding between item grid and border texture. */` | Shorten to one line |
| `PadLeft`, `PadRight`, `PadTop`, `PadBottom` | `PadLeft`, `PadRight`, `PadTop`, `PadBottom` | Keep names, use block comment for group |
| `CellTextureFile` | `CellTextureFile` | Keep, delete "Vanilla cell/grid texture" comment |
| `CornerTextureFile` | `CornerTextureFile` | Keep, delete verbose comment |
| `CornerPixelSize` | `CornerPixelSize` | Keep, delete "Native on-screen size..." comment |
| `CornerScale` | `CornerScale` | Keep, delete "Corners kept 1:1..." comment |
| `CellSize` | `CellSize` | Keep, delete "Size in pixels..." comment |
| `PanelOpacity` | `PanelOpacity` | Keep, delete "Overall panel..." comment |
| `Show()` method | — | Keep complex internal logic comments that explain the "why" |
| `ApplyPanelAppearance()` | — | Keep short explaining comment if needed |
| `ApplyCornerAppearance()` | — | Keep short explaining comment |
| `CreateProceduralPanelTexture()` | — | Keep comment on fallback logic (non-obvious) |
| `CreateGridTile()` | — | Keep comment explaining tile pattern (not obvious from code) |
| `Cleanup()` | — | Keep "Corner sprites are shared..." as block comment (explains vanilla vs owned) |

### 2. ModOptions.cs

| Current | Proposed | Reason |
|---------|----------|--------|
| Lines 19-34: verbose class-level comment | `// Options for Preview UI visibility and styling` | Condense to one line |
| Field comment on line 23-24 | Delete — `backgroundOptionObject` is self-explanatory |
| Lines 30-34: verbose comment on PreviewUIBackround | `// Shown only when DisplayMode == Preview` | Shorten |
| Lines 40-42: verbose comment on BackgroundOpacity | `// Shown only when DisplayMode == Preview AND PreviewUIBackround enabled` | Shorten |
| `backgroundOpacityOptionObject` | `backgroundOpacityOptionObject` | Rename to `previewUIBackgroundOpacityOptionObject` — current name is misleading since it's an opacity option object, not the opacity value itself (the rename should have been done in the previous rename task) |

### 3. HarmonyPatches.cs

| Current | Proposed | Reason |
|---------|----------|--------|
| Lines 11-14: verbose comment on `subscribedContainer` | Delete — field name and usage are clear |
| Lines 17-18: verbose comment on `lastValidatedTarget` | Delete — name is clear |
| Lines 20-28: verbose `IsGameInteractive()` comment | Keep as block comment — explains game-load timing (complex) |
| Lines 145-148: verbose comment on "Reticle text dirty-flag" | Shorten to `// Tracks whether hover text needs rebuild` |
| Lines 195-197: verbose `ResetSceneState()` comment | Shorten to `// Clears scene-sensitive state on unload` |

### 4. StorageSlotInfo.cs — No changes needed

- Comments are appropriate (explain custom vs deferred to Nautilus)

### 5. Translation.cs — No changes needed

- Comments are appropriate (explain logging-once behavior)

### 6. ModPlugin.cs — No changes needed

- Comments are appropriate (explain scene unload cleanup)

---

## Summary of Changes

- **Delete** ~15 overly-verbose comments that add no value
- **Shorten** ~5 multi-line comments to 1-2 lines
- **Keep** ~5 comments that explain genuinely complex behavior
- **Rename** 1 field for clarity (`backgroundOpacityOptionObject` → `previewUIBackgroundOpacityOptionObject`)
- **Styling**: Use `/* */` for grouped padding constants; `//` elsewhere