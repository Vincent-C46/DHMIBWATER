using System.Windows;
using DHBIMWATER.UI.ViewModels.Modeling;
namespace DHBIMWATER.UI.Views.Modeling;
public partial class JointDeflectionSettingsView : Window
{
    public JointDeflectionSettingsView(JointDeflectionSettingsViewModel viewModel) { InitializeComponent(); DataContext = viewModel; viewModel.CloseAction = Close; }
}
