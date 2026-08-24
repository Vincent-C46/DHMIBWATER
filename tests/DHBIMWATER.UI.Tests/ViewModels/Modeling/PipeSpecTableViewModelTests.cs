using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Application.UseCases.Gis;
using DHBIMWATER.Core.Gis;
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
        var master = new StubMaster();
        var vm = Create(repo, master);
        var closed = false;
        vm.CloseAction = () => closed = true;

        vm.SaveCommand.Execute(null);

        Assert.NotNull(repo.Saved);
        Assert.Null(master.Saved);
        Assert.NotNull(vm.Result);
        Assert.True(closed);
    }

    [Fact]
    public void SaveToMaster_SavesWithoutClosing()
    {
        var repo = new StubRepo();
        var master = new StubMaster();
        var vm = Create(repo, master);
        var closed = false;
        vm.CloseAction = () => closed = true;

        vm.SaveToMasterCommand.Execute(null);

        Assert.NotNull(repo.Saved);
        Assert.NotNull(master.Saved);
        Assert.False(closed);
    }

    [Fact]
    public void FittingRows_AreSplitAndAddCommandTargetsActiveTab()
    {
        var vm = Create(new StubRepo(), new StubMaster());

        Assert.Equal(72, vm.SocketFittingRows.Count);
        Assert.Equal(36, vm.FlangedFittingRows.Count);

        vm.SelectedFittingTabIndex = 1;
        vm.AddFittingCommand.Execute(null);

        Assert.Equal(72, vm.SocketFittingRows.Count);
        Assert.Equal(37, vm.FlangedFittingRows.Count);
        Assert.Equal(BendConnection.Flanged, vm.FlangedFittingRows[^1].Connection);
    }

    private static PipeSpecTableViewModel Create(StubRepo repo, StubMaster master)
        => new(BendSettings.Default, new SaveBendSettingsUseCase(new StubTransaction(), repo, master), new StubDialog());

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

    private sealed class StubMaster : IBendSettingsMasterStore
    {
        public BendSettings? Saved { get; private set; }
        public BendSettings? Load() => Saved;
        public void Save(BendSettings settings) => Saved = settings;
    }

    private sealed class StubDialog : IDialogService
    {
        public void Info(string title, string message) { }
        public void Warn(string title, string message) { }
        public bool Confirm(string title, string message) => true;
    }
}
