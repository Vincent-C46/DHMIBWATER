using DHBIMWATER.Application.Interface;
using DHBIMWATER.Application.UseCases;
using DHBIMWATER.Infrastructure.Repositories.Revit;
using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Application 관련 서비스 등록
        services.AddTransient<CountGenericModelUseCase>();


        // Infrastructure 관련 서비스 등록 (공통 인프라 서비스)
        // 예: 로깅, 데이터베이스 컨텍스트 등

        services.AddSingleton<IGenericModelRepository, GenericModelRepository>();

        return services;
    }
}
