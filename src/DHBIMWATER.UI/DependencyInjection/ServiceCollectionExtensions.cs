using DHBIMWATER.UI.ViewModels.GuideLine;
using DHBIMWATER.UI.ViewModels.Modeling;
using DHBIMWATER.UI.ViewModels.Utilities;
using DHBIMWATER.UI.Views.GuideLine;
using DHBIMWATER.UI.Views.Modeling;
using DHBIMWATER.UI.Views.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.UI.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        // View 등록
        services.AddTransient<WaterTankView>();
        services.AddTransient<PumpingStationView>();
        services.AddTransient<Modeling1View>();
        services.AddTransient<GuideLineView>();
        services.AddTransient<ExParamsView>();

        // ViewModel 등록
        services.AddTransient<WaterTankViewModel>();
        services.AddTransient<PumpingStationViewModel>();
        services.AddTransient<Modeling1ViewModel>();
        services.AddTransient<GuideLineViewModel>();
        services.AddTransient<ExParamsViewModel>();

        return services;
    }
}
