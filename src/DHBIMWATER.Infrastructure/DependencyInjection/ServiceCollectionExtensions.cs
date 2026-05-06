using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases;
using DHBIMWATER.Infrastructure.Repositories.Mock;
using DHBIMWATER.Infrastructure.Repositories.Revit;
using DHBIMWATER.Infrastructure.Services.Mock;
using DHBIMWATER.Infrastructure.Services.Revit;
using DHBIMWATER.Infrastructure.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace DHBIMWATER.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Infrastructure 서비스 등록 (Revit 환경용)
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Revit 실제 구현 등록
        services.AddSingleton<IGenericModelRepository, RevitGenericModelRepository>();
        services.AddTransient<ITransactionContext, RevitTransactionContext>();

        #region Element 관련
        services.AddTransient<ILevelQueryRepo, RevitLevelQueryRepo>();
        services.AddTransient<ILevelCommandRepo, RevitLevelCommandRepo>();
        services.AddTransient<IElementTypeQueryRepo, RevitElementTypeQueryRepo>();
        services.AddTransient<IElementTypeCommandRepo, RevitElementTypeCommandRepo>();
        services.AddTransient<IWallCommandRepo, RevitWallCommandRepo>();
        services.AddTransient<IBeamCommandRepo, RevitBeamCommandRepo>();
        services.AddTransient<ISlabCommandRepo, RevitSlabCommandRepo>();
        services.AddTransient<IOpeningCommandRepo, RevitOpeningCommandRepo>();
        services.AddTransient<IDirectShapeCommandRepo, RevitDirectShapeCommandRepo>();
        #endregion

        services.AddTransient<IFileDialogService, RevitFileDialogService>();
        services.AddTransient<IDialogService, RevitDialogService>();
        services.AddTransient<IGuideLineService, RevitGuideLineService>();

        return services;
    }

    /// <summary>
    /// Mock Infrastructure 서비스 등록 (Sandbox/테스트 환경용)
    /// </summary>
    public static IServiceCollection AddMockInfrastructureServices(this IServiceCollection services)
    {
        // Mock 구현 등록 (Revit 없이 동작)
        services.AddSingleton<IGenericModelRepository, MockGenericModelRepository>();
        services.AddTransient<ITransactionContext, MockTransactionContext>();

        #region Element 관련
        services.AddTransient<ILevelQueryRepo, MockLevelQueryRepo>();
        services.AddTransient<ILevelCommandRepo, MockLevelCommandRepo>();
        services.AddTransient<IElementTypeQueryRepo, MockElementTypeQueryRepo>();
        services.AddTransient<IElementTypeCommandRepo, MockElementTypeCommandRepo>();
        services.AddTransient<IWallCommandRepo, MockWallCommandRepo>();
        services.AddTransient<IBeamCommandRepo, MockBeamCommandRepo>();
        services.AddTransient<ISlabCommandRepo, MockSlabCommandRepo>();
        services.AddTransient<IOpeningCommandRepo, MockOpeningCommandRepo>();
        services.AddTransient<IDirectShapeCommandRepo, MockDirectShapeCommandRepo>();
        #endregion

        services.AddTransient<IFileDialogService, MockFileDialogService>();
        services.AddTransient<IDialogService, MockDialogService>();
        services.AddTransient<IGuideLineService, MockGuideLineService>();

        return services;
    }
}