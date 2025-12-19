using DHBIMWATER.Core.Domain.Entities;

namespace DHBIMWATER.Core.Interfaces.Services.Modeling;

/// <summary>
/// 벽체 모델링 서비스
/// </summary>
public interface IWallModelingService
{
    /// <summary>
    /// 벽체 생성
    /// </summary>
    Task<bool> CreateWallAsync(ReservoirWall wall);

    /// <summary>
    /// 여러 벽체 일괄 생성
    /// </summary>
    Task<bool> CreateWallsAsync(IEnumerable<ReservoirWall> walls);
}
