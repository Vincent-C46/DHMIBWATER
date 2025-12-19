using DHBIMWATER.Core.Domain.Entities;

namespace DHBIMWATER.Core.Interfaces.Services.Modeling;

/// <summary>
/// 배수지 전체 모델링 서비스
/// </summary>
public interface IReservoirModelingService
{
    /// <summary>
    /// 배수지 전체 생성
    /// </summary>
    Task<bool> CreateReservoirAsync(DistributionReservoir reservoir);

    /// <summary>
    /// 배수지 수정
    /// </summary>
    Task<bool> UpdateReservoirAsync(DistributionReservoir reservoir);

    /// <summary>
    /// 배수지 삭제
    /// </summary>
    Task<bool> DeleteReservoirAsync(Guid reservoirId);
}
