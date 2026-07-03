using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Services;
using DHBIMWATER.Core.Parameters;

namespace DHBIMWATER.Application.UseCases.AutoGenerator
{
    public class CreatePumpingStationUseCase
    {
        #region Fields
        private readonly ITransactionContext _tx;
        private readonly IDialogService _dialogService;
        private readonly ILevelQueryRepo _levelQueryRepo;
        private readonly ILevelCommandRepo _levelCmdRepo;
        private readonly IWallCommandRepo _wallCmdRepo;
        private readonly ISlabCommandRepo _slabCmdRepo;
        private readonly IBeamCommandRepo _beamCmdRepo;
        private readonly IDirectShapeCommandRepo _dsCmdRepo;
        private readonly IOpeningCommandRepo _openingCmdRepo;
        private readonly ISharedParameterRepository _sharedParameterRepo;
        private readonly IViewCommandRepo _viewCommandRepo;
        private readonly ISetParameterRepo _setParameterRepo;
        private readonly IGenericModelCommandRepo _genericModelCmdRepo;
        private readonly IExcelReader _excelReader;
        private readonly ClassifyExteriorWallsUseCase _classifyWallsUseCase;
        #endregion

        #region Properties

        #endregion

        #region Constructor
        public CreatePumpingStationUseCase(ITransactionContext tx,
                                           IDialogService dialogService,
                                           ILevelQueryRepo levelQueryRepo,
                                           ILevelCommandRepo levelCmdRepo,
                                           ISlabCommandRepo slabCmdRepo,
                                           IWallCommandRepo wallCmdRepo,
                                           IBeamCommandRepo beamCmdRepo,
                                           IDirectShapeCommandRepo dsCmdRepo,
                                           IOpeningCommandRepo openingCmdRepo,
                                           ISharedParameterRepository sharedParameterRepo,
                                           IViewCommandRepo viewCommandRepo,
                                           ISetParameterRepo setParameterRepo,
                                           IGenericModelCommandRepo genericModelCmdRepo,
                                           IExcelReader excelReader,
                                           ClassifyExteriorWallsUseCase classifyWallsUseCase)
        {
            _levelQueryRepo = levelQueryRepo;
            _levelCmdRepo = levelCmdRepo;
            _wallCmdRepo = wallCmdRepo;
            _slabCmdRepo = slabCmdRepo;
            _beamCmdRepo = beamCmdRepo;
            _dsCmdRepo = dsCmdRepo;
            _dialogService = dialogService;
            _openingCmdRepo = openingCmdRepo;
            _sharedParameterRepo = sharedParameterRepo;
            _viewCommandRepo = viewCommandRepo;
            _setParameterRepo = setParameterRepo;
            _genericModelCmdRepo = genericModelCmdRepo;
            _excelReader = excelReader;
            _classifyWallsUseCase = classifyWallsUseCase;

            _tx = tx;
        }
        #endregion

