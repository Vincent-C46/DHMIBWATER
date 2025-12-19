using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Infrastructure 관련 서비스 등록 (공통 인프라 서비스)
        // 예: 로깅, 데이터베이스 컨텍스트 등

        return services;
    }
}
