using DHBIMWATER.UI.ViewModels.Modeling;
using System.Windows;
using System.Windows.Input;

namespace DHBIMWATER.UI.Views.Modeling;

public partial class PipeLayoutView : Window
{
    private readonly PipeLayoutViewModel _viewModel;
    public PipeLayoutView(PipeLayoutViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; DataContext = viewModel; viewModel.CloseAction = Close; }
    private void OnLayoutCanvasSizeChanged(object sender, SizeChangedEventArgs e) => _viewModel.HandleCanvasSizeChanged(e.NewSize.Width, e.NewSize.Height);
    private void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { LayoutCanvas.Focus(); _viewModel.HandleCanvasClick(e.GetPosition(LayoutCanvas)); }
    private void OnCanvasMouseMove(object sender, MouseEventArgs e) => _viewModel.HandleCanvasMove(e.GetPosition(LayoutCanvas));
    private void OnEdgeMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var position = e.GetPosition(LayoutCanvas);
        if (_viewModel.IsDrawing || _viewModel.IsPlacingFitting) _viewModel.HandleCanvasClick(position);
        else if (((FrameworkElement)sender).DataContext is PipeEdgeItem edge) _viewModel.SelectEdge(edge.Id);
        LayoutCanvas.Focus();
        e.Handled = true;
    }
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.OriginalSource is System.Windows.Controls.TextBox or System.Windows.Controls.ComboBox) return;
        if (e.Key == Key.Z && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            if (_viewModel.UndoCommand.CanExecute(null)) _viewModel.UndoCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape) { _viewModel.HandleEscape(); e.Handled = true; }
        else if (e.Key == Key.Delete) { _viewModel.DeleteSelectedEdge(); e.Handled = true; }
    }

    private void OnValidationError(object sender, System.Windows.Controls.ValidationErrorEventArgs e)
        => _viewModel.UpdateInputValidation(e.Action == System.Windows.Controls.ValidationErrorEventAction.Added);
}
