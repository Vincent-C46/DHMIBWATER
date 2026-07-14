# QuantitySettingCommand Design

## Goal

Move quantity-calculation settings out of `QuantityView` so the ribbon's
`QuantitySettingCommand` opens `QuantitySettingsView` independently.

## Architecture

Create `QuantitySettingCommand` in the Revit quantity-command namespace. The
command owns the settings `ExternalEvent` wiring: it loads settings from
DataStorage, opens `QuantitySettingsView` after the load completes, and wires
save/import/export callbacks.

`QuantityCommand` remains responsible only for the quantity-calculation view,
calculation request, selection request, and manual quantity UI. It no longer
opens or configures the settings view. `QuantityViewModel` no longer exposes a
settings command, and the settings toolbar button is removed from
`QuantityView.xaml`.

## Components and Data Flow

1. The Quantity ribbon's "수량산출 설정" button targets
   `QuantitySettingCommand`.
2. `QuantitySettingCommand` resolves `IQuantitySettingsRepository`,
   `SaveQuantitySettingsUseCase`, `IFileDialogService`, and
   `IProjectSettingsRepository` before the modeless view lifetime begins.
3. The command raises `QuantitySettingsRequestId.Open` through the existing
   `QuantitySettingsRequestHandler`.
4. On the WPF dispatcher callback, the handler supplies loaded settings and
   the command creates `QuantitySettingsViewModel` and `QuantitySettingsView`.
5. Save uses `QuantitySettingsRequestId.Save`; import/export continue to use
   the existing `.dhcfg` repository and file-dialog service.

## Error Handling

No new error policy is introduced. Existing behavior remains: missing stored
settings uses a new `ProjectSettings`; cancelled file dialogs perform no work;
an invalid or unreadable imported configuration does not replace the current
view model.

## Testing and Verification

Add or update focused UI/ViewModel tests only if the existing test project can
exercise the removed public settings members. Build the Revit project and run
the existing UI test project. The final work also updates `PROGRESS.md` with
the files changed and any remaining Revit manual-verification note.

## Scope Boundaries

- Reuse `QuantitySettingsRequestHandler` and its request types unchanged.
- Do not alter the settings model, DataStorage schema, quantity-calculation
  logic, or `.dhcfg` format.
- Do not change unrelated modified files in the working tree.
