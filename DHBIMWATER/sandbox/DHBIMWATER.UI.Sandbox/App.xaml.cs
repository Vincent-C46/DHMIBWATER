using DHBIMWATER.UI.Sandbox.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DHBIMWATER.UI.Sandbox;

public partial class App : System.Windows.Application
{
    private IServiceProvider _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // DI 컨테이너 설정
        var services = new ServiceCollection();
        services.AddSandboxServices();
        _serviceProvider = services.BuildServiceProvider();

        // MainWindow를 DI로 생성하여 표시
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}

