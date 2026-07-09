using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.UI.ViewModels.Quantity;
using Xunit;

namespace DHBIMWATER.UI.Tests.ViewModels.Quantity;

public class ManualQuantityViewModelTests
{
    [Fact]
    public async Task MeasureLengthCommand_FillsVariableValueAndUnit()
    {
        var service = new StubMeasurePickService(new MeasureResult(2.5, "m"));
        var vm = new ManualQuantityViewModel(measureService: service)
        {
            RawFormula = "L"
        };
        var variable = vm.VariableInputs.Single();

        vm.MeasureLengthCommand.Execute(variable);
        await Task.Delay(10);

        Assert.Equal("2.500", variable.Value);
        Assert.Equal("m", variable.Unit);
        Assert.False(variable.IsMeasuring);
        Assert.Contains("2.500", vm.Preview);
        Assert.Equal(MeasureKind.Length, service.LastKind);
    }

    [Fact]
    public async Task MeasureAreaCommand_WhenCancelled_LeavesExistingValueUnchanged()
    {
        var service = new StubMeasurePickService(null);
        var vm = new ManualQuantityViewModel(measureService: service)
        {
            RawFormula = "A"
        };
        var variable = vm.VariableInputs.Single();
        variable.Value = "3.25";
        variable.Unit = string.Empty;

        vm.MeasureAreaCommand.Execute(variable);
        await Task.Delay(10);

        Assert.Equal("3.25", variable.Value);
        Assert.Equal(string.Empty, variable.Unit);
        Assert.False(variable.IsMeasuring);
        Assert.Equal(MeasureKind.Area, service.LastKind);
    }

    [Fact]
    public void MeasureCommands_AreDisabled_WhenServiceIsMissing()
    {
        var vm = new ManualQuantityViewModel();

        Assert.False(vm.MeasureLengthCommand.CanExecute(null));
        Assert.False(vm.MeasureAreaCommand.CanExecute(null));
    }

    private sealed class StubMeasurePickService : IMeasurePickService
    {
        private readonly MeasureResult? _result;

        public StubMeasurePickService(MeasureResult? result)
        {
            _result = result;
        }

        public MeasureKind? LastKind { get; private set; }

        public Task<MeasureResult?> PickAsync(MeasureKind kind)
        {
            LastKind = kind;
            return Task.FromResult<MeasureResult?>(_result);
        }
    }
}
