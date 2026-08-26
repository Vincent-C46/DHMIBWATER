using DHBIMWATER.UI.ViewModels.GuideLine;
using DHBIMWATER.UI.ViewModels.Modeling;
using DHBIMWATER.UI.ViewModels.Utilities;
using DHBIMWATER.UI.Views.GuideLine;
using DHBIMWATER.UI.Views.Modeling;
using DHBIMWATER.UI.Views.Utilities;
using DHBIMWATER.Infrastructure.Services.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.UI.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        #region View ���
        // Sample
        services.AddTransient<Modeling1View>();
        services.AddTransient<GuideLineView>();

        // Modeling
        services.AddTransient<WaterTankView>();
        services.AddTransient<PumpingStationView>();
        services.AddTransient<ValveRoomView>();
        services.AddTransient<PipeLayoutView>();
        services.AddTransient<PipeAlignmentModelingView>();

        // Utilities
        services.AddTransient<ExParamsView>();
        #endregion

        #region ViewModel ���
        // Sample
        services.AddTransient<Modeling1ViewModel>();
        services.AddTransient<GuideLineViewModel>();

        // Modeling
        services.AddTransient<WaterTankViewModel>();
        services.AddTransient<PumpingStationViewModel>();
        services.AddTransient<ValveRoomViewModel>();
        services.AddTransient<PipeLayoutViewModel>();
        // 이 모델리스 ViewModel만 전용 STA 스레드에서 안전한 WPF 다이얼로그를 사용한다.
        services.AddTransient(sp => ActivatorUtilities.CreateInstance<PipeAlignmentModelingViewModel>(sp, new WpfDialogService()));

        // Utilities
        services.AddTransient<ExParamsViewModel>();
        #endregion

        return services;
    }
}