        #region Methods
        public void Execute(PumpCreationRequestDto dto)
        {
            #region Excel 데이터 가져오기
            ////File.Open();
            //string excelFilePath = @"F:\02_Work\02_Project\06_펌프장\01_Docs\input data 산식.xlsx";
            //var excelDict = _excelReader.Read(excelFilePath);

            //_dialogService.Info("Success", $"Sheet 개수: {excelDict.Count}");
            //_dialogService.Info("Success", $"{excelDict["종단제원 입력"][28][13]}");
            #endregion

            using (_tx)
            {
                try
                {
                    // 트랜잭션 시작
                    _tx.Begin("Create PumpingStation");

                    #region 0. 공유 매개변수 / 프로젝트 매개변수 생성
                    var defs = GetPumpSharedParameterDefinitions();
                    _sharedParameterRepo.EnsureParameters(defs);
                    #endregion

                    #region 1. 레벨 생성
                    var existingLevels = _levelQueryRepo.GetExistingLevelNames();
                    var existingEngineeringPlanNames = _levelQueryRepo.GetExistingPlanNames();
                    var levels = new List<long>();

                    // Level 생성
                    foreach (var lvl in PumpingStationGeometryCalculator.CalculateLevels(dto))
                    {
                        var existLevel = existingLevels.FirstOrDefault(s => s.Contains(lvl.Name));
                        long levelId;

                        if (existLevel != null)
                        {
                            levelId = _levelCmdRepo.UpdateLevel(existLevel, lvl.Elevation);
                        }
                        else
                        {
                            levelId = _levelCmdRepo.CreateLevel(lvl.Name, lvl.Elevation);
                        }

                        levels.Add(levelId);

                        if (lvl.Name.Contains("LWL") || lvl.Name.Contains("HWL")) continue;

                        // 구조도 작성
                        if (!existingEngineeringPlanNames.Contains(lvl.Name))
                        {
                            _levelCmdRepo.CreatePlan(levelId);
                        }
                    }
                    #endregion

                    #region 2. 슬래브 생성
                    foreach (var slabDef in PumpingStationGeometryCalculator.CalculateSlabs(dto))
                        _slabCmdRepo.CreateSlab(slabDef);

                    // 기초 다이렉트쉐이프
                    var dsDefs = PumpingStationGeometryCalculator.CalculateSolids(dto);
                    var ids = _dsCmdRepo.CreateDirectShapes(dsDefs);
                    #endregion

                    #region 3. 벽체 생성
                    foreach (var linearWallDef in PumpingStationGeometryCalculator.CalculateLinearWalls(dto))
                        _wallCmdRepo.CreateLinearWall(linearWallDef);
                    foreach (var profileWallDef in PumpingStationGeometryCalculator.CalculateProfileWalls(dto))
                        _wallCmdRepo.CreateProfileWall(profileWallDef);

                    var hull = _classifyWallsUseCase.Execute();
                    var hullStr = string.Join("\n", hull.Select((p, i) => $"[{i}] X={p.X:F0}  Y={p.Y:F0}"));
                    //_dialogService.Info("DEBUG - Hull 꼭짓점 (mm)", hullStr);
                    #endregion

                    #region 4. 보 생성
                    foreach (var beamDef in PumpingStationGeometryCalculator.CalculateBeams(dto))
                        _beamCmdRepo.CreateBeam(beamDef);
                    #endregion

                    #region 5. 오프닝 배치
                    // 슬래브 오프닝 (사각형)
                    foreach (var openingDef in PumpingStationGeometryCalculator.CalculateRectangularSlabOpenings(dto))
                        _openingCmdRepo.CreateSlabOpening(openingDef);
                    // 슬래브 오프닝 (원형)
                    foreach (var openingDef in PumpingStationGeometryCalculator.CalculateCircularSlabOpenings(dto))
                        _openingCmdRepo.CreateSlabOpening(openingDef);
                    // 벽체 오프닝 (사각형)
                    foreach (var openingDef in PumpingStationGeometryCalculator.CalculateRectangularWallOpenings(dto))
                        _openingCmdRepo.CreateWallOpening(openingDef);
                    // 벽체 오프닝 (원형)
                    foreach (var openingDef in PumpingStationGeometryCalculator.CalculateCircularWallOpenings(dto))
                        _openingCmdRepo.CreateWallOpening(openingDef);
                    #endregion

                    #region 6. 펌프받침 배치 (FamilyInstance)
                    foreach (var def in PumpingStationGeometryCalculator.CalculateGenericModels(dto))
                        _genericModelCmdRepo.PlaceInstance(def);
                    #endregion

                    #region 7. 결합
                    // 보 작성 메서드 내부에서 상부 슬래브와 결합 (임시 조치)
                    #endregion

                    #region 8. 뷰 작성                    
                    var existingSectionViewNames = _levelQueryRepo.GetExistingSectionNames();
                    var sectionViewDefs = PumpingStationGeometryCalculator.CalculateSectionViews(dto);

                    foreach (var viewDef in sectionViewDefs)
                    {

                        try
                        {
                            _viewCommandRepo.CreateSectionView(viewDef);
                        }
                        catch (Exception ex)
                        {
                            _dialogService.Warn("Error", $"Failed to create section view '{viewDef.Name}': {ex.Message}");
                        }
                    }
                    // Level 3D 범위 최대화
                    foreach(var levelId in levels)
                    {
                        _levelCmdRepo.Maximize3dExtents(levelId);
                    }
                    #endregion

                    #region 9. 타입 설명 추가
                    _setParameterRepo.SetTypeParameter(dto);
                    #endregion

                    // 트랜잭션 커밋
                    _tx.Commit();

                    _dialogService.Info("Success", "펌프장 작성 완료");
                }
                catch (Exception ex)
                {
                    _dialogService.Warn("Rollback", ex.Message);
                    _tx.Rollback();
                    throw;
                }
            }
        }

