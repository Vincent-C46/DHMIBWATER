using DHBIMWATER.UI.ViewModels.Modeling;
using DHBIMWATER.UI.Views.Modeling;
using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.UI.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        // View 등록
        services.AddTransient<Modeling1View>();

        // ViewModel 등록
        services.AddTransient<Modeling1ViewModel>();

        return services;
    }
}
