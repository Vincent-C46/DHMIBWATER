using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System.Windows;
using System;
using DHBIMWATER.Application.UseCases.QuantityCalculator;
using DHBIMWATER.UI.ViewModels.Quantity;

namespace DHBIMWATER.Revit.Commands.Quantity
{
    public class QuantityRequestHandler : IExternalEventHandler
    {
        private readonly CalculateQuantityUseCase _useCase;
        private readonly VisualizeNetFacesUseCase _visualizeNetFacesUseCase;
        private readonly QuantityViewModel _vm;

        private readonly QuantityRequest _quantityRequest = new QuantityRequest();
        public QuantityRequest QuantityRequest { get { return _quantityRequest; } }

        public IList<long> ElementIdsToSelect { get; set; } = new List<long>();
        public IList<long> ElementIdsToVisualize { get; set; } = new List<long>();

        public QuantityRequestHandler(
            CalculateQuantityUseCase useCase,
            VisualizeNetFacesUseCase visualizeNetFacesUseCase,
            QuantityViewModel vm)
        {
            _useCase = useCase;
            _visualizeNetFacesUseCase = visualizeNetFacesUseCase;
            _vm = vm;
        }

        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Document doc = uidoc.Document;

            try
            {
                switch (QuantityRequest.Take())
                {
                    case QuantityRequestId.None:
                        break;
                    case QuantityRequestId.Calculate:
                        var items = _useCase.Execute();
                        System.Windows.Application.Current.Dispatcher.Invoke(() => _vm.ApplyCalculatedItems(items.ToList()));
                        break;
                    case QuantityRequestId.SelectInRevit:
                        var elementIds = ElementIdsToSelect
                                        .Select(id => new ElementId(id))
                                        .Where(id => doc.GetElement(id) != null)
                                        .ToList();
                        uidoc.Selection.SetElementIds(elementIds);
                        break;
                    case QuantityRequestId.VisualizeNetFace:
                        var count = _visualizeNetFacesUseCase.Execute(ElementIdsToVisualize.ToList());
                        TaskDialog.Show("순 면적 시각화", $"순 면적 시각화 {count}개 생성");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Requset 요청 실패\n{ex.Message}");
            }
        }

        public string GetName() => "QuantityRequest";
    }
}
