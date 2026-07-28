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
