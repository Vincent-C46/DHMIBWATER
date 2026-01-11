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
        private readonly IWallCommandRepo _wallCmdRepo;
        private readonly ITransactionContext _tx;

        #endregion

        #region Properties

        #endregion

        #region Constructor
        public CreateReservoirUseCase(IWallCommandRepo wallCmdRepo, ITransactionContext tx)
        {
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
                    _tx.Begin("Create Wall");

                    _wallCmdRepo.CreateWall(dto.DesignConditionDto.LWL, dto.DesignConditionDto.N);

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
