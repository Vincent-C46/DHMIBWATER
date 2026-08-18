using System.Windows;
using DHBIMWATER.UI.ViewModels.Modeling;
namespace DHBIMWATER.UI.Views.Modeling;
public partial class PipeSpecTableView : Window
{
    public PipeSpecTableView(PipeSpecTableViewModel viewModel) { InitializeComponent(); DataContext = viewModel; viewModel.CloseAction = Close; }
}
