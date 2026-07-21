namespace DHBIMWATER.Application.DTOs.Revit.ValveRoom;

/// <summary>밸브실 타입 및 기준 표고.</summary>
public record ValveRoomDesignConditionDto
(
    string RoomType,
    /// <summary>기초 상부 EL(m). 부재 표고 계산의 기준이며 내부원점 표고는 항상 0이다.</summary>
    double FoundationTopEl
);
