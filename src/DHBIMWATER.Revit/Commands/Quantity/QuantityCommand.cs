using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.UseCases.QuantityCalculator;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.Views.Quantity;
using System.Windows.Interop;

namespace DHBIMWATER.Revit.Commands.Quantity
{
    [Transaction(TransactionMode.Manual)]
    public class QuantityCommand : CommandBase
    {
        private QuantityView? _view;

        protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _view = ServiceContainer.GetService<QuantityView>();

            if (_view != null && _view.IsVisible)
            {
                _view.Activate();  // 이미 열려있으면 앞으로 가져오기만
                return Result.Succeeded;
            }

            new WindowInteropHelper(_view).Owner = commandData.Application.MainWindowHandle;
            var useCase = ServiceContainer.GetService<CalculateQuantityUseCase>();
            var handler = new QuantityRequestHandler(useCase, _view.ViewModel);
            var exEvent = ExternalEvent.Create(handler);

            _view.ViewModel.SetExtractAction(() =>
            {
                handler.QuantityRequest.Make(QuantityRequestId.Calculate);
                exEvent.Raise();
            });

            _view.ViewModel.SetSelectAction(ids =>
            {
                handler.ElementIdsToSelect = ids;
                handler.QuantityRequest.Make(QuantityRequestId.SelectInRevit);
                exEvent.Raise();
            });

            _view.Show();
            return Result.Succeeded;
        }
    }
}
