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

namespace DHBIMWATER.Application.UseCases
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
                                           IDialogService dialogService)
        {
            _levelQueryRepo = levelQueryRepo;
            _levelCmdRepo = levelCmdRepo;
            _wallCmdRepo = wallCmdRepo;
            _slabCmdRepo = slabCmdRepo;
            _beamCmdRepo = beamCmdRepo;
            _dsCmdRepo = dsCmdRepo;
            _dialogService = dialogService;
            _openingCmdRepo = openingCmdRepo;
            _tx = tx;
        }
        #endregion

        #region Methods
        public void Execute(PumpCreationRequestDto dto)
        {
            using (_tx)
            {
                try
                {
                    // 트랜잭션 시작
                    _tx.Begin("Create PumpingStation");

                    #region 0. 공유 매개변수 / 프로젝트 매개변수 생성

                    // GUID 하드코딩 - 변경 금지 ❌
                    var guidDict = new Dictionary<string, Guid>()
                                {
                                    { "DH_Addin",       new Guid("f0ff9795-a26f-4a2f-869c-532d2c418fac") },
                                    { "DH_ElementCode", new Guid("280f0a33-1456-4c43-9f01-ae6acf80769b") },
                                    { "DH_Class",       new Guid("5b93b43c-abd7-435b-81b1-6b7ff51419c8") },
                                    { "DH_Category",    new Guid("7e539f5b-80e4-4dd9-a7e1-ac070dbcaa24") },
                                    { "DH_Zone",        new Guid("b60a47f3-3ede-45db-8491-b8f70de329a4") },
                                    { "DH_Part",        new Guid("47c74ae0-3fc8-4a06-9c4e-80713b564b0c") },
                                    // 형상 치수 정보
                                    { "L1",             new Guid("24ef3fdc-96cb-4e32-bb49-c8ecacd92a58") },
                                    { "W1",             new Guid("a6f6385b-2364-4288-a239-813d49e4a572") },
                                    { "L2",             new Guid("180771e8-b65a-4f97-a8e4-d00a67fa4823") },
                                    { "W2",             new Guid("f65c9ace-ffde-4b4f-a01c-b7dd303b0836") },
                                    { "L3",             new Guid("f074ebb2-5306-4082-888f-99d53b4f6f9e") },
                                    { "W3",             new Guid("e8fced45-637e-42bd-b9a9-e29f319e9f39") },
                                    { "H",              new Guid("10337573-871a-40b7-8a3d-4dc3637e9349") },
                                    { "ETC",            new Guid("5ef1d2b7-8766-482f-a81c-22fdcba1e72a") },
                                    //{ "DH_RowNum",      new Guid("98122773-6f3c-49dc-a817-5bfb065d94a1") },
                                    //{ "DH_ColNum",      new Guid("a207e5bc-87dd-4062-973f-149777f98762") },
                                };


                    var def1 = new SharedParameterDefinition() 
                    { 
                        BindingType = ParameterBindingType.Instance,
                        GroupName = "DH_PumpingStation",
                        GroupType = ParameterGroupType.Geometry,
                        Name = "펌프장_구획명",
                        SpecType = ParameterSpecType.Text,
                        UserModifiable = true,
                        Categories = new List<ParameterCategory>() { ParameterCategory.StructuralFraming, 
                                                                     ParameterCategory.StructuralColumns }
                    };


                    #endregion

                    #region 1. 레벨 생성
                    var existingLevels = _levelQueryRepo.GetExistingLevelNames();
                    var existingViewNames = _levelQueryRepo.GetExistingPlanNames();

                    foreach (var lvl in PumpingStationGeometryCalculator.CalculateLevels(dto))
                    {
                        var existLevel = existingLevels.FirstOrDefault(s => s.Contains(lvl.Name));
                        if (existLevel != null)
                        {
                            _levelCmdRepo.UpdateLevel(existLevel, lvl.Elevation);
                        }
                        else
                        {
                            _levelCmdRepo.CreateLevel(lvl.Name, lvl.Elevation);
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
        #endregion
    }
}
