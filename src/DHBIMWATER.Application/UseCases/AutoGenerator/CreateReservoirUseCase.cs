using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Services;
using DHBIMWATER.Core.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DHBIMWATER.Application.UseCases.AutoGenerator
{
    public class CreateReservoirUseCase
    {
        #region Fields
        private readonly ITransactionContext _tx;
        private readonly IDialogService _dialogService;
        private readonly ILevelQueryRepo _levelQueryRepo;
        private readonly ILevelCommandRepo _levelCmdRepo;
        private readonly ISlabCommandRepo _slabCmdRepo;
        private readonly IWallCommandRepo _wallCmdRepo;
        private readonly IBeamCommandRepo _beamCmdRepo;
        private readonly IColumnCommandRepo _columnCmdRepo;
        private readonly ISharedParameterRepository _sharedParameterRepo;
        private readonly ClassifyExteriorWallsUseCase _classifyWallsUseCase;
        #endregion

        #region Constructor
        public CreateReservoirUseCase(
            ITransactionContext tx,
            IDialogService dialogService,
            ILevelQueryRepo levelQueryRepo,
            ILevelCommandRepo levelCmdRepo,
            ISlabCommandRepo slabCmdRepo,
            IWallCommandRepo wallCmdRepo,
            IBeamCommandRepo beamCmdRepo,
            IColumnCommandRepo columnCmdRepo,
            ISharedParameterRepository sharedParameterRepo,
            ClassifyExteriorWallsUseCase classifyWallsUseCase)
        {
            _tx = tx;
            _dialogService = dialogService;
            _levelQueryRepo = levelQueryRepo;
            _levelCmdRepo = levelCmdRepo;
            _slabCmdRepo = slabCmdRepo;
            _wallCmdRepo = wallCmdRepo;
            _beamCmdRepo = beamCmdRepo;
            _columnCmdRepo = columnCmdRepo;
            _sharedParameterRepo = sharedParameterRepo;
            _classifyWallsUseCase = classifyWallsUseCase;
        }
        #endregion

        #region Methods
        public void Execute(ReservoirCreationRequestDto dto)
        {
            using (_tx)
            {
                try
                {
                    _tx.Begin("Create Reservoir");

                    #region 0. 공유 매개변수 생성
                    _sharedParameterRepo.EnsureParameters(GetReservoirSharedParameterDefinitions());
                    #endregion

                    #region 1. 레벨 생성
                    var existingLevels = _levelQueryRepo.GetExistingLevelNames();
                    var existingPlanNames = _levelQueryRepo.GetExistingPlanNames();

                    foreach (var lvl in ReservoirGeometryCalculator.CalculateLevels(dto))
                    {
                        var existing = existingLevels.FirstOrDefault(s => s.Contains(lvl.Name));
                        long levelId;
                        if (existing != null)
                            levelId = _levelCmdRepo.UpdateLevel(existing, lvl.Elevation);
                        else
                            levelId = _levelCmdRepo.CreateLevel(lvl.Name, lvl.Elevation);

                        bool isWaterElevationLevel =
                            lvl.Name == ReservoirGeometryCalculator.LWLLevelName ||
                            lvl.Name == ReservoirGeometryCalculator.HWLLevelName;
                        if (!isWaterElevationLevel && !existingPlanNames.Contains(lvl.Name))
                            _levelCmdRepo.CreatePlan(levelId);
                    }
                    #endregion

                    #region 2. 슬래브 생성
                    foreach (var slabDef in ReservoirGeometryCalculator.CalculateSlabs(dto))
                        _slabCmdRepo.CreateSlab(slabDef);
                    #endregion

                    #region 3. 벽체 생성
                    foreach (var wallDef in ReservoirGeometryCalculator.CalculateLinearWalls(dto))
                        _wallCmdRepo.CreateLinearWall(wallDef);
                    _classifyWallsUseCase.Execute();
                    #endregion

                    #region 4. 기둥 생성
                    foreach (var colDef in ReservoirGeometryCalculator.CalculateColumns(dto))
                        _columnCmdRepo.CreateColumn(colDef);
                    #endregion

                    #region 5. 보 생성
                    foreach (var beamDef in ReservoirGeometryCalculator.CalculateBeams(dto))
                        _beamCmdRepo.CreateBeam(beamDef);
                    #endregion

                    // TODO: 오프닝 배치 (파이프 관통 슬리브 등) — ReservoirGeometryCalculator.CalculateOpenings() 추가 후 구현
                    // TODO: 단면뷰 작성 — ReservoirGeometryCalculator.CalculateSectionViews() 추가 후 구현

                    _tx.Commit();

                    _dialogService.Info("Success", "배수지 작성 완료");
                }
                catch (Exception ex)
                {
                    _dialogService.Warn("Rollback", ex.Message);
                    _tx.Rollback();
                    throw;
                }
            }
        }
        #endregion

        #region Private Helpers
        private static List<SharedParameterDefinition> GetReservoirSharedParameterDefinitions()
        {
            var structuralCategories = new List<ParameterCategory>
            {
                ParameterCategory.StructuralFraming,
                ParameterCategory.StructuralColumns,
                ParameterCategory.GenericModel,
                ParameterCategory.Floors,
                ParameterCategory.Walls,
                ParameterCategory.Stairs,
            };

            return new List<SharedParameterDefinition>
            {
                new SharedParameterDefinition { Name = "DH_ElementCode", SpecType = ParameterSpecType.Text,  GroupType = ParameterGroupType.Data,         BindingType = ParameterBindingType.Instance, Categories = structuralCategories },
                new SharedParameterDefinition { Name = "DH_Addin",       SpecType = ParameterSpecType.Text,  GroupType = ParameterGroupType.Data,         BindingType = ParameterBindingType.Instance, Categories = structuralCategories, UserModifiable = false },
                new SharedParameterDefinition { Name = "DH_Class",       SpecType = ParameterSpecType.Text,  GroupType = ParameterGroupType.Data,         BindingType = ParameterBindingType.Instance, Categories = structuralCategories },
                new SharedParameterDefinition { Name = "DH_Category",    SpecType = ParameterSpecType.Text,  GroupType = ParameterGroupType.Data,         BindingType = ParameterBindingType.Instance, Categories = structuralCategories },
                new SharedParameterDefinition { Name = "DH_Zone",        SpecType = ParameterSpecType.Text,  GroupType = ParameterGroupType.Data,         BindingType = ParameterBindingType.Instance, Categories = structuralCategories },
                new SharedParameterDefinition { Name = "DH_Part",        SpecType = ParameterSpecType.Text,  GroupType = ParameterGroupType.Data,         BindingType = ParameterBindingType.Instance, Categories = structuralCategories },
                new SharedParameterDefinition { Name = "DH_HostId",      SpecType = ParameterSpecType.Text,  GroupType = ParameterGroupType.Data,         BindingType = ParameterBindingType.Instance, Categories = new List<ParameterCategory> { ParameterCategory.GenericModel } },
                new SharedParameterDefinition { Name = "DH_Formula",     SpecType = ParameterSpecType.Text,  GroupType = ParameterGroupType.Data,         BindingType = ParameterBindingType.Instance, Categories = structuralCategories },
                new SharedParameterDefinition { Name = "DH_IsExterior",  SpecType = ParameterSpecType.YesNo, GroupType = ParameterGroupType.Data,         BindingType = ParameterBindingType.Instance, Categories = new List<ParameterCategory> { ParameterCategory.Walls } },
                new SharedParameterDefinition { Name = "DH_뷰 카테고리", SpecType = ParameterSpecType.Text,  GroupType = ParameterGroupType.IdentityData, BindingType = ParameterBindingType.Instance, Categories = new List<ParameterCategory> { ParameterCategory.Views } },
                new SharedParameterDefinition { Name = "DH_뷰 타입",     SpecType = ParameterSpecType.Text,  GroupType = ParameterGroupType.IdentityData, BindingType = ParameterBindingType.Instance, Categories = new List<ParameterCategory> { ParameterCategory.Views } },
            };
        }
        #endregion
    }
}
