using DHBIMWATER.UI.ViewModels.Modeling;
using System.Windows;

namespace DHBIMWATER.UI.Views.Modeling;

public partial class ValveRoomPreviewView : Window
{
    public ValveRoomPreviewView(ValveRoomPreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseAction = Close;
    }
}
