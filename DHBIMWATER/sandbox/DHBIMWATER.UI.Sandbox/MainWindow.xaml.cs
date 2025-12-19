using Fluent;

namespace DHBIMWATER.UI.Sandbox;

public partial class MainWindow : RibbonWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
