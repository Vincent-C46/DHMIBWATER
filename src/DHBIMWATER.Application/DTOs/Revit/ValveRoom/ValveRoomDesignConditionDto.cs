namespace DHBIMWATER.Application.DTOs.Revit.ValveRoom;

/// <summary>밸브실 타입 및 기준 위치.</summary>
public record ValveRoomDesignConditionDto
(
    string RoomType,
    double ReferenceZ
);
