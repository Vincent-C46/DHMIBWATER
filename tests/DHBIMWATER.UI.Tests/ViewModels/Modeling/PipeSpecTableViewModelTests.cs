using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Application.UseCases.Gis;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Infrastructure.Services.Mock;
using DHBIMWATER.UI.ViewModels.Modeling;
using Xunit;

namespace DHBIMWATER.UI.Tests.ViewModels.Modeling;

public sealed class PipeSpecTableViewModelTests
{
    [Fact]
    public void JointDeflections_SplitByJointTypeAndPreserveUnknownRows()
    {
        var unknown = new JointDeflectionSpec("구형 조인트", 250, 2.5);
        var source = BendSettings.Default with
        {
            JointDeflections = new JointDeflectionTable(new[]
            {
                new JointDeflectionSpec(JointTypeCatalog.KpMechanical.ToLowerInvariant(), 100, 5),
                new JointDeflectionSpec(JointTypeCatalog.Tyton, 200, 4),
                unknown
            })
        };
        var vm = new JointDeflectionSettingsViewModel(source);

        Assert.Single(vm.KpRows);
        Assert.Single(vm.TytonRows);
        Assert.Equal(JointTypeCatalog.KpMechanical, vm.KpRows[0].JointType);
        Assert.Equal(JointTypeCatalog.Tyton, vm.TytonRows[0].JointType);

        var result = vm.BuildResult(source);
        Assert.Contains(unknown, result.JointDeflections.Entries);
    }

    [Fact]
    public void JointDeflections_AddCommandsTargetTheirOwnCollections()
    {
        var vm = new JointDeflectionSettingsViewModel(BendSettings.Default);
        var kpCount = vm.KpRows.Count;
        var tytonCount = vm.TytonRows.Count;

        vm.AddKpCommand.Execute(null);
        vm.AddTytonCommand.Execute(null);

        Assert.Equal(kpCount + 1, vm.KpRows.Count);
        Assert.Equal(tytonCount + 1, vm.TytonRows.Count);
        Assert.Equal(JointTypeCatalog.KpMechanical, vm.KpRows[^1].JointType);
        Assert.Equal(JointTypeCatalog.Tyton, vm.TytonRows[^1].JointType);
    }

    [Fact]
    public void Confirm_SavesProjectAndCloses()
    {
        var repo = new StubRepo();
        var vm = Create(repo);
        var closed = false;
        vm.CloseAction = () => closed = true;

        vm.SaveCommand.Execute(null);

        Assert.NotNull(repo.Saved);
        Assert.NotNull(vm.Result);
        Assert.True(closed);
    }

    [Fact]
    public void Export_SavesCurrentScreenToSelectedFileWithoutSavingProjectOrClosing()
    {
        var repo = new StubRepo();
        var dialogs = new StubFileDialog { SavePath = "export.json" };
        var files = new StubFileStore();
        var vm = Create(repo, dialogs, files);
        var closed = false;
        vm.CloseAction = () => closed = true;
        vm.SocketFittingRows[0].WallThicknessMm = 123;

        vm.ExportCommand.Execute(null);

        Assert.Null(repo.Saved);
        Assert.Equal("export.json", files.SavedPath);
        Assert.Equal(123, files.Saved!.Fittings.Entries[0].WallThicknessMm);
        Assert.False(closed);
    }

