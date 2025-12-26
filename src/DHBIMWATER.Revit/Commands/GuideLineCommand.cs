using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.Views.GuideLine;

namespace DHBIMWATER.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class GuideLineCommand : CommandBase
    {
        protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var view = ServiceContainer.GetService<GuideLineView>();
            view.ShowDialog();
            return Result.Succeeded;
        }
    }
}
