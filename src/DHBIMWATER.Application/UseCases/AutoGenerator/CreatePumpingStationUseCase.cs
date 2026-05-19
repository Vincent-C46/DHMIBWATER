using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Services;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Parameters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DHBIMWATER.Application.UseCases.AutoGenerator
{
    public class CreatePumpingStationUseCase
    {
        #region Fields
        private readonly ITransactionContext _tx;
        private readonly ILevelQueryRepo _levelQueryRepo;
        private readonly ILevelCommandRepo _levelCmdRepo;
        private readonly IWallCommandRepo _wallCmdRepo;
        private readonly ISlabCommandRepo _slabCmdRepo;
        private readonly IBeamCommandRepo _beamCmdRepo;
        private readonly IDirectShapeCommandRepo _dsCmdRepo;
        private readonly IOpeningCommandRepo _openingCmdRepo;
        private readonly IDialogService _dialogService;
        private readonly ISharedParameterRepository _sharedParameterRepo;
        private readonly IViewCommandRepo _viewCommandRepo;
        private readonly IExcelReader _excelReader;
        #endregion

        #region Properties

        #endregion

        #region Constructor
        public CreatePumpingStationUseCase(ITransactionContext tx,
                                           ILevelQueryRepo levelQueryRepo,
                                           ILevelCommandRepo levelCmdRepo,
                                           ISlabCommandRepo slabCmdRepo,
                                           IWallCommandRepo wallCmdRepo,
                                           IBeamCommandRepo beamCmdRepo,
                                           IDirectShapeCommandRepo dsCmdRepo,
                                           IOpeningCommandRepo openingCmdRepo,
                                           IDialogService dialogService,
                                           ISharedParameterRepository sharedParameterRepo,
                                           IViewCommandRepo viewCommandRepo,
                                           IExcelReader excelReader)
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
            _excelReader = excelReader;

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

                    // Level 생성
                    foreach (var lvl in PumpingStationGeometryCalculator.CalculateLevels(dto))
                    {
                        var existLevel = existingLevels.FirstOrDefault(s => s.Contains(lvl.Name));
                        int levelId;
                        if (existLevel != null)
                        {
                            levelId = _levelCmdRepo.UpdateLevel(existLevel, lvl.Elevation);
                        }
                        else
                        {
                            levelId = _levelCmdRepo.CreateLevel(lvl.Name, lvl.Elevation);
                        }

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

                    #region 6. 결합
                    // 보 작성 메서드 내부에서 상부 슬래브와 결합 (임시 조치)
                    #endregion

                    #region 7. 뷰 작성                    
                    var existingSectionViewNames  = _levelQueryRepo.GetExistingSectionNames();
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
                    #endregion

                    // 트랜잭션 커밋
                    _tx.Commit();

                    _dialogService.Info("Success", "펌프장 작성 완료");
                }
                catch (Exception)
                {
                    _tx.Rollback();
                    throw;
                }
            }
        }

        // 공유 매개변수 작성
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
                Categories = new List<ParameterCategory>() { ParameterCategory.GenericModel,},
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

            var addList = new List<SharedParameterDefinition>() { def1, def2, def3, def4, def5, def6, def7, def8, };
            defs.AddRange(addList);

            return defs;
        }
        #endregion
    }
}
