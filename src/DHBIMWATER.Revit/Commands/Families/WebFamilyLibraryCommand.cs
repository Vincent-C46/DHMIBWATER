using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Infrastructure.Services.Revit.Families;
using DHBIMWATER.Infrastructure.Services.Revit;
using DHBIMWATER.Infrastructure.Services.Revit.Parameter;
using DHBIMWATER.UI.ViewModels.Documentation.Families;
using DHBIMWATER.UI.Views.Documentation.Families;
using System.Windows.Interop;

namespace DHBIMWATER.Revit.Commands.Families
{
    [Transaction(TransactionMode.Manual)]
    public class WebFamilyLibraryCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc?.Document;
            if (doc == null)
            {
                message = "현재 문서를 찾을 수 없습니다.";
                return Result.Failed;
            }

            IDialogService dialogService = new RevitDialogService();
            var service = new WebFamilyLibraryService(doc);
            var vm = new WebFamilyLibraryViewModel(service, dialogService);

            var view = new WebFamilyLibraryView { DataContext = vm };
            new WindowInteropHelper(view).Owner = commandData.Application.MainWindowHandle;
            view.ShowDialog();

            return Result.Succeeded;
        }
    }
}
