using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.Interfaces.Gis;

/// <summary>
/// 프로젝트와 무관한 <b>마스터</b> 규격 저장소. 핸드북 제원처럼 회사 표준으로 한 번 입력해
/// 모든 프로젝트가 공유하는 값을 담는다.
/// </summary>
/// <remarks>
/// Revit 문서 밖(앱 레벨)에 저장하므로 트랜잭션과 무관하다.
/// 프로젝트별로 다르게 써야 하는 값은 <see cref="IBendSettingsRepo"/>(DataStorage)가 덮어쓴다.
/// </remarks>
public interface IBendSettingsMasterStore
{
    /// <summary>저장된 마스터가 없으면 null.</summary>
    BendSettings? Load();

    /// <summary>트랜잭션 없이 즉시 기록한다.</summary>
    void Save(BendSettings settings);
}
