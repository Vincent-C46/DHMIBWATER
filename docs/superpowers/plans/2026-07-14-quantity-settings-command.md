# QuantitySettingCommand Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Open quantity-calculation settings through a dedicated ribbon command instead of from `QuantityView`.

**Architecture:** Move the settings ExternalEvent and `.dhcfg` callback wiring from `QuantityCommand` into a new `QuantitySettingCommand`. Keep `QuantityCommand` focused on calculation and selection, remove its settings callback from `QuantityViewModel`, and route the existing ribbon button to the new command.

**Tech Stack:** C#/.NET 8 WPF, Autodesk Revit ExternalCommand/ExternalEvent APIs, xUnit.

## Global Constraints

- Preserve `QuantitySettingsRequestHandler`, `QuantitySettingsRequest`, DataStorage, and `.dhcfg` serialization behavior.
- Resolve services before the modeless settings window outlives `CommandBase`'s service-container lifetime.
- Do not inject Revit `Document` directly or open a Transaction outside `SaveQuantitySettingsUseCase`.
- Do not modify unrelated existing working-tree changes.
- Update `PROGRESS.md` and build the Revit project after implementation.

---

### Task 1: Remove QuantityView's settings dependency

**Files:**
- Modify: `src/DHBIMWATER.UI/Views/Quantity/QuantityView.xaml:131-140`
- Modify: `src/DHBIMWATER.UI/ViewModels/Quantity/QuantityViewModel.cs:16-23, 107-114, 133-141, 239-243`
- Test: `tests/DHBIMWATER.UI.Tests/ViewModels/Quantity/QuantityViewModelTests.cs`

**Interfaces:**
- Consumes: no settings-specific service or callback.
- Produces: `QuantityViewModel` exposes only commands used by the quantity-calculation window.

- [ ] **Step 1: Write the failing test**

Create `QuantityViewModelTests.cs` and document the remaining public command contract using the existing constructor dependencies:

```csharp
[Fact]
public void QuantityViewModel_ExposesQuantityWorkflowCommands()
{
    var vm = CreateViewModel();

    Assert.NotNull(vm.ExtractCommand);
    Assert.NotNull(vm.SelectInRevitCommand);
    Assert.Null(typeof(QuantityViewModel).GetProperty("OpenSettingsCommand"));
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests\\DHBIMWATER.UI.Tests\\DHBIMWATER.UI.Tests.csproj --filter "FullyQualifiedName~QuantityViewModel_ExposesQuantityWorkflowCommands"`

Expected: FAIL because `OpenSettingsCommand` still exists.

- [ ] **Step 3: Write the minimal implementation**

Remove the settings action field, command declaration, constructor assignment, and setter from `QuantityViewModel`:

```csharp
private Action<IList<long>>? _selectAction;
private bool _isSelectedInRevit;

public ICommand SelectInRevitCommand { get; }

SelectInRevitCommand = new RelayCommand(
    _ => OnSelectInRevit(), _ => _currentSelectedItems.Count > 0);

public void SetSelectAction(Action<IList<long>> action) => _selectAction = action;
```

Delete the `OpenSettingsCommand` button from the `QuantityView.xaml` toolbar, including its tooltip and inner `StackPanel`.

- [ ] **Step 4: Run the focused test to verify it passes**

