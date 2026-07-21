namespace DHBIMWATER.Application.DTOs.Revit.ValveRoom;

/// <summary>제수밸브실 전용 스펙. RoomType이 "제수밸브실"일 때만 값이 채워진다.</summary>
public record SluiceValveRoomSpecDto
(
    int BeamCountX,
    double BeamOffsetX,
    double BeamSpacingX,
    int BeamCountY,
    double BeamOffsetY,
    double BeamSpacingY,
    string BeamTypeName,
    string ColumnTypeName
);
