using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.UI.ViewModels.Modeling;
using System.Windows;
using Xunit;

namespace DHBIMWATER.UI.Tests.ViewModels.Modeling;

public sealed class PipeLayoutViewModelTests
{
    [Fact]
    public void HandleCanvasMove_SnapsToReferenceAxis_WhenNearestSnapIsEnabled()
    {
        var vm = CreateViewModel();
        vm.HandleCanvasSizeChanged(1000, 1000);
        vm.SnapReferencePoint = false;

        vm.HandleCanvasMove(vm.Transform.ToScreen(new Point2D(50, 500)));

        var expected = vm.Transform.ToScreen(new Point2D(0, 500));
        Assert.True(vm.IsSnapMarkerVisible);
        Assert.Equal(expected.X, vm.SnapMarkerX, 6);
        Assert.Equal(expected.Y, vm.SnapMarkerY, 6);
        Assert.Equal("·", vm.SnapMarkerSymbol);
    }

    [Fact]
    public void HandleCanvasMove_DoesNotSnapToReferenceAxis_WhenNearestSnapIsDisabled()
    {
        var vm = CreateViewModel();
        vm.HandleCanvasSizeChanged(1000, 1000);
        vm.SnapReferencePoint = false;
        vm.SnapNearest = false;

        vm.HandleCanvasMove(vm.Transform.ToScreen(new Point2D(50, 500)));

        Assert.False(vm.IsSnapMarkerVisible);
    }

    [Fact]
    public void DrawingPreview_UsesSameReferenceAxisSnapAsConfirmedPoint()
    {
        var vm = CreateViewModel();
        vm.HandleCanvasSizeChanged(1000, 1000);
        vm.SnapReferencePoint = false;
        vm.HandleCanvasClick(vm.Transform.ToScreen(new Point2D(500, 500)));

        vm.HandleCanvasMove(vm.Transform.ToScreen(new Point2D(50, 1000)));

        var expected = vm.Transform.ToScreen(new Point2D(0, 1000));
        Assert.Equal(expected.X, vm.PreviewX2, 6);
        Assert.Equal(expected.Y, vm.PreviewY2, 6);
    }

    [Fact]
    public void GridSnapMm_AcceptsCustomPositiveValue_AndRejectsNonPositiveValue()
    {
        var vm = CreateViewModel();

        vm.GridSnapMm = 25;

        Assert.Equal(25, vm.GridSnapMm);
        Assert.Throws<ArgumentOutOfRangeException>(() => vm.GridSnapMm = 0);
        Assert.Equal(25, vm.GridSnapMm);
    }

    [Fact]
    public void RemoveFittingCommand_RemovesFittingWithoutSelectedEdge()
    {
        var vm = CreateViewModel();
        vm.HandleCanvasSizeChanged(1000, 1000);
        vm.HandleCanvasClick(vm.Transform.ToScreen(new Point2D(0, 0)));
        vm.HandleCanvasClick(vm.Transform.ToScreen(new Point2D(1000, 0)));
        vm.SelectedFamilyTypeName = "밸브 : DN100";
        vm.HandleCanvasMove(vm.Transform.ToScreen(new Point2D(500, 0)));
        vm.HandleCanvasClick(vm.Transform.ToScreen(new Point2D(500, 0)));
        var fitting = Assert.Single(vm.InlineFittings);

        vm.RemoveFittingCommand.Execute(fitting);

        Assert.Empty(vm.InlineFittings);
    }

    private static PipeLayoutViewModel CreateViewModel() => new(new StubElementTypeQueryRepo());

    private sealed class StubElementTypeQueryRepo : IElementTypeQueryRepo
    {
        public IEnumerable<string> GetSlabTypeNames() => [];
        public IEnumerable<string> GetWallTypeNames() => [];
        public IEnumerable<string> GetColumnTypeNames() => [];
        public IEnumerable<string> GetBeamTypeNames() => [];
        public IEnumerable<string> GetAdaptiveComponentTypeNames() => [];
        public IEnumerable<string> GetAdaptiveInstanceParameterNames(string familyTypeName) => [];
        public int GetAdaptiveBendPointCount(string familyTypeName) => -1;
        public IEnumerable<string> GetPipingSystemTypeNames() => [];
        public IEnumerable<string> GetPipeTypeNames() => [];
        public IEnumerable<string> GetPipeAccessoryTypeNames() => ["밸브 : DN100"];
        public IEnumerable<string> GetGenericModelTypeNames() => [];
        public IEnumerable<string> GetFoundationTypeNames() => [];
        public IEnumerable<string> GetBeamInstanceParameterNames(string beamTypeName) => [];
    }
}
