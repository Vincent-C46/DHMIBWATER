using System.Windows;
using DHBIMWATER.UI.ViewModels.Modeling;
namespace DHBIMWATER.UI.Views.Modeling;
public partial class PipeNetworkDiagnosisView : Window
{
    public PipeNetworkDiagnosisView(PipeNetworkDiagnosisViewModel vm) { InitializeComponent(); DataContext = vm; vm.CloseAction = Close; }
}
