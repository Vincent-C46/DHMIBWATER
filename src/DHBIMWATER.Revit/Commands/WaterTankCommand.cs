using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.Views.Modeling;
using System.Windows.Interop;

namespace DHBIMWATER.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class WaterTankCommand : CommandBase
    {
        protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var view = ServiceContainer.GetService<WaterTankView>();
            new WindowInteropHelper(view).Owner = commandData.Application.MainWindowHandle;
            view.ShowDialog();
            return Result.Succeeded;
        }
    }
}
