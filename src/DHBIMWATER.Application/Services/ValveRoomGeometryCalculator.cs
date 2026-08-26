using DHBIMWATER.Application.DTOs.Revit.ValveRoom;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Application.Services;

public class ValveRoomGeometryCalculator
{
    public const string BaseLevelName = "밸브실 기초 상부";
    public const string IntermediateLevelName = "밸브실 중간슬래브";
    public const string TopLevelName = "밸브실 상부슬래브";

    /// <summary>기초 상부 EL(m)을 프로젝트 내부 단위(mm)로 환산한 기준 레벨 표고.</summary>
    public static double BaseElevation(ValveRoomGeometryRequestDto dto) => dto.DesignConditionDto.FoundationTopEl * 1000;

    public static double TopElevation(ValveRoomGeometryRequestDto dto) => BaseElevation(dto) + RoomHeight(dto);

    public static IReadOnlyList<LevelDefinition> CalculateLevels(ValveRoomGeometryRequestDto dto)
    {
        var levels = new List<LevelDefinition>
        {
            new LevelDefinition { Name = BaseLevelName, Elevation = BaseElevation(dto) },
        };
        // 이토밸브실 - 중간슬래브가 있으면 중간슬래브 상단(2F 바닥) 참고 레벨을 생성한다.
        if (dto.DesignConditionDto.RoomType == "이토밸브실" && dto.MudSpec is { HasIntermediateSlab: true } mud)
            levels.Add(new LevelDefinition
            {
                Name = IntermediateLevelName,
                Elevation = BaseElevation(dto) + mud.Floor1InnerHeight + mud.IntermediateSlabThickness
            });
        levels.Add(new LevelDefinition { Name = TopLevelName, Elevation = TopElevation(dto) });
        return levels;
    }

    public static IReadOnlyList<SlabDefinition> CalculateSlabs(ValveRoomGeometryRequestDto dto)
    {
        var d = dto.DesignConditionDto;
        var pl = dto.PlanSpecDto;
        var pr = dto.ProfileSpecDto;
        // X축=길이(InnerLength), Y축=폭(InnerWidth). Slab(centerX, centerY, xExtent, yExtent, ...) 규약에 맞춰 X에 길이, Y에 폭을 넣는다.
        var outerX = pl.InnerLength + pr.OuterWallThickness * 2;   // X(길이) 방향 외벽 외곽
        var outerY = pl.InnerWidth + pr.OuterWallThickness * 2;    // Y(폭) 방향 외벽 외곽
        var foundationX = outerX + pr.FoundationToe * 2;
        var foundationY = outerY + pr.FoundationToe * 2;
        // 버림콘크리트는 기초보다 사방으로 버림 두께만큼 바깥으로 확장한다.
        var plainX = foundationX + pr.PlainConcreteThickness * 2;
        var plainY = foundationY + pr.PlainConcreteThickness * 2;
        var centerX = pl.InnerLength / 2;
        var centerY = pl.InnerWidth / 2;
        var baseZ = BaseElevation(dto);
        var slabs = new List<SlabDefinition>();
        if (d.RoomType != "공기밸브실")
            slabs.Add(Slab(dto, centerX, centerY, plainX, plainY, pr.PlainConcreteThickness, baseZ - pr.FoundationThickness, "버림콘크리트"));
        if (d.RoomType != "공기밸브실")
            slabs.Add(Slab(dto, centerX, centerY, foundationX, foundationY, pr.FoundationThickness, baseZ, "기초"));
        if (d.RoomType == "이토밸브실" && dto.MudSpec is { HasIntermediateSlab: true } mud)
            slabs.Add(Slab(dto, centerX, centerY, outerX, outerY, mud.IntermediateSlabThickness, baseZ + mud.Floor1InnerHeight + mud.IntermediateSlabThickness, "중간슬래브"));
        slabs.Add(Slab(dto, centerX, centerY, outerX, outerY, pr.UpperSlabThickness, TopElevation(dto), "상부슬래브"));
        return slabs;
    }

    public static IReadOnlyList<FoundationDefinition> CalculateFoundations(ValveRoomGeometryRequestDto dto)
    {
        if (dto.DesignConditionDto.RoomType != "공기밸브실") return Array.Empty<FoundationDefinition>();

        var plan = dto.PlanSpecDto;
        return new[]
        {
            new FoundationDefinition
            {
                Position = new Point3D(plan.InnerLength / 2, plan.InnerWidth / 2, BaseElevation(dto)),
                Length = plan.InnerLength + dto.ProfileSpecDto.OuterWallThickness * 2 + dto.ProfileSpecDto.FoundationToe * 2,
                Width = plan.InnerWidth + dto.ProfileSpecDto.OuterWallThickness * 2 + dto.ProfileSpecDto.FoundationToe * 2,
                Thickness = dto.ProfileSpecDto.FoundationThickness,
                ElementCode = "VR-F",
                Zone = dto.DesignConditionDto.RoomType,
                Part = "기초"
            }
        };
    }

    public static AirValveVoidPlacementDefinition CalculateAirValveVoid(ValveRoomGeometryRequestDto dto, AirValveRoomSpecDto air)
    {
        var plan = dto.PlanSpecDto;
        var isYAxis = air.VoidAxis.Equals("Y", StringComparison.OrdinalIgnoreCase);
        return new AirValveVoidPlacementDefinition(
            new Point3D(isYAxis ? plan.InnerLength / 2 : 0, isYAxis ? 0 : plan.InnerWidth / 2,
                BaseElevation(dto) - air.FoundationTopToPipeCenterDepth),
            air.MainPipeDiameter / 2,
            isYAxis ? "Y" : "X");
    }

