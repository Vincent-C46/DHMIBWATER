using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Application.Interfaces.Settings;
using DHBIMWATER.Application.Services;
using DHBIMWATER.Application.Interfaces.Storage;
using DHBIMWATER.Core.Parameters;
using DHBIMWATER.Infrastructure.Repositories.DB;
using DHBIMWATER.Infrastructure.Repositories.FileSystem;
using DHBIMWATER.Infrastructure.Repositories.Gis;
using DHBIMWATER.Infrastructure.Repositories.Mock;
using DHBIMWATER.Infrastructure.Repositories.Mock.Quantity;
using DHBIMWATER.Infrastructure.Repositories.Revit.Storage;
using DHBIMWATER.Infrastructure.Repositories.Revit.Modeling;
using DHBIMWATER.Infrastructure.Repositories.Revit.Geometry;
using DHBIMWATER.Infrastructure.Repositories.Revit.Quantity;
using DHBIMWATER.Infrastructure.Repositories.Revit.Piping;
using DHBIMWATER.Infrastructure.Repositories.Revit;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Infrastructure.Services.Common;
using DHBIMWATER.Infrastructure.Services.Didas;
using DHBIMWATER.Infrastructure.Services.Excel;
using DHBIMWATER.Infrastructure.Services.Mock;
using DHBIMWATER.Infrastructure.Services.Revit;
using DHBIMWATER.Infrastructure.Services.Revit.Parameter;
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
        services.AddTransient<IProjectLocationCommandRepo, RevitProjectLocationCommandRepo>();
        services.AddTransient<IGenericModelCommandRepo, RevitGenericModelCommandRepo>();
        services.AddTransient<IPipeCommandRepo, RevitPipeMepCommandRepo>();
        services.AddTransient<IPipeCommandRepo, RevitPipeGenericModelCommandRepo>();
        services.AddTransient<IBeamCommandRepo, RevitBeamCommandRepo>();
        services.AddTransient<IColumnCommandRepo, RevitColumnCommandRepo>();
        services.AddTransient<ISlabCommandRepo, RevitSlabCommandRepo>();
        services.AddTransient<IOpeningCommandRepo, RevitOpeningCommandRepo>();
        services.AddTransient<IStairCommandRepo, RevitStairCommandRepo>();
        services.AddTransient<IDirectShapeCommandRepo, RevitDirectShapeCommandRepo>();
        services.AddTransient<IViewCommandRepo, RevitViewCommandRepo>();
        services.AddTransient<ISetParameterRepo, RevitSetParameterRepo>();
        services.AddTransient<ISharedParameterRepository, RevitSharedParameterRepository>();
        services.AddTransient<IPipeAlignmentCommandRepo, RevitPipeAlignmentCommandRepo>();
        services.AddTransient<IPipeAlignmentQueryRepo, RevitPipeAlignmentQueryRepo>();
        services.AddTransient<IShapefileReader, ShapefileReader>();
        services.AddTransient<IAlignmentSourceReader, ShapefileReader>();
        services.AddTransient<IGenericModelRepository, RevitGenericModelRepository>();
        services.AddTransient<IIntersectingElementFinder, RevitIntersectingElementFinder>();
        services.AddTransient<IExteriorWallClassifierRepo, RevitExteriorWallClassifierRepo>();
        #endregion

        #region Quantity 관련
        // IQuantityExtractor 경로 (Rule Engine 미적용 카테고리)
        services.AddTransient<IQuantityExtractor, RevitGenericModelExtractor>();
        services.AddTransient<IQuantityExtractor, RevitStairsExtractor>();
        services.AddTransient<IQuantityExtractor, RevitRailingExtractor>();
        services.AddTransient<IQuantityExtractor, RevitDirectShapeExtractor>();

        // IElementMeasurementExtractor + Rule Engine 경로
        services.AddTransient<IElementMeasurementExtractor, RevitWallMeasurementExtractor>();
        services.AddTransient<IElementMeasurementExtractor, RevitColumnMeasurementExtractor>();
        services.AddTransient<IElementMeasurementExtractor, RevitBeamMeasurementExtractor>();
        services.AddTransient<IElementMeasurementExtractor, RevitFloorMeasurementExtractor>();
        services.AddTransient<IElementMeasurementExtractor, RevitFoundationMeasurementExtractor>();
        services.AddTransient<IElementMeasurementExtractor, RevitRebarMeasurementExtractor>();
        services.AddSingleton<QuantityRuleEngine>();

        services.AddTransient<IQuantityRuleRepository, RevitQuantityRuleRepo>();
        services.AddTransient<IFaceClassifier, RevitFaceClassifier>();
        services.AddTransient<IExcelExporter, ClosedXmlExcelWriter>();
        services.AddTransient<IElementQuantityRepo, ElementQuantityRepo>();
        services.AddTransient<IManualQuantityRepo, ManualQuantityRepo>();
        services.AddTransient<IQuantitySettingsRepository, RevitQuantitySettingsRepo>();
        services.AddTransient<IExcelReader, ExcelReader>();
        services.AddTransient<IProjectSettingsRepository, DhcfgRepo>();
        #endregion
        #region Service 등록
        services.AddTransient<IFileDialogService, WpfFileDialogService>();
        services.AddTransient<IDialogService, RevitDialogService>();
        services.AddTransient<IGuideLineService, RevitGuideLineService>();
        services.AddSingleton<IUsageLogger, DidasUsageService>();   // Didas 로그 연계
        #endregion
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
        services.AddTransient<IGenericModelCommandRepo, MockGenericModelCommandRepo>();
        services.AddTransient<IBeamCommandRepo, MockBeamCommandRepo>();
        services.AddTransient<ISlabCommandRepo, MockSlabCommandRepo>();
        services.AddTransient<IOpeningCommandRepo, MockOpeningCommandRepo>();
        services.AddTransient<IDirectShapeCommandRepo, MockDirectShapeCommandRepo>();
        services.AddTransient<IViewCommandRepo, MockViewCommandRepo>();

        services.AddTransient<ISetParameterRepo, MockSetParameterRepo>();
        services.AddTransient<ISharedParameterRepository, MockSharedParameterRepository>();
        services.AddTransient<IShapefileReader, ShapefileReader>();
        services.AddTransient<IAlignmentSourceReader, ShapefileReader>();
        #endregion

        #region Quantity 관련
        services.AddTransient<IQuantityExtractor, MockWallExtractor>();
        services.AddTransient<IExcelExporter, ClosedXmlExcelWriter>();
        services.AddTransient<IExcelReader, ExcelReader>();
        #endregion


        #region Service 등록
        services.AddTransient<IFileDialogService, WpfFileDialogService>();
        services.AddTransient<IDialogService, MockDialogService>();
        services.AddTransient<IGuideLineService, MockGuideLineService>();
        services.AddSingleton<IUsageLogger, MockUsageLogger>();
        #endregion

        return services;
    }
}
