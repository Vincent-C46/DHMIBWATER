using DHBIMWATER.UI.ViewModels.Modeling;
using System.Windows;
using System.Windows.Input;

namespace DHBIMWATER.UI.Views.Modeling;

public partial class PipeLayoutView : Window
{
    private readonly PipeLayoutViewModel _viewModel;
    private bool _isPanning;
    private Point _lastPanPosition;
    public PipeLayoutView(PipeLayoutViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; DataContext = viewModel; viewModel.CloseAction = Close; }
    private void OnLayoutCanvasSizeChanged(object sender, SizeChangedEventArgs e) => _viewModel.HandleCanvasSizeChanged(e.NewSize.Width, e.NewSize.Height);
    private void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { LayoutCanvas.Focus(); _viewModel.HandleCanvasClick(e.GetPosition(LayoutCanvas)); }
    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        var position = e.GetPosition(LayoutCanvas);
        if (_isPanning)
        {
            if (e.MiddleButton != MouseButtonState.Pressed)
            {
                EndPan();
                return;
            }
            _viewModel.HandleCanvasPan(position.X - _lastPanPosition.X, position.Y - _lastPanPosition.Y);
            _lastPanPosition = position;
            return;
        }
        _viewModel.HandleCanvasMove(position);
    }
    private void OnCanvasMouseLeave(object sender, MouseEventArgs e) => _viewModel.HandleCanvasLeave();
    private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _viewModel.HandleCanvasZoom(e.GetPosition(LayoutCanvas), e.Delta);
        e.Handled = true;
    }
    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;
        _isPanning = true;
        _lastPanPosition = e.GetPosition(LayoutCanvas);
        Mouse.Capture(LayoutCanvas);
        e.Handled = true;
    }
    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;
        EndPan();
        e.Handled = true;
    }
    private void EndPan()
    {
        _isPanning = false;
        if (Mouse.Captured == LayoutCanvas) Mouse.Capture(null);
    }
    private void OnEdgeMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 선택 여부는 "지금 그리는 중인가"가 아니라 모드로 판정한다.
        // 그리기 모드의 선 클릭은 T 접점 생성을 위해 캔버스 클릭으로 그대로 넘긴다.
        if (_viewModel.IsSelectionMode)
        {
            if (((FrameworkElement)sender).DataContext is PipeEdgeItem edge) _viewModel.SelectEdge(edge.Id);
        }
        else _viewModel.HandleCanvasClick(e.GetPosition(LayoutCanvas));
        LayoutCanvas.Focus();
        e.Handled = true;
    }
    private void OnFittingMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is InlineFittingItem fitting) _viewModel.SelectFitting(fitting.Id);
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
        else if (e.Key == Key.Delete)
        {
            if (_viewModel.DeleteSelectedFittingCommand.CanExecute(null)) _viewModel.DeleteSelectedFittingCommand.Execute(null);
            else _viewModel.DeleteSelectedEdge();
            e.Handled = true;
        }
    }

    private void OnValidationError(object sender, System.Windows.Controls.ValidationErrorEventArgs e)
        => _viewModel.UpdateInputValidation(e.Action == System.Windows.Controls.ValidationErrorEventAction.Added);
}
