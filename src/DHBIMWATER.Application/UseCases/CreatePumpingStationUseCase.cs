using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.UseCases
{
    public class CreatePumpingStationUseCase
    {
        #region Fields
        private readonly ITransactionContext _tx;
        private readonly ILevelQueryRepo _levelQueryRepo;
        private readonly ILevelCommandRepo _levelCmdRepo;
        private readonly IWallCommandRepo _wallCmdRepo;
        #endregion

        #region Properties

        #endregion

        #region Constructor
        public CreatePumpingStationUseCase(ITransactionContext tx,
                                           ILevelQueryRepo levelQueryRepo,
                                           ILevelCommandRepo levelCmdRepo,
                                           IWallCommandRepo wallCmdRepo)
        {
            _levelQueryRepo = levelQueryRepo;
            _levelCmdRepo = levelCmdRepo;
            _wallCmdRepo = wallCmdRepo;
            _tx = tx;
        }
        #endregion

        #region Methods
        public void Execute(PumpCreationRequestDto dto)
        {
            #region 1. 매개변수 정리
            var SelectedPumpingStationType = dto.DesignConditionDto.SelectedPumpingStationType;
            var SelectedEntranceType = dto.DesignConditionDto.SelectedEntranceType;
            var D = dto.DesignConditionDto.D;
            var HD = dto.DesignConditionDto.HD;
            var N = dto.DesignConditionDto.N;
            var LWL = dto.DesignConditionDto.LWL;
            var HWL = dto.DesignConditionDto.HWL;

            var B1 = dto.ProfileSpecDto.B1;
            var B3 = dto.ProfileSpecDto.B3;
            var B4 = dto.ProfileSpecDto.B4;
            var B6 = dto.ProfileSpecDto.B6;
            var B7 = dto.ProfileSpecDto.B7;
            var H1 = dto.ProfileSpecDto.H1;
            var H6 = dto.ProfileSpecDto.H6;
            var SelectedTheta = dto.ProfileSpecDto.SelectedTheta;
            var L1 = dto.ProfileSpecDto.L1;
            var L2 = dto.ProfileSpecDto.L2;
            var L3 = dto.ProfileSpecDto.L3;
            var L4 = dto.ProfileSpecDto.L4;
            var H3 = dto.ProfileSpecDto.H3;
            var H4 = dto.ProfileSpecDto.H4;
            var H7 = dto.ProfileSpecDto.H7;
            var OB1 = dto.ProfileSpecDto.OB1;
            var OH1 = dto.ProfileSpecDto.OH1;
            var NS = dto.ProfileSpecDto.NS;
            var HS = dto.ProfileSpecDto.HS;

            var B2 = dto.PlanSpecDto.B2;
            var B8 = dto.PlanSpecDto.B8;
            var SelectedOpeningType = dto.PlanSpecDto.SelectedOpeningType;
            var B5 = dto.PlanSpecDto.B5;
            var B9 = dto.PlanSpecDto.B9;
            var L5 = dto.PlanSpecDto.L5;
            var B10 = dto.PlanSpecDto.B10;

            var T1 = dto.TypeSelectionDto.T1;
            var T2 = dto.TypeSelectionDto.T2;
            var T3 = dto.TypeSelectionDto.T3;
            var T4 = dto.TypeSelectionDto.T4;
            var T5 = dto.TypeSelectionDto.T5;
            var GB1 = dto.TypeSelectionDto.GB1;
            var GH1 = dto.TypeSelectionDto.GH1;
            #endregion

            #region 2. 레벨 작성 준비
            string pumpFndLevelName = "기초(펌프)";
            string screenFndLevelName = "기초(유입부)";
            string valveRoomLevelName = "밸브실";
            string upperSlabLevelName = "상부슬래브";

            var pumpFndLevelElevation = LWL * 1000 - H4;
            var screenFndLevelElevation = LWL * 1000 - H1;
            var upperSlabLevelElevation = HWL * 1000 + H3 + T1;
            var valveRoomLevelElevation = upperSlabLevelElevation - H7 - D - H6;
            #endregion

            #region 3. 슬래브 작성 준비

            #endregion

            #region 4. 벽체 작성 준비

            #endregion

            #region 5. 보 작성 준비

            #endregion

            using (_tx)
            {
                try
                {
                    // 트랜잭션 시작
                    _tx.Begin("Create PumpingStation");

                    #region 레벨 생성
                    var existingLevels = _levelQueryRepo.GetExistingLevelNames();
                    var levelNamesToCreate = new List<string>() { pumpFndLevelName, screenFndLevelName, valveRoomLevelName, upperSlabLevelName, };
                    var elevations = new List<double>() { pumpFndLevelElevation, screenFndLevelElevation, valveRoomLevelElevation, upperSlabLevelElevation, };

                    int i = 0;
                    foreach (var lvl in levelNamesToCreate)
                    {
                        string matched = existingLevels.FirstOrDefault(s => s.Contains(lvl));

                        if (matched != null)
                        {
                            _levelCmdRepo.UpdateLevel(matched, elevations[i]);
                        }
                        else
                        {
                            _levelCmdRepo.CreateLevel(lvl, elevations[i]);
                        }
                        i++;
                    }
                    #endregion

                    #region 슬래브 생성

                    #endregion

                    #region 벽체 생성

                    _wallCmdRepo.CreateWall(dto.DesignConditionDto.HD, dto.DesignConditionDto.N);

                    List<Point3D> ptList = new List<Point3D>() { new Point3D(0, 0, 0), new Point3D(1000, 0, 0), new Point3D(1000, 0, 1000), new Point3D(0, 0, 1000), new Point3D(-500, 0, 500), };
                    _wallCmdRepo.CreateProfileWall(ptList, "일반 - 200mm", "레벨 1");
                    #endregion

                    #region 보 생성

                    #endregion

                    #region 결합

                    #endregion 


                    // 트랜잭션 커밋
                    _tx.Commit();
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
