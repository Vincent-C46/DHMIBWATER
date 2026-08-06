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
    private readonly IDialogService _dialog;

    public CreateValveRoomUseCase(ITransactionContext transaction, ILevelQueryRepo levelQuery, ILevelCommandRepo levelCommand,
        IProjectLocationCommandRepo projectLocationCommand,
        ISlabCommandRepo slabCommand, IFoundationCommandRepo foundationCommand, IAirValveVoidCommandRepo airValveVoidCommand, IWallCommandRepo wallCommand, IBeamCommandRepo beamCommand,
        IColumnCommandRepo columnCommand, IDialogService dialog)
    {
        _transaction = transaction; _levelQuery = levelQuery; _levelCommand = levelCommand; _projectLocationCommand = projectLocationCommand; _slabCommand = slabCommand; _foundationCommand = foundationCommand; _airValveVoidCommand = airValveVoidCommand;
        _wallCommand = wallCommand; _beamCommand = beamCommand; _columnCommand = columnCommand; _dialog = dialog;
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
                foreach (var slab in ValveRoomGeometryCalculator.CalculateSlabs(dto)) _slabCommand.CreateSlab(slab);
                foreach (var foundation in ValveRoomGeometryCalculator.CalculateFoundations(dto))
                {
                    var foundationId = _foundationCommand.CreateFoundationFromFirstInstance(foundation);
                    if (request.AirSpec is { } air)
                        _airValveVoidCommand.CreateAirValveFoundationVoid(foundationId, ValveRoomGeometryCalculator.CalculateAirValveVoid(dto, air));
                }
                foreach (var wall in ValveRoomGeometryCalculator.CalculateWalls(dto)) _wallCommand.CreateLinearWall(wall);
                foreach (var beam in ValveRoomGeometryCalculator.CalculateBeams(dto)) _beamCommand.CreateBeam(beam);
                foreach (var column in ValveRoomGeometryCalculator.CalculateColumns(dto)) _columnCommand.CreateColumn(column);
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
