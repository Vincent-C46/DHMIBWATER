namespace DHBIMWATER.Application.DTOs.Revit.ValveRoom;

/// <summary>독립 밸브실 구조물 생성에 필요한 치수는 mm, 공유좌표는 m, 도북각은 degree 단위이다.
/// 타입 전용 입력은 RoomType에 대응하는 MudSpec/SluiceSpec 중 하나만 채운다.</summary>
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
    public double UpperSlabThickness { get; init; }
    public double InnerWidth { get; init; }
    public double InnerLength { get; init; }
    public double InnerHeight { get; init; }
    /// <summary>RoomType이 "이토밸브실"일 때만 값이 채워진다.</summary>
    public MudValveRoomSpecDto? MudSpec { get; init; }
    /// <summary>RoomType이 "제수밸브실"일 때만 값이 채워진다.</summary>
    public SluiceValveRoomSpecDto? SluiceSpec { get; init; }
}
