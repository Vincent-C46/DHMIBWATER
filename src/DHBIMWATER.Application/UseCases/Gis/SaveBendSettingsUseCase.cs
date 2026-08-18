using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.UseCases.Gis;

/// <summary>설정을 어디까지 저장할지.</summary>
public enum BendSettingsScope
{
    /// <summary>이 문서에만 저장(프로젝트 오버라이드).</summary>
    Project,

    /// <summary>회사 표준으로 마스터에 저장하고, 이 문서에도 함께 적용한다.</summary>
    Master
}

/// <summary>직관·곡관 규격과 Joint 허용굴곡 설정을 한 트랜잭션에 함께 저장한다.</summary>
public sealed class SaveBendSettingsUseCase
{
    private readonly ITransactionContext _transaction;
    private readonly IBendSettingsRepo _repo;
    private readonly IBendSettingsMasterStore _master;

    public SaveBendSettingsUseCase(ITransactionContext transaction, IBendSettingsRepo repo, IBendSettingsMasterStore master)
    { _transaction = transaction; _repo = repo; _master = master; }

    public void Execute(BendSettings settings, BendSettingsScope scope = BendSettingsScope.Project)
    {
        // 마스터는 Revit 문서 밖이라 트랜잭션 밖에서 먼저 기록한다.
        // 트랜잭션이 롤백돼도 마스터가 남지만, 마스터는 회사 표준이라 프로젝트 저장 실패와 무관하게 유효하다.
        if (scope == BendSettingsScope.Master) _master.Save(settings);

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
