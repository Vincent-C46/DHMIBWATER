using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Core.Gis;
namespace DHBIMWATER.Application.Interfaces.Gis;
public interface IAlignmentBeamPlacementRepo
{
    /// <param name="trims">
    /// 선형별 정점 차감량(곡관 몸통 자리). 인덱스는 <paramref name="alignments"/>와 맞춘다.
    /// null이면 차감 없이 절점까지 붙인다 — 곡관 판정 이전 호출부의 기존 동작이다.
    /// </param>
    /// <param name="diameterParameterName">빔 인스턴스에 직경(mm)을 기록할 파라미터명. null이면 기록하지 않는다.</param>
    /// <param name="kindParameterName">빔 인스턴스에 관종을 기록할 파라미터명. null이면 기록하지 않는다.</param>
    int PlaceAlong(IReadOnlyList<PipeAlignment> alignments, string beamTypeName, string? levelName, double intervalM, bool alignTangent, AlignmentPlacementOrigin origin,
        IReadOnlyList<IReadOnlyList<VertexTrim>>? trims = null, string? diameterParameterName = null, string? kindParameterName = null);
}
