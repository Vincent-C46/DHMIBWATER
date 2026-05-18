using System.Linq;
using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewportMoveService
    {
        private readonly Document _doc;

        public ViewportMoveService(Document doc)
        {
            _doc = doc;
        }

        public void Move(string sheetId, string viewId, double x, double y)
        {
            if (!long.TryParse(sheetId, out var sid)) return;
            if (!long.TryParse(viewId, out var vid)) return;

            var sId = new ElementId(sid);
            var vId = new ElementId(vid);

            var viewport = new FilteredElementCollector(_doc, sId)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .FirstOrDefault(vp => vp.ViewId == vId);

            if (viewport == null) return;

            using (var tx = new Transaction(_doc, "Move Viewport To Point"))
            {
                tx.Start();
                viewport.SetBoxCenter(new XYZ(x, y, 0));
                tx.Commit();
            }
        }
        public void MoveBySheetRatio(string sheetId, string viewId, double uRatio, double vRatio)
        {
            if (!long.TryParse(sheetId, out var sid)) return;
            if (!long.TryParse(viewId, out var vid)) return;

            var sId = new ElementId(sid);
            var vId = new ElementId(vid);

            var sheet = _doc.GetElement(sId) as ViewSheet;
            if (sheet == null) return;

            var viewport = new FilteredElementCollector(_doc, sId)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .FirstOrDefault(vp => vp.ViewId == vId);

            if (viewport == null) return;

            var outline = sheet.Outline;
            var x = outline.Min.U + (outline.Max.U - outline.Min.U) * uRatio;
            var y = outline.Min.V + (outline.Max.V - outline.Min.V) * vRatio;

            using (var tx = new Transaction(_doc, "Move Viewport By Ratio"))
            {
                tx.Start();
                viewport.SetBoxCenter(new XYZ(x, y, 0));
                tx.Commit();
            }
        }

        public void ArrangeByDirection(string sheetId, string directionType)
        {
            if (!long.TryParse(sheetId, out var sid)) return;

            var sId = new ElementId(sid);
            var sheet = _doc.GetElement(sId) as ViewSheet;
            if (sheet == null) return;

            var viewports = new FilteredElementCollector(_doc, sId)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .OrderBy(vp => vp.Id.Value)
                .ToList();

            if (viewports.Count == 0) return;

            var points = GetDirectionPoints(directionType, viewports.Count);
            var targets = viewports
                .Select((viewport, index) => new
                {
                    Viewport = viewport,
                    Center = GetClampedSheetPoint(sheet.Outline, viewport, points[index])
                })
                .ToList();

            using (var tx = new Transaction(_doc, "Arrange Viewports By Direction"))
            {
                tx.Start();

                foreach (var target in targets)
                {
                    try
                    {
                        target.Viewport.SetBoxCenter(target.Center);
                    }
                    catch
                    {
                        // Some viewport states can reject movement; keep arranging the rest.
                    }
                }

                tx.Commit();
            }
        }

        private static XYZ GetClampedSheetPoint(BoundingBoxUV sheetOutline, Viewport viewport, UV point)
        {
            var x = sheetOutline.Min.U + (sheetOutline.Max.U - sheetOutline.Min.U) * point.U;
            var y = sheetOutline.Min.V + (sheetOutline.Max.V - sheetOutline.Min.V) * point.V;

            var box = viewport.GetBoxOutline();
            var halfWidth = (box.MaximumPoint.X - box.MinimumPoint.X) * 0.5;
            var halfHeight = (box.MaximumPoint.Y - box.MinimumPoint.Y) * 0.5;
            var margin = 0.05;

            var minX = sheetOutline.Min.U + halfWidth + margin;
            var maxX = sheetOutline.Max.U - halfWidth - margin;
            var minY = sheetOutline.Min.V + halfHeight + margin;
            var maxY = sheetOutline.Max.V - halfHeight - margin;

            if (minX <= maxX)
                x = System.Math.Max(minX, System.Math.Min(maxX, x));

            if (minY <= maxY)
                y = System.Math.Max(minY, System.Math.Min(maxY, y));

            return new XYZ(x, y, 0);
        }

        private static List<UV> GetDirectionPoints(string directionType, int count)
        {
            if (count <= 1 || string.IsNullOrWhiteSpace(directionType) || directionType == "Center")
                return Repeat(new UV(0.5, 0.5), count);

            switch (directionType)
            {
                case "Horizontal":
                    return SpreadHorizontal(count);

                case "Vertical":
                    return SpreadVertical(count);

                case "ZHorizontal":
                    return Grid(count, horizontalFirst: true);

                case "ZVertical":
                    return Grid(count, horizontalFirst: false);

                default:
                    return Repeat(new UV(0.5, 0.5), count);
            }
        }

        private static List<UV> Repeat(UV point, int count)
        {
            var points = new List<UV>();
            for (var i = 0; i < count; i++)
                points.Add(point);
            return points;
        }

        private static List<UV> SpreadHorizontal(int count)
        {
            var points = new List<UV>();
            var min = 0.30;
            var max = 0.70;

            for (var i = 0; i < count; i++)
            {
                var u = count == 1 ? 0.5 : min + (max - min) * i / (count - 1);
                points.Add(new UV(u, 0.5));
            }

            return points;
        }

        private static List<UV> SpreadVertical(int count)
        {
            var points = new List<UV>();
            var top = 0.68;
            var bottom = 0.32;

            for (var i = 0; i < count; i++)
            {
                var v = count == 1 ? 0.5 : top - (top - bottom) * i / (count - 1);
                points.Add(new UV(0.5, v));
            }

            return points;
        }

        private static List<UV> Grid(int count, bool horizontalFirst)
        {
            var horizontal = new[]
            {
                new UV(0.30, 0.68),
                new UV(0.70, 0.68),
                new UV(0.30, 0.32),
                new UV(0.70, 0.32)
            };

            var vertical = new[]
            {
                new UV(0.30, 0.68),
                new UV(0.30, 0.32),
                new UV(0.70, 0.68),
                new UV(0.70, 0.32)
            };

            var source = horizontalFirst ? horizontal : vertical;
            var points = new List<UV>();

            for (var i = 0; i < count; i++)
                points.Add(i < source.Length ? source[i] : new UV(0.5, 0.5));

            return points;
        }
        public void UpdateTitleLayout(string sheetId, string viewId, double offsetX, double offsetY, double lineLength)
        {
            if (!long.TryParse(sheetId, out var sid)) return;
            if (!long.TryParse(viewId, out var vid)) return;

            var sId = new ElementId(sid);
            var vId = new ElementId(vid);

            var viewport = new FilteredElementCollector(_doc, sId)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .FirstOrDefault(vp => vp.ViewId == vId);

            if (viewport == null) return;

            using (var tx = new Transaction(_doc, "Update Viewport Title Layout"))
            {
                tx.Start();

                viewport.LabelOffset = new XYZ(offsetX, offsetY, 0);
                viewport.LabelLineLength = lineLength;

                tx.Commit();
            }
        }

        public void UpdateReservoirTitleLayout(string sheetId, string viewId, bool alignRightBottom)
        {
            if (!long.TryParse(sheetId, out var sid)) return;
            if (!long.TryParse(viewId, out var vid)) return;

            var sId = new ElementId(sid);
            var vId = new ElementId(vid);

            var viewport = new FilteredElementCollector(_doc, sId)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .FirstOrDefault(vp => vp.ViewId == vId);

            if (viewport == null) return;

            var outline = viewport.GetBoxOutline();
            var width = outline.MaximumPoint.X - outline.MinimumPoint.X;
            if (width <= 0) return;

            var lineLength = 0.12;
            var offsetX = alignRightBottom
                ? width + 0.12
                : System.Math.Max(0, (width - lineLength) * 0.5);
            var offsetY = alignRightBottom ? -0.12 : -0.18;

            using (var tx = new Transaction(_doc, "Update Reservoir Viewport Title Layout"))
            {
                tx.Start();
                viewport.LabelOffset = new XYZ(offsetX, offsetY, 0);
                viewport.LabelLineLength = lineLength;
                tx.Commit();
            }
        }

    }
}
