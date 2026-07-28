namespace DHBIMWATER.Application.DTOs.Revit.ValveRoom;

/// <summary>이토밸브실 전용 스펙. RoomType이 "이토밸브실"일 때만 값이 채워진다.</summary>
public record MudValveRoomSpecDto
(
    bool HasIntermediateWall,
    double IntermediateWallOffset,   // 좌측 외벽 내측(X=0) → 중간벽 좌측면까지의 안목거리(mm)
    double IntermediateWallThickness,
    bool HasIntermediateSlab,
    double IntermediateSlabThickness,
    double Floor1InnerHeight,
    double Floor2InnerHeight
);
