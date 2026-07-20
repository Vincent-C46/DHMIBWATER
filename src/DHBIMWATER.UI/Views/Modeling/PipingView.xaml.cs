using System.Windows;
using DHBIMWATER.UI.ViewModels.Modeling;

namespace DHBIMWATER.UI.Views.Modeling;
public partial class PipingView : Window
{
    public PipingView(PipingViewModel viewModel) { InitializeComponent(); DataContext = viewModel; viewModel.CloseAction = Close; }
}
