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