    public static IReadOnlyList<LinearWallDefinition> CalculateWalls(ValveRoomGeometryRequestDto dto)
    {
        var d = dto.DesignConditionDto; var pl = dto.PlanSpecDto; var pr = dto.ProfileSpecDto;
        var t = pr.OuterWallThickness;
        var x0 = -t / 2; var x1 = pl.InnerLength + t / 2;   // 세로(좌우) 벽체 중심선 X (길이 방향)
        var y0 = -t / 2; var y1 = pl.InnerWidth + t / 2;    // 가로(상하) 벽체 중심선 Y (폭 방향)
        var hx0 = x0 - t / 2; var hx1 = x1 + t / 2;         // 가로 벽체 끝점 X (바깥으로)
        var vy0 = y0 + t / 2; var vy1 = y1 - t / 2;         // 세로 벽체 끝점 Y (안쪽으로)
        var walls = new List<LinearWallDefinition>();
        var outerCorners = new[]
        {
            (new Point3D(hx0, y0, 0), new Point3D(hx1, y0, 0)), (new Point3D(x1, vy0, 0), new Point3D(x1, vy1, 0)),
            (new Point3D(hx1, y1, 0), new Point3D(hx0, y1, 0)), (new Point3D(x0, vy1, 0), new Point3D(x0, vy0, 0))
        };
        if (d.RoomType == "이토밸브실" && dto.MudSpec is { HasIntermediateSlab: true } slabMud)
        {
            foreach (var (start, end) in outerCorners)
            {
                walls.Add(Wall(dto, start, end, pr.OuterWallThickness, slabMud.Floor1InnerHeight, "외벽", isExterior: true));
                walls.Add(Wall(dto, start, end, pr.OuterWallThickness, slabMud.Floor2InnerHeight, "외벽", isExterior: true,
                    baseOffset: slabMud.Floor1InnerHeight + slabMud.IntermediateSlabThickness));
            }
        }
        else
        {
            foreach (var (start, end) in outerCorners) walls.Add(Wall(dto, start, end, pr.OuterWallThickness, RoomHeight(dto) - pr.UpperSlabThickness, "외벽", isExterior: true));
        }

        if (d.RoomType != "이토밸브실" || dto.MudSpec is not { HasIntermediateWall: true } mud) return walls;
        // 중간벽 1개. 입력 offset은 좌측 외벽 내측(X=0)에서 중간벽 좌측면까지의 안목거리이므로 중심선으로 환산
        var mx = mud.IntermediateWallOffset + mud.IntermediateWallThickness / 2;
        var mStart = new Point3D(mx, 0, 0);
        var mEnd = new Point3D(mx, pl.InnerWidth, 0);
        if (dto.MudSpec is { HasIntermediateSlab: true } intermediateSlab)
        {
            walls.Add(Wall(dto, mStart, mEnd, mud.IntermediateWallThickness, intermediateSlab.Floor1InnerHeight, "중간벽", isExterior: false));
            walls.Add(Wall(dto, mStart, mEnd, mud.IntermediateWallThickness, intermediateSlab.Floor2InnerHeight, "중간벽", isExterior: false,
                baseOffset: intermediateSlab.Floor1InnerHeight + intermediateSlab.IntermediateSlabThickness));
        }
        else
        {
            walls.Add(Wall(dto, mStart, mEnd, mud.IntermediateWallThickness, RoomHeight(dto) - pr.UpperSlabThickness, "중간벽", isExterior: false));
        }
        return walls;
    }

    public static IReadOnlyList<BeamDefinition> CalculateBeams(ValveRoomGeometryRequestDto dto)
    {
        var d = dto.DesignConditionDto; var pl = dto.PlanSpecDto;
        if (d.RoomType != "제수밸브실" || dto.SluiceSpec is not { } sluice) return Array.Empty<BeamDefinition>();
        var z = TopElevation(dto); var x0 = 0d; var x1 = pl.InnerLength;   // X방향 보는 길이(X) 전 구간을 가로지른다.
        var y0 = 0d; var y1 = pl.InnerWidth;                              // Y방향 보는 폭(Y) 전 구간을 가로지른다.
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
                    // 상단은 상부슬래브 레벨에 구속하되, 슬래브 두께만큼 내려 슬래브 하부에 맞춘다.
                    TopOffset = -dto.ProfileSpecDto.UpperSlabThickness,
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

    // 기초 상단~상부슬래브 상단 높이. 입력 내부 높이는 상부슬래브 하단까지이며, 중간슬래브가 있으면 1F 안목 + 중간슬래브 두께 + 2F 안목 + 상부슬래브 두께.
    private static double RoomHeight(ValveRoomGeometryRequestDto dto) =>
        dto.MudSpec is { HasIntermediateSlab: true } mud
            ? mud.Floor1InnerHeight + mud.IntermediateSlabThickness + mud.Floor2InnerHeight + dto.ProfileSpecDto.UpperSlabThickness
            : dto.ProfileSpecDto.InnerHeight + dto.ProfileSpecDto.UpperSlabThickness;
}
