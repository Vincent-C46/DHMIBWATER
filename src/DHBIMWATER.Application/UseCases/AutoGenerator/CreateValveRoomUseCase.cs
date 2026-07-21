using DHBIMWATER.Application.DTOs.Revit.ValveRoom;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Services;

namespace DHBIMWATER.Application.UseCases.AutoGenerator;

/// <summary>밸브실 입력값을 독립 구조 모델로 변환한다. Transaction은 이 UseCase에서만 관리한다.</summary>
public sealed class CreateValveRoomUseCase
{
    internal const string BaseLevelName = "밸브실 기준 레벨";
    internal const string TopLevelName = "밸브실 상부 레벨";
    internal const double TopLevelElevation = 10000;
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
        var dto = ToGeometryDto(request);
        ValveRoomGeometryCalculator.Validate(dto);
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
                foreach (var slab in ValveRoomGeometryCalculator.CalculateSlabs(dto)) _slabCommand.CreateSlab(slab);
                foreach (var wall in ValveRoomGeometryCalculator.CalculateWalls(dto)) _wallCommand.CreateLinearWall(wall);
                foreach (var beam in ValveRoomGeometryCalculator.CalculateBeams(dto)) _beamCommand.CreateBeam(beam);
                foreach (var column in ValveRoomGeometryCalculator.CalculateColumns(dto)) _columnCommand.CreateColumn(column);
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

    private static ValveRoomGeometryRequestDto ToGeometryDto(ValveRoomRequestDto r) => new(
        new ValveRoomDesignConditionDto(r.RoomType, r.ReferenceZ),
        new ValveRoomPlanSpecDto(r.InnerWidth, r.InnerLength),
        new ValveRoomProfileSpecDto(r.InnerHeight, r.PlainConcreteThickness, r.FoundationThickness, r.FoundationToe, r.OuterWallThickness, r.UpperSlabThickness),
        r.RoomType == "이토밸브실"
            ? new MudValveRoomSpecDto(r.HasIntermediateWall, r.IntermediateWallCount, r.IntermediateWallThickness, r.HasIntermediateSlab, r.IntermediateSlabThickness, r.Floor1InnerHeight, r.Floor2InnerHeight)
            : null,
        r.RoomType == "제수밸브실"
            ? new SluiceValveRoomSpecDto(r.BeamCountX, r.BeamOffsetX, r.BeamSpacingX, r.BeamCountY, r.BeamOffsetY, r.BeamSpacingY, r.BeamTypeName, r.ColumnTypeName)
            : null);
}
