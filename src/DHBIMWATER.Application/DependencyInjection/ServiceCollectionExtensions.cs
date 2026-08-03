using DHBIMWATER.Application.UseCases;
using DHBIMWATER.Application.Gis;
using DHBIMWATER.Application.UseCases.AutoGenerator;
using DHBIMWATER.Application.UseCases.Gis;
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
        services.AddTransient<CreateValvePipingUseCase>();
        services.AddTransient<CreateValveRoomUseCase>();
        services.AddTransient<ClassifyExteriorWallsUseCase>();
        services.AddTransient<AlignmentSourceLoader>();
        services.AddTransient<ModelPipeAlignmentUseCase>();
        services.AddTransient<AnalyzePipeNetworkUseCase>();
        services.AddTransient<SaveBendSettingsUseCase>();

        return services;
    }
}
