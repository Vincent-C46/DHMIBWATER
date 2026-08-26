using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.UseCases.Gis;

/// <summary>직관·곡관 규격과 Joint 허용굴곡 설정을 한 트랜잭션에 함께 저장한다.</summary>
public sealed class SaveBendSettingsUseCase
{
    private readonly ITransactionContext _transaction;
    private readonly IBendSettingsRepo _repo;

    public SaveBendSettingsUseCase(ITransactionContext transaction, IBendSettingsRepo repo)
    { _transaction = transaction; _repo = repo; }

    public void Execute(BendSettings settings)
    {
        using (_transaction)
        {
            try
            {
                _transaction.Begin("관로 규격 설정 저장");
                _repo.Save(settings);
                _transaction.Commit();
            }
            catch { _transaction.Rollback(); throw; }
        }
    }
}
