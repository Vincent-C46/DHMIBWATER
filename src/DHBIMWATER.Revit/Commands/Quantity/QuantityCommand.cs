using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.Views.Quantity;
using System.Windows.Interop;

namespace DHBIMWATER.Revit.Commands.Quantity
{
    [Transaction(TransactionMode.Manual)]
    public class QuantityCommand : CommandBase
    {
        protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var view = ServiceContainer.GetService<QuantityView>();
            new WindowInteropHelper(view).Owner = commandData.Application.MainWindowHandle;
                view.ShowDialog();
            return Result.Succeeded;
        }
    }
}
