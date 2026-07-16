using DHBIMWATER.UI.ViewModels.Modeling;
using System.Windows;
using System.Windows.Input;

namespace DHBIMWATER.UI.Views.Modeling;

public partial class PipeLayoutView : Window
{
    private readonly PipeLayoutViewModel _viewModel;
    public PipeLayoutView(PipeLayoutViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; DataContext = viewModel; viewModel.CloseAction = Close; }
    private void OnLayoutCanvasSizeChanged(object sender, SizeChangedEventArgs e) => _viewModel.HandleCanvasSizeChanged(e.NewSize.Width, e.NewSize.Height);
    private void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => _viewModel.HandleCanvasClick(e.GetPosition(LayoutCanvas));
    private void OnCanvasMouseMove(object sender, MouseEventArgs e) => _viewModel.HandleCanvasMove(e.GetPosition(LayoutCanvas));
    private void OnEdgeMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is PipeEdgeItem edge) _viewModel.SelectEdge(edge.Id);
        e.Handled = true;
    }
}
