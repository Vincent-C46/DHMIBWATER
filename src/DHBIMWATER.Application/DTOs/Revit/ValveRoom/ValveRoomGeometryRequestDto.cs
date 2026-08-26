namespace DHBIMWATER.Application.DTOs.Revit.ValveRoom;

/// <summary>ValveRoomGeometryCalculator 입력. RoomType에 따라 MudSpec/SluiceSpec 중 하나만 채워진다.</summary>
public record ValveRoomGeometryRequestDto
(
    ValveRoomDesignConditionDto DesignConditionDto,
    ValveRoomPlanSpecDto PlanSpecDto,
    ValveRoomProfileSpecDto ProfileSpecDto,
    MudValveRoomSpecDto? MudSpec,
    SluiceValveRoomSpecDto? SluiceSpec
);
