using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Infrastructure.Services.Mock;

namespace DHBIMWATER.Infrastructure.Repositories.Mock;

public sealed class MockNetFaceVisualizerRepo : INetFaceVisualizerRepo
{
    public int Visualize(IReadOnlyList<long> elementIds)
    {
        new MockDialogService().Info("순 면적 시각화", $"{elementIds.Count}개 요소의 순 면적 시각화 요청");
        return 0;
    }
}
