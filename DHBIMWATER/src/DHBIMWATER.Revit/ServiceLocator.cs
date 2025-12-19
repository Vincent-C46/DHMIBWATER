using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.Revit
{
    internal class ServiceLocator
    {
        private static IServiceProvider? _serviceProvider;

        internal static IServiceProvider ServiceProvider
        {
            get => _serviceProvider ?? throw new InvalidOperationException("ServiceProvider is not initialized.");
            set => _serviceProvider = value;
        }

        // notnull 제약 조건을 사용하여 T가 null이 될 수 없음을 보장
        public static T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }
    }
}
