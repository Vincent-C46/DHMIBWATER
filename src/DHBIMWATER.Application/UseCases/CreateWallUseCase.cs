using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.UseCases
{
    public class CreateWallUseCase
    {
        private readonly IWallCommandRepo _wallCmdRepo;
        private readonly ITransactionContext _transactionContext;

        #region Constructor
        public CreateWallUseCase(IWallCommandRepo wallCmdRepo)
        {
            _wallCmdRepo = wallCmdRepo;
        }
        #endregion

        #region Methods 
        public void Execute()
        {
            // 벽 생성 로직 구현
            _wallCmdRepo.CreateWall();
        }
        #endregion
    }
}
