namespace DHBIMWATER.Application.DTOs.Revit.ValveRoom;

/// <summary>독립 밸브실 구조물 생성에 필요한 치수는 mm, 공유좌표는 m, 도북각은 degree 단위이다.</summary>
public record ValveRoomRequestDto
{
    public string RoomType { get; init; } = string.Empty;
    public double ReferenceX { get; init; }
    /// <summary>내부공간 좌하단 내부원점의 동쪽 방향 공유좌표(m).</summary>
    public double SharedCoordinateX { get; init; }
    /// <summary>내부공간 좌하단 내부원점의 북쪽 방향 공유좌표(m).</summary>
    public double SharedCoordinateY { get; init; }
    /// <summary>내부공간 좌하단 내부원점의 공유표고(m).</summary>
    public double SharedElevation { get; init; }
    /// <summary>진북에서 도북(프로젝트 북)까지 시계방향 각도(degree).</summary>
    public double TrueNorthToProjectNorthClockwiseDegrees { get; init; }
    public double ReferenceY { get; init; }
    public double ReferenceZ { get; init; }
    public double PlainConcreteThickness { get; init; }
    public double FoundationThickness { get; init; }
    public double FoundationToe { get; init; }
    public double OuterWallThickness { get; init; }
    public double IntermediateWallThickness { get; init; }
    public double UpperSlabThickness { get; init; }
    public double IntermediateSlabThickness { get; init; }
    public double InnerWidth { get; init; }
    public double InnerLength { get; init; }
    public double InnerHeight { get; init; }
    public bool HasIntermediateWall { get; init; }
    public int IntermediateWallCount { get; init; }
    public bool HasIntermediateSlab { get; init; }
    public double Floor1InnerHeight { get; init; }
    public double Floor2InnerHeight { get; init; }
    public int BeamCountX { get; init; }
    public double BeamOffsetX { get; init; }
    public double BeamSpacingX { get; init; }
    public int BeamCountY { get; init; }
    public double BeamOffsetY { get; init; }
    public double BeamSpacingY { get; init; }
    public string BeamTypeName { get; init; } = string.Empty;
    public string ColumnTypeName { get; init; } = string.Empty;
}