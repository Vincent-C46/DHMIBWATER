using DHBIMWATER.Core.Domain.Entities;

namespace DHBIMWATER.Core.Interfaces.Services.Modeling;

/// <summary>
/// 슬래브 모델링 서비스
/// </summary>
public interface ISlabModelingService
{
    /// <summary>
    /// 슬래브 생성
    /// </summary>
    Task<bool> CreateSlabAsync(ReservoirSlab slab);

    /// <summary>
    /// 바닥 슬래브 생성
    /// </summary>
    Task<bool> CreateFloorSlabAsync(ReservoirSlab slab);

    /// <summary>
    /// 지붕 슬래브 생성
    /// </summary>
    Task<bool> CreateRoofSlabAsync(ReservoirSlab slab);
}
