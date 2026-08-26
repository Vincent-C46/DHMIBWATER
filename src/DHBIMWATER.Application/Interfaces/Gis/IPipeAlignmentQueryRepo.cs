using DHBIMWATER.Application.DTOs.Gis;

namespace DHBIMWATER.Application.Interfaces.Gis;

/// <summary>이미 생성된 관로 선형 DirectShape를 다시 읽어 Phase 2(패밀리/파이프 배치)에 넘길 수 있게 한다.</summary>
public interface IPipeAlignmentQueryRepo
{
    IReadOnlyList<PipeAlignmentQueryResult> GetByElementIds(IReadOnlyList<int> elementIds);
}
