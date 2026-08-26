using DHBIMWATER.Application.DTOs.Revit.ValveRoom;
using DHBIMWATER.Application.Services;
using Xunit;

namespace DHBIMWATER.UI.Tests.Services;

public class ValveRoomGeometryCalculatorTests
{
    [Fact]
    public void CalculateWalls_UsesClearDistanceToIntermediateWallLeftFace()
    {
        var dto = CreateMudValveRoomDto(intermediateWallOffset: 1200, intermediateWallThickness: 300);

        var wall = Assert.Single(ValveRoomGeometryCalculator.CalculateWalls(dto).Where(w => w.Part == "중간벽"));

        Assert.Equal(1350, wall.StartPoint.X);
        Assert.Equal(1350, wall.EndPoint.X);
    }

    [Fact]
    public void CalculateColumns_OffsetsTopBelowUpperSlabThickness()
    {
        var dto = CreateSluiceValveRoomDto(upperSlabThickness: 250);

        var column = Assert.Single(ValveRoomGeometryCalculator.CalculateColumns(dto));

        Assert.Equal(ValveRoomGeometryCalculator.TopLevelName, column.TopLevelName);
        Assert.Equal(-250, column.TopOffset);
    }

    [Fact]
    public void AirValveRoom_UsesIndependentFoundationInsteadOfFoundationFloor()
    {
        var dto = new ValveRoomGeometryRequestDto(
            new ValveRoomDesignConditionDto("공기밸브실", 123.45),
            new ValveRoomPlanSpecDto(1500, 1800),
            new ValveRoomProfileSpecDto(2000, 100, 650, 300, 300, 400),
            null,
            null);

        var foundation = Assert.Single(ValveRoomGeometryCalculator.CalculateFoundations(dto));

        Assert.DoesNotContain(ValveRoomGeometryCalculator.CalculateSlabs(dto), slab => slab.Part == "기초");
        Assert.Equal(900, foundation.Position.X);
        Assert.Equal(750, foundation.Position.Y);
        Assert.Equal(123450, foundation.Position.Z);
        Assert.Equal(3300, foundation.Length);
        Assert.Equal(3000, foundation.Width);
        Assert.Equal(650, foundation.Thickness);
    }

    [Fact]
    public void AirValveRoom_ExcludesPlainConcreteAndPlacesXAxisVoidAtPipeCenterDepth()
    {
        var dto = new ValveRoomGeometryRequestDto(
            new ValveRoomDesignConditionDto("공기밸브실", 123.45),
            new ValveRoomPlanSpecDto(1500, 1800),
            new ValveRoomProfileSpecDto(2000, 100, 650, 100, 300, 400),
            null,
            null);

        var voidDefinition = ValveRoomGeometryCalculator.CalculateAirValveVoid(dto, new AirValveRoomSpecDto(300, 250, "X"));

        Assert.DoesNotContain(ValveRoomGeometryCalculator.CalculateSlabs(dto), slab => slab.Part == "버림콘크리트");
        Assert.Equal(0, voidDefinition.PipeCenter.X);
        Assert.Equal(750, voidDefinition.PipeCenter.Y);
        Assert.Equal(123200, voidDefinition.PipeCenter.Z);
        Assert.Equal(150, voidDefinition.Radius);
        Assert.Equal("X", voidDefinition.Axis);
    }

    [Fact]
    public void AirValveRoom_PlacesYAxisVoidAtPlanLengthCenter()
    {
        var dto = new ValveRoomGeometryRequestDto(
            new ValveRoomDesignConditionDto("공기밸브실", 0),
            new ValveRoomPlanSpecDto(1500, 1800),
            new ValveRoomProfileSpecDto(2000, 100, 650, 100, 300, 400),
            null,
            null);

        var voidDefinition = ValveRoomGeometryCalculator.CalculateAirValveVoid(dto, new AirValveRoomSpecDto(200, 325, "Y"));

        Assert.Equal(900, voidDefinition.PipeCenter.X);
        Assert.Equal(0, voidDefinition.PipeCenter.Y);
        Assert.Equal(-325, voidDefinition.PipeCenter.Z);
        Assert.Equal(100, voidDefinition.Radius);
        Assert.Equal("Y", voidDefinition.Axis);
    }

    [Theory]
    [InlineData("이토밸브실")]
    [InlineData("제수밸브실")]
    [InlineData("공기밸브실")]
    public void CalculateGeometry_UsesInnerHeightFromFoundationTopToUpperSlabUnderside(string roomType)
    {
        const double foundationTopEl = 123.45;
        const double innerHeight = 2500;
        const double upperSlabThickness = 400;
        var dto = new ValveRoomGeometryRequestDto(
            new ValveRoomDesignConditionDto(roomType, foundationTopEl),
            new ValveRoomPlanSpecDto(2000, 3000),
            new ValveRoomProfileSpecDto(innerHeight, 100, 500, 300, 300, upperSlabThickness),
            roomType == "이토밸브실" ? new MudValveRoomSpecDto(false, 0, 0, false, 0, 0, 0) : null,
            null);

        var topLevel = Assert.Single(ValveRoomGeometryCalculator.CalculateLevels(dto).Where(l => l.Name == ValveRoomGeometryCalculator.TopLevelName));
        var upperSlab = Assert.Single(ValveRoomGeometryCalculator.CalculateSlabs(dto).Where(s => s.Part == "상부슬래브"));
        var wall = Assert.Single(ValveRoomGeometryCalculator.CalculateWalls(dto).Where(w => w.Part == "외벽"));

        Assert.Equal(126350, topLevel.Elevation);
        Assert.Equal(topLevel.Elevation, upperSlab.ElevationZ);
        Assert.Equal(innerHeight, wall.Height);
    }

    private static ValveRoomGeometryRequestDto CreateMudValveRoomDto(double intermediateWallOffset, double intermediateWallThickness) => new(
        new ValveRoomDesignConditionDto("이토밸브실", 0),
        new ValveRoomPlanSpecDto(2500, 4000),
        new ValveRoomProfileSpecDto(2500, 100, 500, 300, 300, 250),
        new MudValveRoomSpecDto(true, intermediateWallOffset, intermediateWallThickness, false, 0, 0, 0),
        null);

    private static ValveRoomGeometryRequestDto CreateSluiceValveRoomDto(double upperSlabThickness) => new(
        new ValveRoomDesignConditionDto("제수밸브실", 0),
        new ValveRoomPlanSpecDto(2000, 3000),
        new ValveRoomProfileSpecDto(2500, 100, 500, 300, 300, upperSlabThickness),
        null,
        new SluiceValveRoomSpecDto(1, 500, 0, 1, 500, 0, "Beam", "Column"));
}
