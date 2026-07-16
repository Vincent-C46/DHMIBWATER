using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using DHBIMWATER.UI.Utilities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling;

/// <summary>
/// 밸브실 입력값을 2D 평면 스키매틱으로 보여준다. Revit 없이 순수 좌표 계산만 수행하며,
/// 부재 산식은 <c>CreateValveRoomUseCase</c>의 평면 배치 로직과 동일하게 유지한다.
/// </summary>
public sealed class ValveRoomPreviewViewModel : ViewModelBase
{
    private const double CanvasWidth = 560;
    private const double CanvasHeight = 420;
    private const double MarginPx = 40;

    public ValveRoomPreviewViewModel(ValveRoomViewModel source)
    {
        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
        Rects = [];
        Lines = [];
        Columns = [];
        Build(source);
    }

    public ObservableCollection<PreviewRectItem> Rects { get; }
    public ObservableCollection<PreviewLineItem> Lines { get; }
    public ObservableCollection<PreviewPointItem> Columns { get; }
    public string DimensionSummary { get; private set; } = string.Empty;
    public ICommand CloseCommand { get; }
    public Action? CloseAction { get; set; }

    private void Build(ValveRoomViewModel r)
    {
        var isMud = r.SelectedValveRoomType == "이토밸브실";
        var isSluice = r.SelectedValveRoomType == "제수밸브실";

        var outerWidth = r.InnerWidth + r.OuterWallThickness * 2;
        var outerLength = r.InnerLength + r.OuterWallThickness * 2;
        var foundationWidth = outerWidth + r.FoundationToe * 2;
        var foundationLength = outerLength + r.FoundationToe * 2;

        var transform = BuildFitTransform(foundationWidth, foundationLength);

        Rects.Add(Rect(transform, foundationWidth, foundationLength, "#B0BEC5", "#ECEFF1", 1));
        Rects.Add(Rect(transform, outerWidth, outerLength, "#2C3E50", "Transparent", 2));
        Rects.Add(Rect(transform, r.InnerWidth, r.InnerLength, "#7F8C8D", "Transparent", 1, dashed: true));

        if (isMud && r.HasIntermediateWall)
        {
            for (var i = 1; i <= r.IntermediateWallCount; i++)
            {
                var x = -r.InnerWidth / 2 + r.InnerWidth * i / (r.IntermediateWallCount + 1d);
                Lines.Add(Line(transform, x, -r.InnerLength / 2, x, r.InnerLength / 2, "#2C3E50", 2));
            }
        }

        if (isSluice)
        {
            var x0 = -r.InnerWidth / 2; var x1 = r.InnerWidth / 2;
            var y0 = -r.InnerLength / 2; var y1 = r.InnerLength / 2;
            for (var i = 0; i < r.BeamCountX; i++)
            {
                var y = y0 + r.BeamOffsetX + r.BeamSpacingX * i;
                Lines.Add(Line(transform, x0, y, x1, y, "#E67E22", 2, dashed: true));
            }
            for (var i = 0; i < r.BeamCountY; i++)
            {
                var x = x0 + r.BeamOffsetY + r.BeamSpacingY * i;
                Lines.Add(Line(transform, x, y0, x, y1, "#E67E22", 2, dashed: true));
            }
            for (var cx = 0; cx < r.BeamCountY; cx++)
            for (var cy = 0; cy < r.BeamCountX; cy++)
            {
                var x = x0 + r.BeamOffsetY + r.BeamSpacingY * cx;
                var y = y0 + r.BeamOffsetX + r.BeamSpacingX * cy;
                Columns.Add(Point(transform, x, y));
            }
        }

        var height = isMud && r.HasIntermediateSlab ? r.Floor1InnerHeight + r.IntermediateSlabThickness + r.Floor2InnerHeight : r.InnerHeight;
        DimensionSummary = $"기초 {foundationWidth:N0}×{foundationLength:N0} mm · 외벽 외곽 {outerWidth:N0}×{outerLength:N0} mm · 내부 H {height:N0} mm";
    }

    private static CanvasModelTransform BuildFitTransform(double modelWidth, double modelHeight)
    {
        var availableWidth = CanvasWidth - MarginPx * 2;
        var availableHeight = CanvasHeight - MarginPx * 2;
        var scale = Math.Min(availableWidth / Math.Max(modelWidth, 1), availableHeight / Math.Max(modelHeight, 1));
        return new CanvasModelTransform { PixelsPerMillimeter = scale, PanOrigin = new Point(CanvasWidth / 2, CanvasHeight / 2) };
    }

    private static PreviewRectItem Rect(CanvasModelTransform t, double width, double height, string stroke, string fill, double thickness, bool dashed = false)
    {
        var a = t.ToScreen(new(-width / 2, height / 2));
        var b = t.ToScreen(new(width / 2, -height / 2));
        return new PreviewRectItem(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y), stroke, fill, thickness, dashed ? "4,3" : null);
    }

    private static PreviewLineItem Line(CanvasModelTransform t, double x1, double y1, double x2, double y2, string stroke, double thickness, bool dashed = false)
    {
        var a = t.ToScreen(new(x1, y1));
        var b = t.ToScreen(new(x2, y2));
        return new PreviewLineItem(a.X, a.Y, b.X, b.Y, stroke, thickness, dashed ? "4,3" : null);
    }

    private static PreviewPointItem Point(CanvasModelTransform t, double x, double y)
    {
        var p = t.ToScreen(new(x, y));
        return new PreviewPointItem(p.X, p.Y);
    }
}

public sealed record PreviewRectItem(double X, double Y, double Width, double Height, string Stroke, string Fill, double StrokeThickness, string? DashArray);
public sealed record PreviewLineItem(double X1, double Y1, double X2, double Y2, string Stroke, double StrokeThickness, string? DashArray);
public sealed record PreviewPointItem(double X, double Y);
