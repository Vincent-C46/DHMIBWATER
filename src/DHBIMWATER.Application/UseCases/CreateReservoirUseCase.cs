using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.UseCases
{
    public class CreateReservoirUseCase
    {
        #region Fields
        private readonly ILevelQueryRepo _levelQueryRepo;
        private readonly ILevelCommandRepo _levelCmdRepo;
        private readonly IWallCommandRepo _wallCmdRepo;
        private readonly ITransactionContext _tx;
        #endregion

        #region Properties

        #endregion

        #region Constructor
        public CreateReservoirUseCase(ILevelQueryRepo levelQueryRepo,
                                      ILevelCommandRepo levelCmdRepo,
                                      IWallCommandRepo wallCmdRepo,
                                      ITransactionContext tx)
        {
            _levelQueryRepo = levelQueryRepo;
            _levelCmdRepo = levelCmdRepo;
            _wallCmdRepo = wallCmdRepo;
            _tx = tx;
        }
        #endregion

        #region Methods 
        public void Execute(ReservoirCreationRequestDto dto)
        {
            using (_tx)
            {
                try
                {
                    // Transaction 시작
                    _tx.Begin("Create Reservoir");

                    // 기존 Level 리스트 조회
                    var existingLevels = _levelQueryRepo.GetExistingLevelNames();

                    // Level 생성
                    string tankFoundLevelName = "수조부 바닥슬래브";
                    string tankUpperLevelName = "수조부 상부슬래브";
                    string valveRoomFoundLevelName = "밸브실 바닥슬래브";
                    string valveRoomMidLevelName = "밸브실 중간슬래브";

                    var levelNamesToCreate = new List<string>() { tankFoundLevelName, tankUpperLevelName, valveRoomFoundLevelName, valveRoomMidLevelName };

                    foreach (var lvl in levelNamesToCreate)
                    {
                        if (existingLevels.Contains(lvl))
                        {
                            _levelCmdRepo.UpdateLevel(lvl, 20);
                        }
                        else
                        {
                            _levelCmdRepo.CreateLevel(lvl, 20);
                        }
                    }

                    // 바닥 생성

                    // 벽 생성
                    //_wallCmdRepo.CreateLinearWall(dto.DesignConditionDto.LWL, dto.DesignConditionDto.N);
                        
                    // 기둥 생성 (독립기초)
                    // 보 생성
                    // 결합
                    // 단면뷰 작성

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
