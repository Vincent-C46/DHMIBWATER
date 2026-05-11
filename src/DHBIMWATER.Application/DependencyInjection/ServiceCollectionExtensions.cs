using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases;
using DHBIMWATER.Application.UseCases.Parameter;
using DHBIMWATER.Application.UseCases.Sheets;
using Microsoft.Extensions.DependencyInjection;



namespace DHBIMWATER.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application 관련 서비스 등록
        services.AddTransient<CountGenericModelUseCase>();
        services.AddTransient<IExportParamsUseCase, ExportParamsUseCase>();
        services.AddTransient<IImportParamsUseCase, ImportParamsUseCase>();
        services.AddTransient<IWaterReservoirUseCase, WaterReservoirUseCase>();
   



        return services;
    }
}
