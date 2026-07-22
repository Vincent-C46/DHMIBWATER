using DHBIMWATER.Application.DTOs.Revit.ValveRoom;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Application.Services;

public class ValveRoomGeometryCalculator
{
    public const string BaseLevelName = "밸브실 기초 상부";
    public const string TopLevelName = "밸브실 상부슬래브";

    /// <summary>기초 상부 EL(m)을 프로젝트 내부 단위(mm)로 환산한 기준 레벨 표고.</summary>
    public static double BaseElevation(ValveRoomGeometryRequestDto dto) => dto.DesignConditionDto.FoundationTopEl * 1000;

    public static double TopElevation(ValveRoomGeometryRequestDto dto) => BaseElevation(dto) + RoomHeight(dto);

    public static IReadOnlyList<LevelDefinition> CalculateLevels(ValveRoomGeometryRequestDto dto) => new List<LevelDefinition>
    {
        new LevelDefinition { Name = BaseLevelName, Elevation = BaseElevation(dto) },
        new LevelDefinition { Name = TopLevelName, Elevation = TopElevation(dto) },
    };

    public static IReadOnlyList<SlabDefinition> CalculateSlabs(ValveRoomGeometryRequestDto dto)
    {
        var d = dto.DesignConditionDto;
        var pl = dto.PlanSpecDto;
        var pr = dto.ProfileSpecDto;
        var outerWidth = pl.InnerWidth + pr.OuterWallThickness * 2;
        var outerLength = pl.InnerLength + pr.OuterWallThickness * 2;
        var foundationWidth = outerWidth + pr.FoundationToe * 2;
        var foundationLength = outerLength + pr.FoundationToe * 2;
        var baseZ = BaseElevation(dto);
        var slabs = new List<SlabDefinition>
        {
            Slab(dto, pl.InnerWidth / 2, pl.InnerLength / 2, foundationWidth, foundationLength, pr.PlainConcreteThickness, baseZ - pr.FoundationThickness, "버림콘크리트"),
            Slab(dto, pl.InnerWidth / 2, pl.InnerLength / 2, foundationWidth, foundationLength, pr.FoundationThickness, baseZ, "기초")
        };
        if (d.RoomType == "이토밸브실" && dto.MudSpec is { HasIntermediateSlab: true } mud)
            slabs.Add(Slab(dto, pl.InnerWidth / 2, pl.InnerLength / 2, outerWidth, outerLength, mud.IntermediateSlabThickness, baseZ + mud.Floor1InnerHeight, "중간슬래브"));
        slabs.Add(Slab(dto, pl.InnerWidth / 2, pl.InnerLength / 2, outerWidth, outerLength, pr.UpperSlabThickness, TopElevation(dto), "상부슬래브"));
        return slabs;
    }

    public static IReadOnlyList<LinearWallDefinition> CalculateWalls(ValveRoomGeometryRequestDto dto)
    {
        var d = dto.DesignConditionDto; var pl = dto.PlanSpecDto; var pr = dto.ProfileSpecDto;
        var x0 = -pr.OuterWallThickness / 2; var x1 = pl.InnerWidth + pr.OuterWallThickness / 2;
        var y0 = -pr.OuterWallThickness / 2; var y1 = pl.InnerLength + pr.OuterWallThickness / 2;
        var walls = new List<LinearWallDefinition>();
        var outerCorners = new[]
        {
            (new Point3D(x0, y0, 0), new Point3D(x1, y0, 0)), (new Point3D(x1, y0, 0), new Point3D(x1, y1, 0)),
            (new Point3D(x1, y1, 0), new Point3D(x0, y1, 0)), (new Point3D(x0, y1, 0), new Point3D(x0, y0, 0))
        };
        if (d.RoomType == "이토밸브실" && dto.MudSpec is { HasIntermediateSlab: true } slabMud)
        {
            foreach (var (start, end) in outerCorners)
            {
                walls.Add(Wall(dto, start, end, pr.OuterWallThickness, slabMud.Floor1InnerHeight, "외벽", isExterior: true));
                // BaseOffset은 기준 레벨(밸브실 기초 상부) 기준 상대 오프셋이므로 절대표고를 더하지 않는다.
                walls.Add(Wall(dto, start, end, pr.OuterWallThickness, slabMud.Floor2InnerHeight, "외벽", isExterior: true,
                    baseOffset: slabMud.Floor1InnerHeight + slabMud.IntermediateSlabThickness));
            }
        }
        else
        {
            foreach (var (start, end) in outerCorners) walls.Add(Wall(dto, start, end, pr.OuterWallThickness, RoomHeight(dto), "외벽", isExterior: true));
        }

        if (d.RoomType != "이토밸브실" || dto.MudSpec is not { HasIntermediateWall: true } mud) return walls;
        for (var i = 1; i <= mud.IntermediateWallCount; i++)
        {
            var x = pl.InnerWidth * i / (mud.IntermediateWallCount + 1d);
            walls.Add(Wall(dto, new Point3D(x, 0, 0), new Point3D(x, pl.InnerLength, 0), mud.IntermediateWallThickness, RoomHeight(dto), "중간벽", isExterior: false));
        }
        return walls;
    }

