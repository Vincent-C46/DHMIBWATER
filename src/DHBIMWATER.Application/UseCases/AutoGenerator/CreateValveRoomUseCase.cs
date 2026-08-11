using DHBIMWATER.Application.DTOs.Revit.ValveRoom;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Services;

namespace DHBIMWATER.Application.UseCases.AutoGenerator;

/// <summary>밸브실 입력값을 독립 구조 모델로 변환한다. Transaction은 이 UseCase에서만 관리한다.</summary>
public sealed class CreateValveRoomUseCase
{
    private readonly ITransactionContext _transaction;
    private readonly ILevelQueryRepo _levelQuery;
    private readonly ILevelCommandRepo _levelCommand;
    private readonly ISlabCommandRepo _slabCommand;
    private readonly IProjectLocationCommandRepo _projectLocationCommand;
    private readonly IFoundationCommandRepo _foundationCommand;
    private readonly IAirValveVoidCommandRepo _airValveVoidCommand;
    private readonly IWallCommandRepo _wallCommand;
    private readonly IBeamCommandRepo _beamCommand;
    private readonly IColumnCommandRepo _columnCommand;
    private readonly IElementTypeCommandRepo _elementTypeCommand;
    private readonly IDialogService _dialog;

    public CreateValveRoomUseCase(ITransactionContext transaction, ILevelQueryRepo levelQuery, ILevelCommandRepo levelCommand,
        IProjectLocationCommandRepo projectLocationCommand,
        ISlabCommandRepo slabCommand, IFoundationCommandRepo foundationCommand, IAirValveVoidCommandRepo airValveVoidCommand, IWallCommandRepo wallCommand, IBeamCommandRepo beamCommand,
        IColumnCommandRepo columnCommand, IElementTypeCommandRepo elementTypeCommand, IDialogService dialog)
    {
        _transaction = transaction; _levelQuery = levelQuery; _levelCommand = levelCommand; _projectLocationCommand = projectLocationCommand; _slabCommand = slabCommand; _foundationCommand = foundationCommand; _airValveVoidCommand = airValveVoidCommand;
        _wallCommand = wallCommand; _beamCommand = beamCommand; _columnCommand = columnCommand; _elementTypeCommand = elementTypeCommand; _dialog = dialog;
    }

