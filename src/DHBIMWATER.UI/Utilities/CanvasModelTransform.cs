using DHBIMWATER.Core.Geometry;
using System.Windows;

namespace DHBIMWATER.UI.Utilities;

/// <summary>Canvas pixel 좌표와 배관 모델 mm 좌표 사이의 변환을 담당한다.</summary>
public sealed class CanvasModelTransform
{
    public double PixelsPerMillimeter { get; set; } = 0.08;
    public Point PanOrigin { get; set; } = new(40, 40);

    public Point2D ToModel(Point screen) => new((screen.X - PanOrigin.X) / PixelsPerMillimeter, (PanOrigin.Y - screen.Y) / PixelsPerMillimeter);
    public Point ToScreen(Point2D model) => new(PanOrigin.X + model.X * PixelsPerMillimeter, PanOrigin.Y - model.Y * PixelsPerMillimeter);
}
