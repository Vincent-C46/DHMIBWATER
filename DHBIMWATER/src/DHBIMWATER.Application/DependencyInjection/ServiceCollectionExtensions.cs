using DHBIMWATER.Application.Interface;
using DHBIMWATER.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application 관련 서비스 등록
        services.AddTransient<CountGenericModelUseCase>();

        return services;
    }
}
