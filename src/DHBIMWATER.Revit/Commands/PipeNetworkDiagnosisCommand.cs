using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.Views.Modeling;
using System.Windows.Interop;

namespace DHBIMWATER.Revit.Commands;

/// <summary>
/// 관로 네트워크 진단 창. 진단 실행과 설정 저장은 창 안에서 ViewModel이 UseCase를 직접 호출한다
/// (결과를 같은 창에 표시해야 하므로 창을 닫은 뒤 실행하는 다른 커맨드와 형태가 다르다).
/// </summary>
[Transaction(TransactionMode.Manual)]
public class PipeNetworkDiagnosisCommand : CommandBase
{
    protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var view = ServiceContainer.GetService<PipeNetworkDiagnosisView>();
        new WindowInteropHelper(view).Owner = commandData.Application.MainWindowHandle;
        view.ShowDialog();
        return Result.Succeeded;
    }
}