        // 공유 매개변작성
        private List<SharedParameterDefinition> GetPumpSharedParameterDefinitions()
        {
            var defs = new List<SharedParameterDefinition>();

            var def1 = new SharedParameterDefinition()
            {
                Name = "DH_ElementCode",
                SpecType = ParameterSpecType.Text,
                GroupType = ParameterGroupType.Data,
                BindingType = ParameterBindingType.Instance,
                Categories = new List<ParameterCategory>() { ParameterCategory.StructuralFraming,
                                                             ParameterCategory.StructuralColumns,
                                                             ParameterCategory.GenericModel,
                                                             ParameterCategory.Floors,
                                                             ParameterCategory.Walls,
                                                             ParameterCategory.Stairs,},
            };

            var def2 = new SharedParameterDefinition()
            {
                Name = "DH_Addin",
                SpecType = ParameterSpecType.Text,
                GroupType = ParameterGroupType.Data,
                BindingType = ParameterBindingType.Instance,
                Categories = new List<ParameterCategory>() { ParameterCategory.StructuralFraming,
                                                             ParameterCategory.StructuralColumns,
                                                             ParameterCategory.GenericModel,
                                                             ParameterCategory.Floors,
                                                             ParameterCategory.Walls,
                                                             ParameterCategory.Stairs,},
                UserModifiable = false,
            };

            var def3 = new SharedParameterDefinition()
            {
                Name = "DH_Class",
                SpecType = ParameterSpecType.Text,
                GroupType = ParameterGroupType.Data,
                BindingType = ParameterBindingType.Instance,
                Categories = new List<ParameterCategory>() { ParameterCategory.StructuralFraming,
                                                             ParameterCategory.StructuralColumns,
                                                             ParameterCategory.GenericModel,
                                                             ParameterCategory.Floors,
                                                             ParameterCategory.Walls,
                                                             ParameterCategory.Stairs,},
            };

            var def4 = new SharedParameterDefinition()
            {
                Name = "DH_Category",
                SpecType = ParameterSpecType.Text,
                GroupType = ParameterGroupType.Data,
                BindingType = ParameterBindingType.Instance,
                Categories = new List<ParameterCategory>() { ParameterCategory.StructuralFraming,
                                                             ParameterCategory.StructuralColumns,
                                                             ParameterCategory.GenericModel,
                                                             ParameterCategory.Floors,
                                                             ParameterCategory.Walls,
                                                             ParameterCategory.Stairs,},
            };

            var def5 = new SharedParameterDefinition()
            {
                Name = "DH_Zone",
                SpecType = ParameterSpecType.Text,
                GroupType = ParameterGroupType.Data,
                BindingType = ParameterBindingType.Instance,
                Categories = new List<ParameterCategory>() { ParameterCategory.StructuralFraming,
                                                             ParameterCategory.StructuralColumns,
                                                             ParameterCategory.GenericModel,
                                                             ParameterCategory.Floors,
                                                             ParameterCategory.Walls,
                                                             ParameterCategory.Stairs,},
            };

            var def6 = new SharedParameterDefinition()
            {
                Name = "DH_Part",
                SpecType = ParameterSpecType.Text,
                GroupType = ParameterGroupType.Data,
                BindingType = ParameterBindingType.Instance,
                Categories = new List<ParameterCategory>() { ParameterCategory.StructuralFraming,
                                                             ParameterCategory.StructuralColumns,
                                                             ParameterCategory.GenericModel,
                                                             ParameterCategory.Floors,
                                                             ParameterCategory.Walls,
                                                             ParameterCategory.Stairs,},
            };

            var def7 = new SharedParameterDefinition()
            {
                Name = "DH_HostId",
                SpecType = ParameterSpecType.Text,
                GroupType = ParameterGroupType.Data,
                BindingType = ParameterBindingType.Instance,
                Categories = new List<ParameterCategory>() { ParameterCategory.GenericModel, },
            };

            var def8 = new SharedParameterDefinition()
            {
                Name = "DH_Formula",
                SpecType = ParameterSpecType.Text,
                GroupType = ParameterGroupType.Data,
                BindingType = ParameterBindingType.Instance,
                Categories = new List<ParameterCategory>() { ParameterCategory.StructuralFraming,
                                                             ParameterCategory.StructuralColumns,
                                                             ParameterCategory.GenericModel,
                                                             ParameterCategory.Floors,
                                                             ParameterCategory.Walls,
                                                             ParameterCategory.Stairs,},
            };
            var def9 = new SharedParameterDefinition()
            {
                Name = "DH_IsExterior",
                SpecType = ParameterSpecType.YesNo,
                GroupType = ParameterGroupType.Data,
                BindingType = ParameterBindingType.Instance,
                Categories = new List<ParameterCategory>() { ParameterCategory.Walls, },
            };
            var def10 = new SharedParameterDefinition()
            {
                Name = "DH_뷰 카테고리",
                SpecType = ParameterSpecType.Text,
                GroupType = ParameterGroupType.IdentityData,
                BindingType = ParameterBindingType.Instance,
                Categories = new List<ParameterCategory>() { ParameterCategory.Views, },
            };

            var def11 = new SharedParameterDefinition()
            {
                Name = "DH_뷰 타입",
                SpecType = ParameterSpecType.Text,
                GroupType = ParameterGroupType.IdentityData,
                BindingType = ParameterBindingType.Instance,
                Categories = new List<ParameterCategory>() { ParameterCategory.Views, },

            };

            var addList = new List<SharedParameterDefinition>() { def1, def2, def3, def4, def5, def6, def7, def8, def9, def10, def11, };
            defs.AddRange(addList);

            return defs;
        }
        #endregion
    }
}
