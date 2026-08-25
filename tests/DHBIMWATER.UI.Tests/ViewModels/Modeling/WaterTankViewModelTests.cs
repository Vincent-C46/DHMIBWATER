using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.UI.ViewModels.Modeling;
using System.Reflection;
using Xunit;

namespace DHBIMWATER.UI.Tests.ViewModels.Modeling;

public class WaterTankViewModelTests
{
    [Fact]
    public void Defaults_ExposeLargeReservoirDimensionsInMeters()
    {
        var vm = CreateViewModel();

        Assert.Equal(25, vm.W);
        Assert.Equal(30, vm.L);
        Assert.Equal(4, vm.M1);
        Assert.Equal(4, vm.M2);
        Assert.Equal(4, vm.M3);
        Assert.Equal(4, vm.M4);
        Assert.Equal(2.5, vm.Wh);
        Assert.Equal(2.5, vm.Lh);
        Assert.Equal(2, vm.Hh);
        Assert.Equal(4, vm.H1F);
        Assert.Equal(30, vm.Lv);
        Assert.Equal(5, vm.Wv);
        Assert.Equal(2.5, vm.We);
        Assert.Equal(1, vm.Wp);
        Assert.Equal(1, vm.Hp);
        Assert.Equal(0, vm.Ltt);
        Assert.Equal(0, vm.Lvt);
        Assert.Equal(32.4, vm.CRT, 2);
        Assert.Equal(2.95, vm.H2F, 2);
    }

    [Fact]
    public void BuildCreationRequestDto_ConvertsMeterInputsToMillimetersForCalculator()
    {
        var vm = CreateViewModel();

        var dto = BuildCreationRequestDto(vm);

        Assert.Equal(25000, dto.TankDto.W);
        Assert.Equal(30000, dto.TankDto.L);
        Assert.Equal(4000, dto.TankDto.M1);
        Assert.Equal(4000, dto.TankDto.M2);
        Assert.Equal(4000, dto.TankDto.M3);
        Assert.Equal(4000, dto.TankDto.M4);
        Assert.Equal(2500, dto.TankDto.Wh);
        Assert.Equal(2500, dto.TankDto.Lh);
        Assert.Equal(2000, dto.TankDto.Hh);
        Assert.Equal(0, dto.TankDto.Ltt);
        Assert.Equal(4000, dto.ValveDto.H1F);
        Assert.Equal(30000, dto.ValveDto.Lv);
        Assert.Equal(5000, dto.ValveDto.Wv);
        Assert.Equal(0, dto.ValveDto.Lvt);
        Assert.Equal(2500, dto.ValveDto.We);
        Assert.Equal(1000, dto.ValveDto.Wp);
        Assert.Equal(1000, dto.ValveDto.Hp);
        Assert.Equal(100, dto.ValveDto.TrOff);
        Assert.Equal(300, dto.ValveDto.WpThk);
    }

    private static WaterTankViewModel CreateViewModel()
        => new(
            useCase: null!,
            dialogService: new StubDialogService(),
            typeQueryRepo: new StubElementTypeQueryRepo(),
            fileDialogService: new StubFileDialogService());

    private static ReservoirCreationRequestDto BuildCreationRequestDto(WaterTankViewModel vm)
    {
        var method = typeof(WaterTankViewModel).GetMethod(
            "BuildCreationRequestDto",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        return Assert.IsType<ReservoirCreationRequestDto>(method!.Invoke(vm, null));
    }

    private sealed class StubElementTypeQueryRepo : IElementTypeQueryRepo
    {
        public IEnumerable<string> GetSlabTypeNames() => Array.Empty<string>();
        public IEnumerable<string> GetWallTypeNames() => Array.Empty<string>();
        public IEnumerable<string> GetColumnTypeNames() => new[] { "Column" };
        public IEnumerable<string> GetBeamTypeNames() => new[] { "Beam" };
        public IEnumerable<string> GetAdaptiveComponentTypeNames() => Array.Empty<string>();
        public IEnumerable<string> GetAdaptiveInstanceParameterNames(string familyTypeName) => Array.Empty<string>();
        public int GetAdaptiveBendPointCount(string familyTypeName) => -1;
        public IEnumerable<string> GetPipeAccessoryTypeNames() => Array.Empty<string>();
        public IEnumerable<string> GetPipeFittingTypeNames() => Array.Empty<string>();
        public IEnumerable<string> GetPipeAccessoryInstanceParameterNames(string familyTypeName) => Array.Empty<string>();
        public IEnumerable<string> GetGenericModelTypeNames() => Array.Empty<string>();
        public IEnumerable<string> GetBeamInstanceParameterNames(string beamTypeName) => Array.Empty<string>();
        public IEnumerable<string> GetPipingSystemTypeNames() => Array.Empty<string>();
        public IEnumerable<string> GetPipeTypeNames() => Array.Empty<string>();
        public IEnumerable<string> GetLevelNames() => Array.Empty<string>();
        public IEnumerable<string> GetFoundationTypeNames() => Array.Empty<string>();
    }

    private sealed class StubDialogService : IDialogService
    {
        public void Info(string title, string message) { }
        public void Warn(string title, string message) { }
        public bool Confirm(string title, string message) => true;
    }

    private sealed class StubFileDialogService : IFileDialogService
    {
        public string? OpenFile(string title, string filter) => null;
        public string? SaveFile(string title, string filter, string defaultFileName = "") => null;
    }
}
