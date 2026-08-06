# Preview UI vs vanilla interaction

## Vanilla key classes/methods

**GUIHand** — hover/click dispatch. Runs only when `AvatarInputHandler.main.IsEnabled() && !IsPDAInUse()`. Raycasts 2m, walks up to first `IHandTarget`.

**Player** — `GetMode()`, `IsFreeToInteract()`, `GetPDA()`. `IsFreeToInteract = !cinematicModeActive && IsAlive() && (Normal || Piloting)`.

**StorageContainer** — `OnHandHover`, `OnHandClick`. Early-return if `!enabled` or same-object `Constructable` not `constructed`.

**PDA** — `isInUse`, `Open`, `Close`. Open sets `isInUse=true` + `Inventory.SetUsedStorage`.

**Inventory** — `IsUsingStorage`, `SetUsedStorage`, `ClearUsedStorage`.

**Builder** — `isPlacing`. True while ghost placed.

**BeaconLabel** — `IHandTarget` for rename dialog. `Beacon` is `PlayerTool`, not hand target.

**WaitScreen** — `IsWaiting`.

## Mod mirrors

**CanShowOverlay** — mirrors `IsFreeToInteract`, `pda.isInUse`, `IsUsingStorage`, `Builder.isPlacing`. No beacon check (revalidation handles it).

**IsStorageInteractable** — mirrors `StorageContainer.OnHandHover`/`OnHandClick` (`enabled` + `Constructable.constructed`).

**Patch_OnHandHover_Postfix** — sets `hoveredStorage` only if `IsStorageInteractable`.

**Patch_GUIHand_OnUpdate_Postfix** — revalidates `hoveredStorage` vs `guiHand.GetActiveTarget()`; calls `Tick` only if `PreviewUI` + `IsStorageInteractable(hoveredStorage)`.

**Gap fixed** — unconstructed locker while building (vanilla hides reticle, mod now mirrors). Disabled container. Death/cinematic (via `IsFreeToInteract`).

**Not fixed** — pause menu overlay (intentional: shows live preview while changing mod options).
