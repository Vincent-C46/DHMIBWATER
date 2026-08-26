using DHBIMWATER.Application.DTOs.Gis;

namespace DHBIMWATER.Application.Interfaces.Gis;

public interface IAdaptiveBendPlacementRepo
{
    /// <summary>
    /// Adaptive Point 5개(P1~P5)를 갖는 곡관 패밀리를 배치한다. Connector 연결은 하지 않는다
    /// (곡관은 Beam 모드 전용이라 인접 요소에 MEP Connector가 없다 — 사용자 결정 2026-08-11).
    /// Transaction은 호출부(UseCase)가 관리한다.
    /// </summary>
    AdaptiveBendPlacementResult Place(IReadOnlyList<AdaptiveBendPlacementPlan> plans, AlignmentPlacementOrigin origin, PipeInfoParameterContext? info = null, IProgress<PipeAlignmentProgress>? progress = null);

    /// <summary>
    /// 카탈로그가 가리키는 (Family, Type) 중 현재 문서에 로드돼 있지 않은 것만 골라 돌려준다. 읽기 전용이라
    /// Transaction 없이 호출할 수 있다. 트랜잭션을 열기 전에 미리 확인해, 없는 패밀리 때문에 직관 배치까지
    /// 끝낸 뒤 전체가 롤백되는 상황을 막는 용도다.
    /// </summary>
    IReadOnlyList<(string FamilyName, string TypeName)> FindMissingSymbols(IEnumerable<(string FamilyName, string TypeName)> pairs);
}
