using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Piping;
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
    public void StraightLengthMm_RejectsNonPositiveValue_AndAcceptsCustomValue()
    {
        var vm = CreateViewModel();

        Assert.Throws<ArgumentOutOfRangeException>(() => vm.StraightLengthMm = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => vm.StraightLengthMm = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => vm.StraightLengthMm = double.NaN);

        vm.StraightLengthMm = 500;

        Assert.Equal(500, vm.StraightLengthMm);
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

    [Fact]
    public void UndoCommand_RestoresPreviousNetworkAfterDrawing()
    {
        var vm = CreateViewModel();
        vm.HandleCanvasSizeChanged(1000, 1000);
        vm.HandleCanvasClick(vm.Transform.ToScreen(new Point2D(0, 0)));
        vm.HandleCanvasClick(vm.Transform.ToScreen(new Point2D(1000, 0)));
        Assert.Single(vm.Edges);

        vm.UndoCommand.Execute(null);

        Assert.Empty(vm.Edges);
        Assert.False(vm.UndoCommand.CanExecute(null));
    }

    [Fact]
    public void ValidationSummary_ReportsNonOrthogonalThreeWayBranch()
    {
        var vm = CreateViewModel();
        vm.HandleCanvasSizeChanged(1000, 1000);
        Draw(vm, new(-1000, 0), new(1000, 0));
        Draw(vm, new(0, 0), new(1000, 1000));

        Assert.Contains("직교 T가 아닌 3방향 분기 1개", vm.ValidationSummary);
    }

    [Fact]
    public void CreateModelCommand_UsesSelectedSegmentFamilies_AndWaitsForCompletion()
    {
        var vm = CreateViewModel();
        vm.HandleCanvasSizeChanged(1000, 1000);
        Draw(vm, new(0, 0), new(1000, 0));
        PipeNetworkDefinition? request = null;
        vm.CreateModelAction = value => request = value;

        vm.CreateModelCommand.Execute(null);

        Assert.NotNull(request);
        Assert.Equal(PipeOutputMode.PipeAccessorySegment, request!.OutputMode);
        Assert.Equal("1층", request.LevelName);

        // 패밀리·파라미터는 스텁 목록 이름에서 자동 추정된다.
        Assert.NotNull(request.SegmentFamilies);
        Assert.Equal("직관 : DN100", request.SegmentFamilies!.SegmentFamilyTypeName);
        Assert.Equal("길이", request.SegmentFamilies.LengthParameterName);

        Assert.True(vm.IsCreating);
        Assert.False(vm.CreateModelCommand.CanExecute(null));

        vm.ApplyModelCreationResult(true, "완료");
        Assert.False(vm.IsCreating);
        Assert.Contains("완료", vm.Status);
    }

    [Fact]
    public void Segment_family_selection_is_auto_guessed_from_the_fitting_list()
    {
        var vm = CreateViewModel();

        Assert.Equal("직관 : DN100", vm.SegmentFamilyTypeName);
        Assert.Equal("곡관90 : DN100", vm.Bend90FamilyTypeName);
        Assert.Equal("곡관45 : DN100", vm.Bend45FamilyTypeName);
        Assert.Equal("T형 : DN100", vm.TeeFamilyTypeName);
        Assert.Equal("길이", vm.LengthParameterName);
    }

    [Fact]
    public void Accessory_and_segment_family_lists_are_separated()
    {
        var vm = CreateViewModel();

        Assert.Equal(["밸브 : DN100"], vm.FamilyTypeNames);
        Assert.Equal(
            ["직관 : DN100", "단관 : DN100", "곡관90 : DN100", "곡관45 : DN100", "T형 : DN100"],
            vm.SegmentFamilyTypeNames);
        Assert.Contains(vm.SegmentFamilyTypeName, vm.SegmentFamilyTypeNames);
        Assert.Contains(vm.Bend90FamilyTypeName, vm.SegmentFamilyTypeNames);
        Assert.Contains(vm.Bend45FamilyTypeName, vm.SegmentFamilyTypeNames);
        Assert.Contains(vm.TeeFamilyTypeName, vm.SegmentFamilyTypeNames);
    }

    [Fact]
    public void Missing_length_parameter_blocks_model_creation()
    {
        var vm = CreateViewModel();
        vm.HandleCanvasSizeChanged(1000, 1000);
        Draw(vm, new(0, 0), new(1000, 0));
        vm.CreateModelAction = _ => { };
        Assert.True(vm.CreateModelCommand.CanExecute(null));

        vm.LengthParameterName = null;

        Assert.False(vm.CreateModelCommand.CanExecute(null));
        Assert.Contains("관 길이 파라미터", vm.ValidationSummary);
    }

    [Fact]
    public void CreateModelCommand_PassesCustomStraightLength()
    {
        var vm = CreateViewModel();
        vm.HandleCanvasSizeChanged(1000, 1000);
        Draw(vm, new(0, 0), new(1000, 0));
        vm.StraightLengthMm = 500;
        PipeNetworkDefinition? request = null;
        vm.CreateModelAction = value => request = value;

        vm.CreateModelCommand.Execute(null);

        Assert.NotNull(request);
        Assert.Equal(500, request!.SegmentFamilies!.StraightLengthMm);
    }

    [Fact]
    public void ValidationSummary_RequiresSegmentFamilyAndLengthParameter()
    {
        var vm = CreateViewModel();

        vm.SegmentFamilyTypeName = null;
        Assert.Contains("관 패밀리를 선택하세요.", vm.ValidationSummary);

        vm.SegmentFamilyTypeName = "직관 : DN100";
        vm.LengthParameterName = null;
        Assert.Contains("관 길이 파라미터를 선택하세요.", vm.ValidationSummary);
    }

    private static void Draw(PipeLayoutViewModel vm, Point2D start, Point2D end)
    {
        vm.HandleCanvasClick(vm.Transform.ToScreen(start));
        vm.HandleCanvasClick(vm.Transform.ToScreen(end));
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
        public IEnumerable<string> GetPipingSystemTypeNames() => ["급수"];
        public IEnumerable<string> GetPipeTypeNames() => ["배관"];
        public IEnumerable<string> GetLevelNames() => ["1층"];
        public IEnumerable<string> GetPipeAccessoryTypeNames() => ["밸브 : DN100"];
        public IEnumerable<string> GetPipeFittingTypeNames() =>
            ["직관 : DN100", "단관 : DN100", "곡관90 : DN100", "곡관45 : DN100", "T형 : DN100"];
        public IEnumerable<string> GetPipeAccessoryInstanceParameterNames(string familyTypeName) => ["길이", "DN"];
        public IEnumerable<string> GetGenericModelTypeNames() => [];
        public IEnumerable<string> GetFoundationTypeNames() => [];
        public IEnumerable<string> GetBeamInstanceParameterNames(string beamTypeName) => [];
    }
}
