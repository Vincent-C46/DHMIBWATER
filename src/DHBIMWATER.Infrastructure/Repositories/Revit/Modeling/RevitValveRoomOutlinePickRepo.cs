using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Piping;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling
{
    /// <summary>
    /// Revit 뷰에서 외곽 벽체를 피킹해 <see cref="ValveRoomOutline"/>을 만든다.
    /// 반드시 Revit API 컨텍스트(ExternalEvent)에서 호출해야 한다.
    /// </summary>
    internal class RevitValveRoomOutlinePickRepo : IValveRoomOutlinePickRepo
    {
        private readonly Func<UIDocument?> _uiDoc;

        public RevitValveRoomOutlinePickRepo(Func<UIDocument?> uiDoc)
        {
            _uiDoc = uiDoc;
        }

        public ValveRoomOutline? PickExteriorWalls()
        {
            var uiDoc = _uiDoc();
            if (uiDoc == null) return null;

            IList<Reference> references;
            try
            {
                references = uiDoc.Selection.PickObjects(ObjectType.Element, new WallSelectionFilter(), "밸브실 외곽 벽체를 선택한 뒤 완료를 누르세요");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;    // 사용자가 ESC로 취소
            }

            var walls = references
                .Select(r => uiDoc.Document.GetElement(r) as Wall)
                .Where(w => w != null && w!.Location is LocationCurve { Curve: Line })
                .Cast<Wall>()
                .ToList();
            if (walls.Count == 0) return null;

            // 실내측 판정 기준점: 선택한 벽 중심선 중점들의 평균.
            var center = Average(walls.Select(MidPoint).ToList());

            var outlineWalls = walls.Select(wall => ToOutlineWall(wall, center)).ToList();
            return new ValveRoomOutline(outlineWalls);
        }

        private static OutlineWall ToOutlineWall(Wall wall, XYZ center)
        {
            var line = (Line)((LocationCurve)wall.Location).Curve;
            var start = line.GetEndPoint(0);
            var end = line.GetEndPoint(1);

            var direction = (end - start).Normalize();
            var normal = new XYZ(-direction.Y, direction.X, 0);      // 평면상 좌측 법선
            var half = wall.Width / 2.0;

            // 중심점에 더 가까운 쪽이 실내측이다.
            var mid = (start + end) / 2.0;
            var towardCenter = mid + normal * half;
            var awayFromCenter = mid - normal * half;
            var innerNormal = towardCenter.DistanceTo(center) <= awayFromCenter.DistanceTo(center) ? normal : -normal;

            var innerStart = start + innerNormal * half;
            var innerEnd = end + innerNormal * half;
            var outerStart = start - innerNormal * half;
            var outerEnd = end - innerNormal * half;

            return new OutlineWall(
                wall.Id.Value,
                ToPoint2D(innerStart),
                ToPoint2D(innerEnd),
                ToPoint2D(outerStart),
                ToPoint2D(outerEnd),
                UC.FtToMm(wall.Width));
        }

        private static XYZ MidPoint(Wall wall)
        {
            var curve = ((LocationCurve)wall.Location).Curve;
            return (curve.GetEndPoint(0) + curve.GetEndPoint(1)) / 2.0;
        }

        private static XYZ Average(IReadOnlyList<XYZ> points)
            => new(points.Average(p => p.X), points.Average(p => p.Y), 0);

        private static Point2D ToPoint2D(XYZ point) => new(UC.FtToMm(point.X), UC.FtToMm(point.Y));

        private sealed class WallSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) => elem is Wall;
            public bool AllowReference(Reference reference, XYZ position) => false;
        }
    }
}
