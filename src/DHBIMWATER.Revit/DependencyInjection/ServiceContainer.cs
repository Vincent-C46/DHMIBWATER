using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.DependencyInjection;
using DHBIMWATER.Infrastructure.DependencyInjection;
using DHBIMWATER.UI.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.Revit.DependencyInjection
{
    public class ServiceContainer
    {
        private static IServiceProvider? _serviceProvider;
        public static IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("ServiceContainer is not built.");

        internal static void Build(UIApplication uiApp)
        {
            Dispose();

            ServiceCollection services = new ServiceCollection();
            services.AddSingleton<Func<IntPtr>>(() => uiApp.MainWindowHandle);

            // 현재 활성 문서를 가져오는 람다 메서드를 서비스로 등록 - 다른 서비스에서 주입하여 호출할 때 현재 문서를 반환
            services.AddSingleton<Func<Document?>>(() => uiApp.ActiveUIDocument?.Document);

            // Revit 관련 서비스 등록
            services.AddUIServices();                   // UI View/ViewModel
            services.AddRevitServices();                // Revit 관련 서비스
            services.AddApplicationServices();          // Application 서비스
            services.AddInfrastructureServices();       // Infrastructure 서비스

            _serviceProvider = services.BuildServiceProvider();
        }

        public static void Dispose()
        {
            if (_serviceProvider is IDisposable d)
                d.Dispose();

            _serviceProvider = null;
        }

        // notnull 제약 조건을 사용하여 T가 null이 될 수 없음을 보장
        public static T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }
    }
}
