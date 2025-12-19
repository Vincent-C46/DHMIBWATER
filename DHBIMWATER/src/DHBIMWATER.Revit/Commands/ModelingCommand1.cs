using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.UI.Views.Modeling;

namespace DHBIMWATER.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ModelingCommand1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // ServiceLocator를 통해 View 가져오기 및 DataContext 설정
            var modeling1view = ServiceLocator.GetService<Modeling1View>();
            modeling1view.ShowDialog();

            return Result.Succeeded;
        }
    }
}
