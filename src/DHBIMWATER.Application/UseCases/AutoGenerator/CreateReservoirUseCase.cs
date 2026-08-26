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
        private readonly IGenericModelCommandRepo _genericModelCmdRepo;
        private readonly IElementTypeCommandRepo _elementTypeCmdRepo;
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
            IGenericModelCommandRepo genericModelCmdRepo, IElementTypeCommandRepo elementTypeCmdRepo,
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
            _genericModelCmdRepo = genericModelCmdRepo;
            _elementTypeCmdRepo = elementTypeCmdRepo;
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
                    var slabDefs = ReservoirGeometryCalculator.CalculateSlabs(dto).ToList();
                    var slabLevels = _levelQueryRepo.GetLevelIds(slabDefs.Select(s => s.LevelName));
                    var slabConcrete = new Core.Structures.ConcreteSpec(25, 30, 150);
                    var slabMaterial = _elementTypeCmdRepo.FindOrCreateConcreteMaterial(slabConcrete);
                    var slabTypes = slabDefs.Select(s => s.Thickness).Distinct().ToDictionary(t => t, t => _elementTypeCmdRepo.FindOrCreateSlabType(new Core.Structures.FloorTypeSpec(t, $"일반 - {t}mm", slabConcrete), slabMaterial));
                    foreach (var slabDef in slabDefs)
                        _slabCmdRepo.CreateSlab(slabDef, slabLevels[slabDef.LevelName], slabTypes[slabDef.Thickness]);
                    #endregion

                    #region 3. 벽체 생성
                    var wallDefs = ReservoirGeometryCalculator.CalculateLinearWalls(dto).ToList();
                    var levelIds = _levelQueryRepo.GetLevelIds(wallDefs.Select(w => w.LevelName));
                    var concrete = new Core.Structures.ConcreteSpec(25, 27, 120);
                    var materialId = _elementTypeCmdRepo.FindOrCreateConcreteMaterial(concrete);
                    var wallTypeIds = wallDefs.Select(w => w.Thickness).Distinct().ToDictionary(
                        thickness => thickness,
                        thickness => _elementTypeCmdRepo.FindOrCreateWallType(new Core.Structures.WallTypeSpec(thickness, $"일반 - {thickness}mm", concrete), materialId));
                    foreach (var wallDef in wallDefs)
                        _wallCmdRepo.CreateLinearWall(wallDef, levelIds[wallDef.LevelName], wallTypeIds[wallDef.Thickness]);
                    _classifyWallsUseCase.Execute();
                    #endregion

                    #region 4. 기둥 생성
                    var columnDefs = ReservoirGeometryCalculator.CalculateColumns(dto).ToList();
                    var columnLevels = _levelQueryRepo.GetLevelIds(columnDefs.SelectMany(c => new[] { c.BaseLevelName, c.TopLevelName }));
                    var columnTypes = columnDefs.Select(c => c.TypeName).Distinct().ToDictionary(name => name, _elementTypeCmdRepo.FindColumnSymbol);
                    foreach (var colDef in columnDefs)
                        _columnCmdRepo.CreateColumn(colDef, columnLevels[colDef.BaseLevelName], columnLevels[colDef.TopLevelName], columnTypes[colDef.TypeName]);
                    #endregion

                    #region 5. 보 생성
                    var beamDefs = ReservoirGeometryCalculator.CalculateBeams(dto).ToList();
                    var beamLevels = _levelQueryRepo.GetLevelIds(beamDefs.Select(b => b.LevelName));
                    var beamTypes = beamDefs.GroupBy(b => b.Part == "HAUNCH" ? "HAUNCH" : b.TypeName ?? $"{b.Width} x {b.Height}").ToDictionary(g => g.Key, g => g.Key == "HAUNCH" ? _elementTypeCmdRepo.FindHaunchBeamSymbol() : !string.IsNullOrEmpty(g.First().TypeName) ? _elementTypeCmdRepo.FindBeamSymbol(g.First().TypeName) : _elementTypeCmdRepo.FindOrCreateBeamType(new Core.Structures.BeamTypeSpec(g.First().Width, g.First().Height, g.Key)));
                    foreach (var beamDef in beamDefs)
                        _beamCmdRepo.CreateBeam(beamDef, beamLevels[beamDef.LevelName], beamTypes[beamDef.Part == "HAUNCH" ? "HAUNCH" : beamDef.TypeName ?? $"{beamDef.Width} x {beamDef.Height}"]);
                    #endregion

                    #region 6. 일반 패밀리 배치 (단차버림 Con'c, PIT)
                    var genericDefs = ReservoirGeometryCalculator.CalculateGenericModels(dto).ToList();
                    var genericLevels = _levelQueryRepo.GetLevelIds(genericDefs.Select(g => g.LevelName));
                    var genericSymbols = genericDefs.Select(g => g.SymbolName).Distinct().ToDictionary(name => name, _elementTypeCmdRepo.FindGenericModelSymbol);
                    foreach (var def in genericDefs)
                        _genericModelCmdRepo.PlaceInstance(def, genericLevels[def.LevelName], genericSymbols[def.SymbolName]);
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

