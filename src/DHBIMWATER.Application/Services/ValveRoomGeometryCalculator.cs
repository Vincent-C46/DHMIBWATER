using DHBIMWATER.Application.DTOs.Revit.ValveRoom;
using DHBIMWATER.Application.UseCases.AutoGenerator;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Application.Services;

public class ValveRoomGeometryCalculator
{
    private const string FoundationPumpLevelName = "기초";
    private const string UpperSlabLevelName = "상부슬래브";


    public static IReadOnlyList<LevelDefinition> CalculateLevels(ValveRoomGeometryRequestDto dto)
    {
        var d = dto.DesignConditionDto;
        var pl = dto.PlanSpecDto;
        var pr = dto.ProfileSpecDto;

        var outerWidth = pl.InnerWidth + pr.OuterWallThickness * 2;
        var outerLength = pl.InnerLength + pr.OuterWallThickness * 2;
        var foundationWidth = outerWidth + pr.FoundationToe * 2;
        var foundationLength = outerLength + pr.FoundationToe * 2;

        var levels = new List<LevelDefinition>
        {
            new LevelDefinition { Name = FoundationPumpLevelName,  Elevation = 1000 },
            new LevelDefinition { Name = UpperSlabLevelName,  Elevation = 1000 },
        };
     
        return levels;
    }

    public static IReadOnlyList<SlabDefinition> CalculateSlabs(ValveRoomGeometryRequestDto dto)
    {
        var d = dto.DesignConditionDto; 
        var pl = dto.PlanSpecDto; 
        var pr = dto.ProfileSpecDto;
        var outerWidth = pl.InnerWidth + pr.OuterWallThickness * 2;
        var outerLength = pl.InnerLength + pr.OuterWallThickness * 2;
        var foundationWidth = outerWidth + pr.FoundationToe * 2;
        var foundationLength = outerLength + pr.FoundationToe * 2;
        var slabs = new List<SlabDefinition>
        {
            Slab(dto, pl.InnerWidth / 2, pl.InnerLength / 2, foundationWidth, foundationLength, pr.PlainConcreteThickness, d.ReferenceZ - pr.FoundationThickness, "버림콘크리트"),
            Slab(dto, pl.InnerWidth / 2, pl.InnerLength / 2, foundationWidth, foundationLength, pr.FoundationThickness, d.ReferenceZ, "기초")
        };
        if (d.RoomType == "이토밸브실" && dto.MudSpec is { HasIntermediateSlab: true } mud)
            slabs.Add(Slab(dto, pl.InnerWidth / 2, pl.InnerLength / 2, outerWidth, outerLength, mud.IntermediateSlabThickness, d.ReferenceZ + mud.Floor1InnerHeight, "중간슬래브"));
        slabs.Add(Slab(dto, pl.InnerWidth / 2, pl.InnerLength / 2, outerWidth, outerLength, pr.UpperSlabThickness, d.ReferenceZ + RoomHeight(dto), "상부슬래브"));
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
                walls.Add(Wall(dto, start, end, pr.OuterWallThickness, slabMud.Floor2InnerHeight, "외벽", isExterior: true,
                    baseOffset: d.ReferenceZ + slabMud.Floor1InnerHeight + slabMud.IntermediateSlabThickness));
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
        var z = d.ReferenceZ + RoomHeight(dto); var x0 = 0d; var x1 = pl.InnerWidth;
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
                Position = new Point3D(sluice.BeamOffsetY + sluice.BeamSpacingY * x, sluice.BeamOffsetX + sluice.BeamSpacingX * y, d.ReferenceZ),
                TypeName = sluice.ColumnTypeName, BaseLevelName = CreateValveRoomUseCase.BaseLevelName, TopLevelName = CreateValveRoomUseCase.TopLevelName,
                TopOffset = d.ReferenceZ + RoomHeight(dto) - CreateValveRoomUseCase.TopLevelElevation, ElementCode = "VR-C", Zone = d.RoomType, Part = "보 교차부 기둥"
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
        Points = Rectangle(centerX, centerY, width, length), SubPoints = Array.Empty<Point2D>(), Thickness = thickness, ElevationZ = z,
        LevelName = CreateValveRoomUseCase.BaseLevelName, Category = "슬래브", ElementCode = "VR-S", Zone = dto.DesignConditionDto.RoomType, Part = part
    };

    private static LinearWallDefinition Wall(ValveRoomGeometryRequestDto dto, Point3D start, Point3D end, double thickness, double height, string part, bool isExterior, double? baseOffset = null) => new()
    {
        StartPoint = start, EndPoint = end, Thickness = thickness, Height = height, BaseOffset = baseOffset ?? dto.DesignConditionDto.ReferenceZ,
        LevelName = CreateValveRoomUseCase.BaseLevelName, Category = "벽", ElementCode = "VR-W", Zone = dto.DesignConditionDto.RoomType, Part = part, IsExterior = isExterior
    };

    private static BeamDefinition Beam(ValveRoomGeometryRequestDto dto, Point3D start, Point3D end, string part) => new()
    {
        StartPoint = start, EndPoint = end, TypeName = dto.SluiceSpec!.BeamTypeName, LevelName = CreateValveRoomUseCase.BaseLevelName,
        Category = "보", ElementCode = "VR-B", Zone = dto.DesignConditionDto.RoomType, Part = part
    };

    private static IReadOnlyList<Point2D> Rectangle(double x, double y, double width, double length) =>
        new[] { new Point2D(x - width / 2, y - length / 2), new Point2D(x + width / 2, y - length / 2), new Point2D(x + width / 2, y + length / 2), new Point2D(x - width / 2, y + length / 2) };

    private static double RoomHeight(ValveRoomGeometryRequestDto dto) =>
        dto.MudSpec is { HasIntermediateSlab: true } mud
            ? mud.Floor1InnerHeight + mud.IntermediateSlabThickness + mud.Floor2InnerHeight
            : dto.ProfileSpecDto.InnerHeight;
}