    public void Execute(ValveRoomRequestDto request)
    {
        var dto = ToGeometryDto(request);
        ValveRoomGeometryCalculator.Validate(dto);
        using (_transaction)
        {
            try
            {
                _transaction.Begin("Create Valve Room");

                EnsureLevels(dto);
                // 내부원점·프로젝트 기준점은 X, Y만 가진다. 표고는 항상 0이며 구조물 높이는 레벨이 결정한다.
                _projectLocationCommand.SetInternalOriginSharedPosition(
                    request.SharedCoordinateX,
                    request.SharedCoordinateY,
                    request.TrueNorthToProjectNorthClockwiseDegrees);
                var slabs = ValveRoomGeometryCalculator.CalculateSlabs(dto).ToList();
                var slabLevels = _levelQuery.GetLevelIds(slabs.Select(s => s.LevelName));
                var slabConcrete = new DHBIMWATER.Core.Structures.ConcreteSpec(25, 30, 150);
                var slabMaterial = _elementTypeCommand.FindOrCreateConcreteMaterial(slabConcrete);
                var slabTypes = slabs.Select(s => s.Thickness).Distinct().ToDictionary(t => t, t => _elementTypeCommand.FindOrCreateSlabType(new DHBIMWATER.Core.Structures.FloorTypeSpec(t, $"일반 - {t}mm", slabConcrete), slabMaterial));
                foreach (var slab in slabs) _slabCommand.CreateSlab(slab, slabLevels[slab.LevelName], slabTypes[slab.Thickness]);
                foreach (var foundation in ValveRoomGeometryCalculator.CalculateFoundations(dto))
                {
                    var foundationId = _foundationCommand.CreateFoundationFromFirstInstance(foundation);
                    if (request.AirSpec is { } air)
                        _airValveVoidCommand.CreateAirValveFoundationVoid(foundationId, ValveRoomGeometryCalculator.CalculateAirValveVoid(dto, air));
                }
                var walls = ValveRoomGeometryCalculator.CalculateWalls(dto).ToList();
                var wallLevelIds = _levelQuery.GetLevelIds(walls.Select(w => w.LevelName));
                var concrete = new DHBIMWATER.Core.Structures.ConcreteSpec(25, 27, 120);
                var materialId = _elementTypeCommand.FindOrCreateConcreteMaterial(concrete);
                var wallTypeIds = walls.Select(w => w.Thickness).Distinct().ToDictionary(thickness => thickness,
                    thickness => _elementTypeCommand.FindOrCreateWallType(new DHBIMWATER.Core.Structures.WallTypeSpec(thickness, $"일반 - {thickness}mm", concrete), materialId));
                foreach (var wall in walls) _wallCommand.CreateLinearWall(wall, wallLevelIds[wall.LevelName], wallTypeIds[wall.Thickness]);
                var beams = ValveRoomGeometryCalculator.CalculateBeams(dto).ToList();
                var beamLevels = _levelQuery.GetLevelIds(beams.Select(b => b.LevelName));
                var beamTypes = beams.GroupBy(b => b.Part == "HAUNCH" ? "HAUNCH" : b.TypeName ?? $"{b.Width} x {b.Height}").ToDictionary(g => g.Key, g => g.Key == "HAUNCH" ? _elementTypeCommand.FindHaunchBeamSymbol() : !string.IsNullOrEmpty(g.First().TypeName) ? _elementTypeCommand.FindBeamSymbol(g.First().TypeName) : _elementTypeCommand.FindOrCreateBeamType(new DHBIMWATER.Core.Structures.BeamTypeSpec(g.First().Width, g.First().Height, g.Key)));
                foreach (var beam in beams) _beamCommand.CreateBeam(beam, beamLevels[beam.LevelName], beamTypes[beam.Part == "HAUNCH" ? "HAUNCH" : beam.TypeName ?? $"{beam.Width} x {beam.Height}"]);
                var columns = ValveRoomGeometryCalculator.CalculateColumns(dto).ToList();
                var columnLevels = _levelQuery.GetLevelIds(columns.SelectMany(c => new[] { c.BaseLevelName, c.TopLevelName }));
                var columnTypes = columns.Select(c => c.TypeName).Distinct().ToDictionary(name => name, _elementTypeCommand.FindColumnSymbol);
                foreach (var column in columns) _columnCommand.CreateColumn(column, columnLevels[column.BaseLevelName], columnLevels[column.TopLevelName], columnTypes[column.TypeName]);
                _transaction.Commit();
                _dialog.Info("모델 생성", $"{request.RoomType} 모델 생성을 완료했습니다.");
            }
            catch { _transaction.Rollback(); throw; }
        }
    }

    /// <summary>기초 상부 EL로 산출한 레벨을 생성하되, 이미 있으면 표고만 갱신한다.</summary>
    private void EnsureLevels(ValveRoomGeometryRequestDto dto)
    {
        var levels = _levelQuery.GetExistingLevelNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var level in ValveRoomGeometryCalculator.CalculateLevels(dto))
            if (levels.Contains(level.Name)) _levelCommand.UpdateLevel(level.Name, level.Elevation);
            else _levelCommand.CreateLevel(level.Name, level.Elevation);
    }

    private static ValveRoomGeometryRequestDto ToGeometryDto(ValveRoomRequestDto r) => new(
        new ValveRoomDesignConditionDto(r.RoomType, r.FoundationTopEl),
        new ValveRoomPlanSpecDto(r.InnerWidth, r.InnerLength),
        new ValveRoomProfileSpecDto(r.InnerHeight, r.PlainConcreteThickness, r.FoundationThickness, r.FoundationToe, r.OuterWallThickness, r.UpperSlabThickness),
        r.MudSpec,
        r.SluiceSpec);
}