    public static IReadOnlyList<BeamDefinition> CalculateBeams(ValveRoomGeometryRequestDto dto)
    {
        var d = dto.DesignConditionDto; var pl = dto.PlanSpecDto;
        if (d.RoomType != "제수밸브실" || dto.SluiceSpec is not { } sluice) return Array.Empty<BeamDefinition>();
        var z = TopElevation(dto); var x0 = 0d; var x1 = pl.InnerWidth;
        var y0 = 0d; var y1 = pl.InnerLength;
        var beams = new List<BeamDefinition>();
        for (var i = 0; i < sluice.BeamCountX; i++)
            beams.Add(Beam(dto, new Point3D(x0, y0 + sluice.BeamOffsetX + sluice.BeamSpacingX * i, z), new Point3D(x1, y0 + sluice.BeamOffsetX + sluice.BeamSpacingX * i, z), "X방향 보"));
        for (var i = 0; i < sluice.BeamCountY; i++)
            beams.Add(Beam(dto, new Point3D(x0 + sluice.BeamOffsetY + sluice.BeamSpacingY * i, y0, z), new Point3D(x0 + sluice.BeamOffsetY + sluice.BeamSpacingY * i, y1, z), "Y방향 보"));
        return beams;
    }

    public static IReadOnlyList<ColumnDefinition> CalculateColumns(ValveRoomGeometryRequestDto dto)
    {
        var d = dto.DesignConditionDto;
        if (d.RoomType != "제수밸브실" || dto.SluiceSpec is not { } sluice) return Array.Empty<ColumnDefinition>();
        var columns = new List<ColumnDefinition>();
        for (var x = 0; x < sluice.BeamCountY; x++)
            for (var y = 0; y < sluice.BeamCountX; y++)
                columns.Add(new ColumnDefinition
                {
                    Position = new Point3D(sluice.BeamOffsetY + sluice.BeamSpacingY * x, sluice.BeamOffsetX + sluice.BeamSpacingX * y, BaseElevation(dto)),
                    TypeName = sluice.ColumnTypeName,
                    BaseLevelName = BaseLevelName,
                    TopLevelName = TopLevelName,
                    // 상부 레벨이 이미 상부슬래브 표고이므로 추가 오프셋이 필요 없다.
                    TopOffset = 0,
                    ElementCode = "VR-C",
                    Zone = d.RoomType,
                    Part = "보 교차부 기둥"
                });
        return columns;
    }

    public static void Validate(ValveRoomGeometryRequestDto dto)
    {
        var d = dto.DesignConditionDto; var pl = dto.PlanSpecDto; var pr = dto.ProfileSpecDto;
        // RoomHeight(dto)가 MudSpec에 의존하므로 스펙 존재 검사를 치수 검사보다 먼저 수행한다.
        if (d.RoomType == "이토밸브실" && dto.MudSpec is null)
            throw new ArgumentException("이토밸브실은 중간벽·중간슬래브 입력이 필요합니다.");
        if (pl.InnerWidth <= 0 || pl.InnerLength <= 0 || RoomHeight(dto) <= 0 || pr.FoundationThickness <= 0 || pr.OuterWallThickness <= 0)
            throw new ArgumentException("밸브실 치수와 두께는 0보다 커야 합니다.");
        if (d.RoomType == "제수밸브실" && (dto.SluiceSpec is null || string.IsNullOrWhiteSpace(dto.SluiceSpec.BeamTypeName) || string.IsNullOrWhiteSpace(dto.SluiceSpec.ColumnTypeName)))
            throw new ArgumentException("제수밸브실은 보와 기둥 유형을 선택해야 합니다.");
    }

    private static SlabDefinition Slab(ValveRoomGeometryRequestDto dto, double centerX, double centerY, double width, double length, double thickness, double z, string part) => new()
    {
        Points = Rectangle(centerX, centerY, width, length),
        SubPoints = Array.Empty<Point2D>(),
        Thickness = thickness,
        ElevationZ = z,
        LevelName = BaseLevelName,
        Category = "슬래브",
        ElementCode = "VR-S",
        Zone = dto.DesignConditionDto.RoomType,
        Part = part
    };

    private static LinearWallDefinition Wall(ValveRoomGeometryRequestDto dto, Point3D start, Point3D end, double thickness, double height, string part, bool isExterior, double? baseOffset = null) => new()
    {
        StartPoint = start,
        EndPoint = end,
        Thickness = thickness,
        Height = height,
        BaseOffset = baseOffset ?? 0,
        LevelName = BaseLevelName,
        Category = "벽",
        ElementCode = "VR-W",
        Zone = dto.DesignConditionDto.RoomType,
        Part = part,
        IsExterior = isExterior
    };

    private static BeamDefinition Beam(ValveRoomGeometryRequestDto dto, Point3D start, Point3D end, string part) => new()
    {
        StartPoint = start,
        EndPoint = end,
        TypeName = dto.SluiceSpec!.BeamTypeName,
        LevelName = BaseLevelName,
        Category = "보",
        ElementCode = "VR-B",
        Zone = dto.DesignConditionDto.RoomType,
        Part = part
    };

    private static IReadOnlyList<Point2D> Rectangle(double x, double y, double width, double length) =>
        new[] { new Point2D(x - width / 2, y - length / 2), new Point2D(x + width / 2, y - length / 2), new Point2D(x + width / 2, y + length / 2), new Point2D(x - width / 2, y + length / 2) };

    private static double RoomHeight(ValveRoomGeometryRequestDto dto) =>
        dto.MudSpec is { HasIntermediateSlab: true } mud
            ? mud.Floor1InnerHeight + mud.IntermediateSlabThickness + mud.Floor2InnerHeight
            : dto.ProfileSpecDto.InnerHeight;
}
