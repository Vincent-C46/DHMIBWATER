using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.DependencyInjection;
using System.Windows;
using System.Windows.Interop;

namespace DHBIMWATER.Revit.Commands;

public abstract class ModelessCommandBase : IExternalCommand
{
    protected virtual string DialogOwnerName => "DHBIMWATER";

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        if (ModelessWindowGate.IsOpen)
        {
            ModelessWindowGate.ActivateOpen();
            TaskDialog.Show(DialogOwnerName, $"'{ModelessWindowGate.OpenTitle}' 창이 이미 열려 있습니다.\n먼저 닫은 뒤 다시 실행하세요.");
            return Result.Cancelled;
        }

        ServiceContainer.Build(commandData.Application);
        try
        {
            return ExecuteInternal(commandData, ref message, elements);
        }
        catch (Exception ex)
        {
            ServiceContainer.Dispose();
            message = ex.Message;
            return Result.Failed;
        }
    }

    protected static void AttachModelessWindow(Window view, IntPtr revitHandle)
    {
        new WindowInteropHelper(view).Owner = revitHandle;
        ModelessWindowGate.Open(view);
        view.Closed += (_, _) => ModelessWindowGate.Close(view);
    }

    protected abstract Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements);
}
