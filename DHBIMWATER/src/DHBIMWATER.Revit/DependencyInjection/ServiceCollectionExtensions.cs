using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.Revit.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRevitServices(this IServiceCollection services)
    {
        // Revit 관련 서비스 등록
        // 예: services.AddSingleton<IRevitService, RevitService>();

        return services;
    }
}
