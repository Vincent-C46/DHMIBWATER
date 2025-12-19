using DHBIMWATER.Core.Interfaces.Services.Modeling;
using DHBIMWATER.UI.DependencyInjection;
using DHBIMWATER.UI.Sandbox.Services.Modeling;
using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.UI.Sandbox.DependencyInjection;

/// <summary>
/// Sandbox Mock 서비스 등록
/// </summary>
public static class SandboxServiceRegistration
{
    public static IServiceCollection AddSandboxServices(this IServiceCollection services)
    {
        // UI Services 등록
        services.AddUIServices();

        // ViewModels 등록
        services.AddTransient<MainViewModel>();

        // Views 등록
        services.AddTransient<MainWindow>();

        // Mock 서비스 등록
        services.AddSingleton<IReservoirModelingService, MockReservoirModelingService>();

        return services;
    }
}
