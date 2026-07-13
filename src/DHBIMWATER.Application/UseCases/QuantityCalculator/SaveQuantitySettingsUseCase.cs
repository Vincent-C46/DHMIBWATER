using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Storage;
using DHBIMWATER.Core.Settings;

namespace DHBIMWATER.Application.UseCases.QuantityCalculator
{
    /// <summary>
    /// 수량산출 설정을 DataStorage에 저장한다 (Transaction은 이 UseCase에서 관리 — CLAUDE.md 규칙).
    /// </summary>
    public class SaveQuantitySettingsUseCase
    {
        private readonly ITransactionContext _tx;
        private readonly IQuantitySettingsRepository _repo;

        public SaveQuantitySettingsUseCase(ITransactionContext tx, IQuantitySettingsRepository repo)
        {
            _tx = tx;
            _repo = repo;
        }

        public void Execute(ProjectSettings settings)
        {
            using (_tx)
            {
                try
                {
                    _tx.Begin("Save Quantity Settings");
                    _repo.Save(settings);
                    _tx.Commit();
                }
                catch
                {
                    _tx.Rollback();
                    throw;
                }
            }
        }
    }
}
