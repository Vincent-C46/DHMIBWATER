using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DHBIMWATER.Application.UseCases.Parameter;
using DHBIMWATER.Application.Interfaces.Parameter;
using DHBIMWATER.Infrastructure.Services.Revit.Parameter;


namespace DHBIMWATER.Revit.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRevitServices(this IServiceCollection services)
    {
        // Revit 관련 서비스 등록
        // 예: services.AddSingleton<IRevitService, RevitService>();
        services.AddTransient<IExportParamsUseCase, ExportParamsUseCase>();
        services.AddTransient<IImportParamsUseCase, ImportParamsUseCase>();

        services.AddTransient<IExportParamsGateway, RevitExportParamsGateway>();
        services.AddTransient<IImportParamsGateway, RevitImportParamsGateway>();

        return services;
    }
}
