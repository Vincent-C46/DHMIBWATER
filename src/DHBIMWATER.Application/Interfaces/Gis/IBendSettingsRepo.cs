using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.Interfaces.Gis;

public interface IBendSettingsRepo
{
    /// <summary>저장된 설정이 없으면 null. 기본값 대체는 호출부(UseCase/VM)가 결정한다.</summary>
    BendSettings? Load();

    /// <summary>트랜잭션은 열지 않는다. 반드시 UseCase의 트랜잭션 안에서 호출된다.</summary>
    void Save(BendSettings settings);
}