Run: `dotnet test tests\\DHBIMWATER.UI.Tests\\DHBIMWATER.UI.Tests.csproj --filter "FullyQualifiedName~QuantityViewModel_ExposesQuantityWorkflowCommands"`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/DHBIMWATER.UI/Views/Quantity/QuantityView.xaml src/DHBIMWATER.UI/ViewModels/Quantity/QuantityViewModel.cs tests/DHBIMWATER.UI.Tests/ViewModels/Quantity/QuantityViewModelTests.cs
git commit -m "refactor: remove quantity view settings action"
```

### Task 2: Add the standalone QuantitySettingCommand

**Files:**
- Create: `src/DHBIMWATER.Revit/Commands/Quantity/QuantitySettingCommand.cs`
- Modify: `src/DHBIMWATER.Revit/Commands/Quantity/QuantityCommand.cs:4-13, 19-136`

**Interfaces:**
- Consumes: `IQuantitySettingsRepository`, `SaveQuantitySettingsUseCase`, `IFileDialogService`, `IProjectSettingsRepository`, `QuantitySettingsRequestHandler`, and `QuantitySettingsViewModel`.
- Produces: `[Transaction(TransactionMode.Manual)] public class QuantitySettingCommand : CommandBase`.

- [ ] **Step 1: Write the failing test**

Because the Revit command assembly has no isolated test project and requires Revit API assemblies, add a compile-time contract check by temporarily referencing the intended type in `QuantityRibbonModule`:

```csharp
RevitCommandType<QuantitySettingCommand>.FullName
```

- [ ] **Step 2: Run the build to verify it fails**

Run: `dotnet build src\\DHBIMWATER.Revit\\DHBIMWATER.Revit.csproj -c Release`

Expected: FAIL with a missing `QuantitySettingCommand` type.

- [ ] **Step 3: Write the minimal implementation**

Create `QuantitySettingCommand` by moving `WireSettings` and `WireSettingsVm` unchanged from `QuantityCommand`; replace the QuantityView owner with the Revit main window handle:

```csharp
[Transaction(TransactionMode.Manual)]
public class QuantitySettingCommand : CommandBase
{
    protected override Result ExecuteInternal(
        ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var settingsRepo = ServiceContainer.GetService<IQuantitySettingsRepository>();
        var saveUseCase = ServiceContainer.GetService<SaveQuantitySettingsUseCase>();
        var handler = new QuantitySettingsRequestHandler(settingsRepo, () => saveUseCase);
        var externalEvent = ExternalEvent.Create(handler);
        var fileDialog = ServiceContainer.GetService<IFileDialogService>();
        var dhcfgRepo = ServiceContainer.GetService<IProjectSettingsRepository>();

        handler.OnLoaded = settings =>
        {
            var vm = new QuantitySettingsViewModel(settings);
            var view = new QuantitySettingsView(vm);
            new WindowInteropHelper(view).Owner = commandData.Application.MainWindowHandle;
            WireSettingsVm(vm, view, handler, externalEvent, fileDialog, dhcfgRepo);
            view.Show();
        };

        handler.Request.Make(QuantitySettingsRequestId.Open);
        externalEvent.Raise();
        return Result.Succeeded;
    }
}
```

Retain the existing `WireSettingsVm` logic in the new class. Remove `WireSettings`, `WireSettingsVm`, settings usings, and `WireSettings();` from `QuantityCommand`.

- [ ] **Step 4: Run the Revit build to verify it passes**

Run: `dotnet build src\\DHBIMWATER.Revit\\DHBIMWATER.Revit.csproj -c Release`

Expected: build succeeds with zero compilation errors. Existing nullable/reference or deployment-copy warnings may remain.

- [ ] **Step 5: Commit**

```powershell
git add src/DHBIMWATER.Revit/Commands/Quantity/QuantityCommand.cs src/DHBIMWATER.Revit/Commands/Quantity/QuantitySettingCommand.cs
git commit -m "feat: add standalone quantity settings command"
```

### Task 3: Route the ribbon settings button and record delivery

**Files:**
- Modify: `src/DHBIMWATER.Revit/UI/Modules/QuantityRibbonModule.cs:14`
- Modify: `PROGRESS.md`

**Interfaces:**
- Consumes: `QuantitySettingCommand` from Task 2.
- Produces: `QuantitySettingCommand` ribbon item launches the standalone command.

- [ ] **Step 1: Write the failing compile-time reference**

Change only the ribbon command type:

```csharp
RevitCommandType<QuantitySettingCommand>.FullName
```

- [ ] **Step 2: Run the build to verify the pre-implementation failure**

Run: `dotnet build src\\DHBIMWATER.Revit\\DHBIMWATER.Revit.csproj -c Release`

Expected: before Task 2, FAIL for missing `QuantitySettingCommand`; after Task 2, use this step as the routing verification.

- [ ] **Step 3: Write the minimal implementation**

Keep the button identifiers and labels unchanged; only use the new command type:

```csharp
PushButtonData quantitySettingsBtn = new PushButtonData(
    "QuantitySettingCommand", "수량산출 설정",
    Assembly.GetExecutingAssembly().Location,
    RevitCommandType<QuantitySettingCommand>.FullName);
```

Append a dated `PROGRESS.md` entry listing the command extraction, removed QuantityView button/callback, ribbon routing, verification result, and the remaining manual Revit check for settings load/save/import/export.

- [ ] **Step 4: Run final verification**

Run:

```powershell
dotnet test tests\\DHBIMWATER.UI.Tests\\DHBIMWATER.UI.Tests.csproj
dotnet build src\\DHBIMWATER.Revit\\DHBIMWATER.Revit.csproj -c Release
```

Expected: UI tests pass and Revit project has zero compilation errors.

- [ ] **Step 5: Commit**

```powershell
git add src/DHBIMWATER.Revit/UI/Modules/QuantityRibbonModule.cs PROGRESS.md
git commit -m "feat: route quantity settings ribbon command"
```
