using Autodesk.Revit.UI;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.Revit.UI;
using DHBIMWATER.UI.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DHBIMWATER.Revit
{
    public class App : IExternalApplication
    {
        // Static constructor로 AssemblyResolve 이벤트를 가장 먼저 등록
        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string? assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string? assemblyName = new AssemblyName(args.Name).Name;
            string assemblyPath = string.Empty;

            if (assemblyFolder != null)
                assemblyPath = Path.Combine(assemblyFolder, assemblyName + ".dll");

            if (File.Exists(assemblyPath))
                return Assembly.LoadFrom(assemblyPath);

            return null;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            // DI 컨테이너 초기화 및 서비스 등록
            var services = new ServiceCollection();
            services.AddRevitServices();  // Revit 관련 서비스
            services.AddUIServices();      // UI View/ViewModel

            ServiceLocator.ServiceProvider = services.BuildServiceProvider();

            // Revit UI 등록
            RibbonBuilder.CreateRibbonPanel(application);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // 종료 시 정리 작업
            return Result.Succeeded;
        }

    }
}
