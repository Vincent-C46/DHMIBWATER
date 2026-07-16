using DHBIMWATER.Application.DTOs.Revit.ValveRoom;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Application.UseCases.AutoGenerator;

/// <summary>밸브실 입력값을 독립 구조 모델로 변환한다. Transaction은 이 UseCase에서만 관리한다.</summary>
public sealed class CreateValveRoomUseCase
{
    private const string BaseLevelName = "밸브실 기준 레벨";
    private const string TopLevelName = "밸브실 상부 레벨";
    private const double TopLevelElevation = 10000;
    private readonly ITransactionContext _transaction;
    private readonly ILevelQueryRepo _levelQuery;
    private readonly ILevelCommandRepo _levelCommand;
    private readonly ISlabCommandRepo _slabCommand;
    private readonly IProjectLocationCommandRepo _projectLocationCommand;
    private readonly IWallCommandRepo _wallCommand;
    private readonly IBeamCommandRepo _beamCommand;
    private readonly IColumnCommandRepo _columnCommand;
    private readonly IDialogService _dialog;

    public CreateValveRoomUseCase(ITransactionContext transaction, ILevelQueryRepo levelQuery, ILevelCommandRepo levelCommand,
        IProjectLocationCommandRepo projectLocationCommand,
        ISlabCommandRepo slabCommand, IWallCommandRepo wallCommand, IBeamCommandRepo beamCommand,
        IColumnCommandRepo columnCommand, IDialogService dialog)
    {
        _transaction = transaction; _levelQuery = levelQuery; _levelCommand = levelCommand; _projectLocationCommand = projectLocationCommand; _slabCommand = slabCommand;
        _wallCommand = wallCommand; _beamCommand = beamCommand; _columnCommand = columnCommand; _dialog = dialog;
    }

    public void Execute(ValveRoomRequestDto request)
    {
        Validate(request);
        using (_transaction)
        {
            try
            {
                _transaction.Begin("Create Valve Room");
                EnsureLevels();
                _projectLocationCommand.SetInternalOriginSharedPosition(
                    request.SharedCoordinateX,
                    request.SharedCoordinateY,
                    request.SharedElevation,
                    request.TrueNorthToProjectNorthClockwiseDegrees);
                foreach (var slab in CalculateSlabs(request)) _slabCommand.CreateSlab(slab);
                foreach (var wall in CalculateWalls(request)) _wallCommand.CreateLinearWall(wall);
                foreach (var beam in CalculateBeams(request)) _beamCommand.CreateBeam(beam);
                foreach (var column in CalculateColumns(request)) _columnCommand.CreateColumn(column);
                _transaction.Commit();
                _dialog.Info("모델 생성", $"{request.RoomType} 모델 생성을 완료했습니다.");
            }
            catch { _transaction.Rollback(); throw; }
        }
    }

