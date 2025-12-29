using DHBIMWATER.UI.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.UI.Sandbox.DependencyInjection;

public static class SandboxServiceRegistration
{
    public static IServiceCollection AddSandboxServices(this IServiceCollection services)
    {
        // ViewModels 등록
        services.AddTransient<MainViewModel>();

        // Views 등록
        services.AddTransient<MainWindow>();

        // Mock 서비스 등록

        return services;
    }
}
