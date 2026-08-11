using DHBIMWATER.Application.DTOs.Gis;

namespace DHBIMWATER.Application.Interfaces.Gis;

public interface IAdaptiveBendPlacementRepo
{
    /// <summary>
    /// Adaptive Point 5개(P1~P5)를 갖는 곡관 패밀리를 배치한다. Connector 연결은 하지 않는다
    /// (곡관은 Beam 모드 전용이라 인접 요소에 MEP Connector가 없다 — 사용자 결정 2026-08-11).
    /// Transaction은 호출부(UseCase)가 관리한다.
    /// </summary>
    int Place(IReadOnlyList<AdaptiveBendPlacementPlan> plans, AlignmentPlacementOrigin origin);
}
