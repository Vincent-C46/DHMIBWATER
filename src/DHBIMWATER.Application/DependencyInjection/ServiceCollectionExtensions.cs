using DHBIMWATER.Application.UseCases;
using DHBIMWATER.Application.UseCases.AutoGenerator;
using DHBIMWATER.Application.UseCases.QuantityCalculator;
using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application 관련 서비스 등록
        services.AddTransient<CountGenericModelUseCase>();
        services.AddTransient<CreateReservoirUseCase>();
        services.AddTransient<CreatePumpingStationUseCase>();
        services.AddTransient<CalculateQuantityUseCase>();
        services.AddTransient<ExportQuantityUseCase>();

        return services;
    }
}