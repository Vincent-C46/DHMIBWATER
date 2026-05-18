using DHBIMWATER.UI.ViewModels.GuideLine;
using DHBIMWATER.UI.ViewModels.Modeling;
using DHBIMWATER.UI.ViewModels.Quantity;
using DHBIMWATER.UI.ViewModels.Utilities;
using DHBIMWATER.UI.Views.GuideLine;
using DHBIMWATER.UI.Views.Modeling;
using DHBIMWATER.UI.Views.Quantity;
using DHBIMWATER.UI.Views.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.UI.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        #region View 등록
        // Sample
        services.AddTransient<Modeling1View>();
        services.AddTransient<GuideLineView>();

        // Modeling
        services.AddTransient<WaterTankView>();
        services.AddTransient<PumpingStationView>();

        // Quantity
        services.AddTransient<QuantityCalculationView>();

        // Utilities
        services.AddTransient<ExParamsView>();
        #endregion

        #region ViewModel 등록
        // Sample
        services.AddTransient<Modeling1ViewModel>();
        services.AddTransient<GuideLineViewModel>();

        // Modeling
        services.AddTransient<WaterTankViewModel>();
        services.AddTransient<PumpingStationViewModel>();

        // Quantity
        services.AddTransient<QuantityCalculationViewModel>();

        // Utilities
        services.AddTransient<ExParamsViewModel>();
        #endregion

        return services;
    }
}
