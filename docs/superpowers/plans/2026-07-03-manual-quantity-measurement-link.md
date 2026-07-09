# Manual Quantity Measurement Link Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Revit-driven length and area measurement buttons to manual quantity variable cards so measured values flow back into the existing formula preview and save path.

**Architecture:** Keep measurement abstraction in `Application`, execute Revit picks through a new `ExternalEvent` service in `Revit`, and thread that service through `QuantityViewModel` into `ManualQuantityViewModel`. Convert the manual quantity popup from modal to modeless so Revit selection remains available while preserving the existing add/edit callbacks.

**Tech Stack:** .NET 8, WPF, Revit ExternalEvent API, xUnit

## Global Constraints

- `CLAUDE.md` rules apply as the canonical project rules.
- Requested changes must stay surgical and avoid unrelated refactors.
- Revit API calls must run only inside `IExternalEventHandler.Execute`.
- Transaction is not required for measurement and must not be added.
- `ManualQuantityViewModel` must receive measurement capability via parent flow, not direct DI construction in the dialog call sites.
- Build after each major step and update `PROGRESS.md` when complete.

---

### Task 1: Add Measurement Abstractions And ViewModel Tests

**Files:**
- Create: `src/DHBIMWATER.Application/Interfaces/Quantity/IMeasurePickService.cs`
- Create: `tests/DHBIMWATER.UI.Tests/DHBIMWATER.UI.Tests.csproj`
- Create: `tests/DHBIMWATER.UI.Tests/ViewModels/Quantity/ManualQuantityViewModelTests.cs`
- Modify: `DHBIMWATER.sln`

**Interfaces:**
- Produces: `enum MeasureKind`, `record MeasureResult`, `interface IMeasurePickService { Task<MeasureResult?> PickAsync(MeasureKind kind); }`
- Produces: failing and passing tests for `ManualQuantityViewModel` measurement behavior

- [ ] Add the measurement abstraction file in `Application`.
- [ ] Add a new `xUnit` test project targeting `net8.0-windows` with project references to `src/DHBIMWATER.UI` and `src/DHBIMWATER.Application`.
- [ ] Write failing tests for:
  - length measurement fills `VariableInput.Value`, `VariableInput.Unit`, and preview
  - cancelled measurement leaves existing value unchanged
  - commands stay disabled when no measurement service is provided
- [ ] Run the new test project and verify the new tests fail for the expected missing members.

### Task 2: Implement UI Measurement Flow And Modeless Dialog

**Files:**
- Modify: `src/DHBIMWATER.UI/ViewModels/Quantity/ManualQuantityViewModel.cs`
- Modify: `src/DHBIMWATER.UI/ViewModels/Quantity/QuantityViewModel.cs`
- Modify: `src/DHBIMWATER.UI/Views/Quantity/ManualQuantityView.xaml`
- Modify: `src/DHBIMWATER.UI/Views/Quantity/ManualQuantityView.xaml.cs`
- Modify: `src/DHBIMWATER.UI/Views/Quantity/QuantityView.xaml.cs`

**Interfaces:**
- Consumes: `IMeasurePickService`, `MeasureKind`, `MeasureResult`
- Produces: `QuantityViewModel.MeasureService`, `SetMeasureService(IMeasurePickService service)`
- Produces: `ManualQuantityViewModel.MeasureLengthCommand`, `MeasureAreaCommand`

- [ ] Extend `ManualQuantityViewModel` with optional measurement service injection, measurement commands, async update flow, and `VariableInput.IsMeasuring`.
- [ ] Convert `ManualQuantityView` close handling from modal `DialogResult` to modeless `Close()`.
- [ ] Update `QuantityView.xaml.cs` add/edit handlers to open modeless dialogs and process results through `CloseRequested`.
- [ ] Update `ManualQuantityView.xaml` to show 5-column variable cards with unit text and two measure buttons using existing icon pack styles.
- [ ] Run the `ManualQuantityViewModel` tests and verify they pass.

### Task 3: Implement Revit ExternalEvent Measurement Service And Wire It

**Files:**
- Create: `src/DHBIMWATER.Revit/Commands/Quantity/RevitMeasurePickService.cs`
- Modify: `src/DHBIMWATER.Revit/Commands/Quantity/QuantityCommand.cs`

**Interfaces:**
- Consumes: `IMeasurePickService`
- Produces: `RevitMeasurePickService : IMeasurePickService, IExternalEventHandler`

- [ ] Add the new Revit measurement service using `ExternalEvent.Create(this)`, `TaskCompletionSource`, and `PickPoints`/`PickObject(ObjectType.Face)` execution paths.
- [ ] Wire the service into `QuantityCommand` and pass it to `QuantityViewModel.SetMeasureService`.
- [ ] Build the affected projects and verify there are no compile errors from the new Revit integration.

### Task 4: Final Verification And Progress Log

**Files:**
- Modify: `PROGRESS.md`

**Interfaces:**
- Consumes: all prior tasks
- Produces: updated progress log and known TODO notes

- [ ] Build the solution or the maximal buildable project set in this environment.
- [ ] Record the completed feature, changed files, and remaining runtime verification notes in `PROGRESS.md`.
