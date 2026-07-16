using DHBIMWATER.UI.ViewModels.Modeling;
using System.Windows;

namespace DHBIMWATER.UI.Views.Modeling;

public partial class ValveRoomView : Window
{
    public ValveRoomView(ValveRoomViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseAction = Close;
    }
}