    private void EnsureLevels()
    {
        var levels = _levelQuery.GetExistingLevelNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!levels.Contains(BaseLevelName)) _levelCommand.CreateLevel(BaseLevelName, 0);
        if (!levels.Contains(TopLevelName)) _levelCommand.CreateLevel(TopLevelName, TopLevelElevation);
    }

    private static IEnumerable<SlabDefinition> CalculateSlabs(ValveRoomRequestDto r)
    {
        var outerWidth = r.InnerWidth + r.OuterWallThickness * 2;
        var outerLength = r.InnerLength + r.OuterWallThickness * 2;
        var foundationWidth = outerWidth + r.FoundationToe * 2;
        var foundationLength = outerLength + r.FoundationToe * 2;
        yield return Slab(r, r.InnerWidth / 2, r.InnerLength / 2, foundationWidth, foundationLength, r.PlainConcreteThickness, r.ReferenceZ - r.FoundationThickness, "버림콘크리트");
        yield return Slab(r, r.InnerWidth / 2, r.InnerLength / 2, foundationWidth, foundationLength, r.FoundationThickness, r.ReferenceZ, "기초");
        if (r.RoomType == "이토밸브실" && r.HasIntermediateSlab)
            yield return Slab(r, r.InnerWidth / 2, r.InnerLength / 2, outerWidth, outerLength, r.IntermediateSlabThickness, r.ReferenceZ + r.Floor1InnerHeight, "중간슬래브");
        yield return Slab(r, r.InnerWidth / 2, r.InnerLength / 2, outerWidth, outerLength, r.UpperSlabThickness, r.ReferenceZ + RoomHeight(r), "상부슬래브");
    }

    private static IEnumerable<LinearWallDefinition> CalculateWalls(ValveRoomRequestDto r)
    {
        var x0 = -r.OuterWallThickness / 2; var x1 = r.InnerWidth + r.OuterWallThickness / 2;
        var y0 = -r.OuterWallThickness / 2; var y1 = r.InnerLength + r.OuterWallThickness / 2;
        foreach (var (start, end) in new[]
        {
            (new Point3D(x0, y0, 0), new Point3D(x1, y0, 0)), (new Point3D(x1, y0, 0), new Point3D(x1, y1, 0)),
            (new Point3D(x1, y1, 0), new Point3D(x0, y1, 0)), (new Point3D(x0, y1, 0), new Point3D(x0, y0, 0))
        }) yield return Wall(r, start, end, r.OuterWallThickness, RoomHeight(r), "외벽", isExterior: true);

        if (r.RoomType != "이토밸브실" || !r.HasIntermediateWall) yield break;
        for (var i = 1; i <= r.IntermediateWallCount; i++)
        {
            var x = r.InnerWidth * i / (r.IntermediateWallCount + 1d);
            yield return Wall(r, new Point3D(x, 0, 0), new Point3D(x, r.InnerLength, 0), r.IntermediateWallThickness, RoomHeight(r), "중간벽", isExterior: false);
        }
    }

    private static IEnumerable<BeamDefinition> CalculateBeams(ValveRoomRequestDto r)
    {
        if (r.RoomType != "제수밸브실") yield break;
        var z = r.ReferenceZ + RoomHeight(r); var x0 = 0d; var x1 = r.InnerWidth;
        var y0 = 0d; var y1 = r.InnerLength;
        for (var i = 0; i < r.BeamCountX; i++)
            yield return Beam(r, new Point3D(x0, y0 + r.BeamOffsetX + r.BeamSpacingX * i, z), new Point3D(x1, y0 + r.BeamOffsetX + r.BeamSpacingX * i, z), "X방향 보");
        for (var i = 0; i < r.BeamCountY; i++)
            yield return Beam(r, new Point3D(x0 + r.BeamOffsetY + r.BeamSpacingY * i, y0, z), new Point3D(x0 + r.BeamOffsetY + r.BeamSpacingY * i, y1, z), "Y방향 보");
    }

    private static IEnumerable<ColumnDefinition> CalculateColumns(ValveRoomRequestDto r)
    {
        if (r.RoomType != "제수밸브실") yield break;
        var x0 = 0d; var y0 = 0d;
        for (var x = 0; x < r.BeamCountY; x++)
        for (var y = 0; y < r.BeamCountX; y++)
            yield return new ColumnDefinition
            {
                Position = new Point3D(x0 + r.BeamOffsetY + r.BeamSpacingY * x, y0 + r.BeamOffsetX + r.BeamSpacingX * y, r.ReferenceZ),
                TypeName = r.ColumnTypeName, BaseLevelName = BaseLevelName, TopLevelName = TopLevelName,
                TopOffset = r.ReferenceZ + RoomHeight(r) - TopLevelElevation, ElementCode = "VR-C", Zone = r.RoomType, Part = "보 교차부 기둥"
            };
    }

    private static SlabDefinition Slab(ValveRoomRequestDto r, double centerX, double centerY, double width, double length, double thickness, double z, string part) => new()
    {
        Points = Rectangle(centerX, centerY, width, length), SubPoints = Array.Empty<Point2D>(), Thickness = thickness, ElevationZ = z,
        LevelName = BaseLevelName, Category = "슬래브", ElementCode = "VR-S", Zone = r.RoomType, Part = part
    };
    private static LinearWallDefinition Wall(ValveRoomRequestDto r, Point3D start, Point3D end, double thickness, double height, string part, bool isExterior) => new()
    {
        StartPoint = start, EndPoint = end, Thickness = thickness, Height = height, BaseOffset = r.ReferenceZ,
        LevelName = BaseLevelName, Category = "벽", ElementCode = "VR-W", Zone = r.RoomType, Part = part, IsExterior = isExterior
    };
    private static BeamDefinition Beam(ValveRoomRequestDto r, Point3D start, Point3D end, string part) => new()
    {
        StartPoint = start, EndPoint = end, TypeName = r.BeamTypeName, LevelName = BaseLevelName,
        Category = "보", ElementCode = "VR-B", Zone = r.RoomType, Part = part
    };
    private static IReadOnlyList<Point2D> Rectangle(double x, double y, double width, double length) =>
        new[] { new Point2D(x - width / 2, y - length / 2), new Point2D(x + width / 2, y - length / 2), new Point2D(x + width / 2, y + length / 2), new Point2D(x - width / 2, y + length / 2) };
    private static double RoomHeight(ValveRoomRequestDto r) => r.RoomType == "이토밸브실" && r.HasIntermediateSlab
        ? r.Floor1InnerHeight + r.IntermediateSlabThickness + r.Floor2InnerHeight : r.InnerHeight;
    private static void Validate(ValveRoomRequestDto r)
    {
        if (r.InnerWidth <= 0 || r.InnerLength <= 0 || RoomHeight(r) <= 0 || r.FoundationThickness <= 0 || r.OuterWallThickness <= 0)
            throw new ArgumentException("밸브실 치수와 두께는 0보다 커야 합니다.");
        if (r.RoomType == "제수밸브실" && (string.IsNullOrWhiteSpace(r.BeamTypeName) || string.IsNullOrWhiteSpace(r.ColumnTypeName)))
            throw new ArgumentException("제수밸브실은 보와 기둥 유형을 선택해야 합니다.");
    }
}