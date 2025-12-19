using DHBIMWATER.Core.Domain.Entities;
using DHBIMWATER.Core.Interfaces.Services.Modeling;
using System.Diagnostics;

namespace DHBIMWATER.UI.Sandbox.Services.Modeling;

/// <summary>
/// Mock 배수지 모델링 서비스 (테스트용)
/// </summary>
public class MockReservoirModelingService : IReservoirModelingService
{
    public Task<bool> CreateReservoirAsync(DistributionReservoir reservoir)
    {
        Debug.WriteLine("=== Mock: 배수지 생성 ===");
        Debug.WriteLine($"이름: {reservoir.Name}");
        Debug.WriteLine($"형식: {reservoir.Type}");
        Debug.WriteLine($"치수: L{reservoir.InternalLength.Meters:F2}m x W{reservoir.InternalWidth.Meters:F2}m x H{reservoir.InternalHeight.Meters:F2}m");
        Debug.WriteLine($"용량: {reservoir.CalculateCapacity():F2} m³");
        Debug.WriteLine("배수지가 성공적으로 생성되었습니다 (Mock)");

        return Task.FromResult(true);
    }

    public Task<bool> UpdateReservoirAsync(DistributionReservoir reservoir)
    {
        Debug.WriteLine($"=== Mock: 배수지 수정 - {reservoir.Name} ===");
        return Task.FromResult(true);
    }

    public Task<bool> DeleteReservoirAsync(Guid reservoirId)
    {
        Debug.WriteLine($"=== Mock: 배수지 삭제 - {reservoirId} ===");
        return Task.FromResult(true);
    }
}
