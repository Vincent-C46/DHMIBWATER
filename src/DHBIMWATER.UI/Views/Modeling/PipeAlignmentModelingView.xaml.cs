using System.Windows;
using DHBIMWATER.UI.ViewModels.Modeling;
namespace DHBIMWATER.UI.Views.Modeling;
public partial class PipeAlignmentModelingView : Window
{
    public PipeAlignmentModelingView(PipeAlignmentModelingViewModel viewModel) { InitializeComponent(); DataContext = viewModel; viewModel.CloseAction = Close; }
}