    [Fact]
    public void Import_LoadsScreenWithoutSavingProjectUntilConfirm()
    {
        var repo = new StubRepo();
        var dialogs = new StubFileDialog { OpenPath = "import.json" };
        var imported = new BendSettings(
            new StraightPipeSpecTable(new[] { new StraightPipeSpec(PipeKindCatalog.Water2, 100, 118, 6.8) }),
            new JointDeflectionTable(new[] { new JointDeflectionSpec(JointTypeCatalog.Tyton, 100, 3) }),
            new BendFittingCatalog(new[] { new BendFittingEntry(100, 45, BendForm.BType, 120, 210, 8.8) }),
            JointTypeCatalog.Tyton, JointApplicationMode.BothJoints, BendConnection.Flanged);
        var vm = Create(repo, dialogs, new StubFileStore { LoadResult = imported });

        vm.ImportCommand.Execute(null);

        Assert.Null(repo.Saved);
        Assert.Null(vm.Result);
        Assert.Single(vm.SocketFittingRows);
        Assert.Equal(8.8, vm.SocketFittingRows[0].WallThicknessMm);
        Assert.Equal(JointTypeCatalog.Tyton, vm.Joint.ActiveJointType);
        Assert.Equal(JointApplicationMode.BothJoints, vm.Joint.ApplicationMode);

        vm.SaveCommand.Execute(null);

        Assert.NotNull(repo.Saved);
        Assert.Equal(BendConnection.Flanged, repo.Saved!.ActiveBendConnection);
        Assert.Equal(8.8, repo.Saved.Fittings.Find(100, 45)!.WallThicknessMm);
    }

    [Fact]
    public void ImportFailure_KeepsCurrentRowsAndWarns()
    {
        var dialog = new StubDialog();
        var vm = Create(new StubRepo(), new StubFileDialog { OpenPath = "bad.json" },
            new StubFileStore { LoadException = new InvalidDataException("bad") }, dialog);
        var originalCount = vm.SocketFittingRows.Count;

        vm.ImportCommand.Execute(null);

        Assert.Equal(originalCount, vm.SocketFittingRows.Count);
        Assert.Contains("불러오지 못했습니다", dialog.Warning);
    }

    [Fact]
    public void FittingRows_AreSplitAndAddCommandTargetsActiveTab()
    {
        var vm = Create(new StubRepo());

        Assert.Equal(72, vm.SocketFittingRows.Count);
        Assert.Equal(36, vm.FlangedFittingRows.Count);

        vm.SelectedFittingTabIndex = 1;
        vm.AddFittingCommand.Execute(null);

        Assert.Equal(72, vm.SocketFittingRows.Count);
        Assert.Equal(37, vm.FlangedFittingRows.Count);
        Assert.Equal(BendConnection.Flanged, vm.FlangedFittingRows[^1].Connection);
    }

    private static PipeSpecTableViewModel Create(StubRepo repo, StubFileDialog? fileDialog = null,
        StubFileStore? fileStore = null, StubDialog? dialog = null)
        => new(BendSettings.Default, new SaveBendSettingsUseCase(new StubTransaction(), repo), dialog ?? new StubDialog(),
            fileDialog ?? new StubFileDialog(), fileStore ?? new StubFileStore(), new DirectRevitDispatcher());

    private sealed class StubTransaction : ITransactionContext
    {
        public void Begin(string name) { }
        public void Commit() { }
        public void Rollback() { }
        public void Dispose() { }
    }

    private sealed class StubRepo : IBendSettingsRepo
    {
        public BendSettings? Saved { get; private set; }
        public BendSettings? Load() => Saved;
        public void Save(BendSettings settings) => Saved = settings;
    }

    private sealed class StubFileDialog : IFileDialogService
    {
        public string? OpenPath { get; init; }
        public string? SavePath { get; init; }
        public string? OpenFile(string title, string filter) => OpenPath;
        public string? SaveFile(string title, string filter, string defaultFileName = "") => SavePath;
    }

    private sealed class StubFileStore : IBendSettingsFileStore
    {
        public BendSettings? LoadResult { get; init; }
        public Exception? LoadException { get; init; }
        public string? SavedPath { get; private set; }
        public BendSettings? Saved { get; private set; }
        public BendSettings Load(string path) => LoadException is not null ? throw LoadException : LoadResult ?? BendSettings.Default;
        public void Save(string path, BendSettings settings) { SavedPath = path; Saved = settings; }
    }

    private sealed class StubDialog : IDialogService
    {
        public string? Warning { get; private set; }
        public void Info(string title, string message) { }
        public void Warn(string title, string message) => Warning = message;
        public bool Confirm(string title, string message) => true;
    }
}
