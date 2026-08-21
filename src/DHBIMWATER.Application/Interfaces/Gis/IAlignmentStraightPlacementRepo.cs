using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Core.Gis;
namespace DHBIMWATER.Application.Interfaces.Gis;

/// <summary>
/// 관로 직관을 2점 가변(Adaptive Component) 패밀리로 배치한다.
/// 곡관(5점)은 <see cref="IAdaptiveBendPlacementRepo"/>가 맡으며, 둘이 같은 표현 체계를 이룬다.
/// </summary>
public interface IAlignmentStraightPlacementRepo
{
    /// <param name="straightFamilyTypeName">직관 패밀리·타입("패밀리명 : 타입명"). Adaptive Point가 2개여야 한다.</param>
    /// <param name="trims">
    /// 선형별 정점 차감량(곡관 몸통 자리). 인덱스는 <paramref name="alignments"/>와 맞춘다.
    /// null이면 차감 없이 절점까지 붙인다 — 곡관 판정 이전 호출부의 기존 동작이다.
    /// </param>
    /// <param name="diameterParameterName">인스턴스에 호칭지름(mm)을 기록할 파라미터명. null이면 기록하지 않는다.</param>
    AlignmentStraightPlacementResult PlaceAlong(IReadOnlyList<PipeAlignment> alignments, string straightFamilyTypeName, double intervalM, AlignmentPlacementOrigin origin,
        StraightPipeSpecTable specs, IReadOnlyList<IReadOnlyList<VertexTrim>>? trims = null,
        string? diameterParameterName = null, string? outerDiameterParameterName = null, string? thicknessParameterName = null,
        PipeInfoParameterContext? info = null);
}
