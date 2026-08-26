using System.Windows;
using DHBIMWATER.UI.ViewModels.Modeling;
namespace DHBIMWATER.UI.Views.Modeling;
public partial class ExcelAlignmentMappingView : Window
{
    public ExcelAlignmentMappingView(ExcelAlignmentMappingViewModel viewModel) { InitializeComponent(); DataContext = viewModel; viewModel.CloseAction = Close; }
}
