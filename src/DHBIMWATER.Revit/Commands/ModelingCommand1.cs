using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.Views.Modeling;

namespace DHBIMWATER.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ModelingCommand1 : CommandBase
    {
        protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // ServiceContainer에서 View 인스턴스 가져오기
            var view = ServiceContainer.GetService<Modeling1View>();
            view.ShowDialog();
            return Result.Succeeded;
        }
    }
}
