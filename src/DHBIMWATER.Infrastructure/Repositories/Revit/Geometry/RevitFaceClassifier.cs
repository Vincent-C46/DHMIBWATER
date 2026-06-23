using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Geometry
{
    public class RevitFaceClassifier : IFaceClassifier
    {
        private readonly Func<Document?> _doc;

        public RevitFaceClassifier(Func<Document?> doc)
        {
            _doc = doc;
        }

        public IReadOnlyDictionary<FaceType, double> GetFaceAreas(long elementId)
        {
            var doc = _doc();
            if (doc == null) return new Dictionary<FaceType, double>();

            var elem = doc.GetElement(new ElementId(elementId));
            if (elem == null) return new Dictionary<FaceType, double>();

            var result = new Dictionary<FaceType, double>();
            foreach (var face in RevitGeometryHelper.GetFaces(elem).OfType<PlanarFace>())
            {
                var faceType = Classify(elem, face);
                result[faceType] = result.GetValueOrDefault(faceType) + UC.Ft2ToM2(face.Area);
            }
            return result;
        }

        internal static FaceType Classify(Element elem, PlanarFace face) =>
            (BuiltInCategory)elem.Category.Id.Value switch
            {
                BuiltInCategory.OST_StructuralFraming      => ClassifyBeam(elem, face.FaceNormal),
                BuiltInCategory.OST_Walls                  => ClassifyWall(elem, face),
                BuiltInCategory.OST_Floors                 => ClassifyFloor(elem, face),
                BuiltInCategory.OST_StructuralFoundation   => ClassifyFoundation(face.FaceNormal),
                BuiltInCategory.OST_StructuralColumns      => ClassifyColumn(face.FaceNormal),
                BuiltInCategory.OST_Stairs                 => ClassifyStairs(elem, face.FaceNormal),
                _                                          => FaceType.Side,
            };

        private static FaceType ClassifyBeam(Element elem, XYZ normal)
        {
            if (normal.Z < -0.9) return FaceType.Bottom;
            if (normal.Z > 0.9) return FaceType.Top;

            if (elem.Location is not LocationCurve lc) return FaceType.Side;
            var dir = (lc.Curve.GetEndPoint(1) - lc.Curve.GetEndPoint(0)).Normalize();
            if (Math.Abs(normal.DotProduct(dir)) > 0.9) return FaceType.End;

            var right = dir.CrossProduct(XYZ.BasisZ).Normalize();
            return normal.DotProduct(right) >= 0 ? FaceType.Right : FaceType.Left;
        }

        private static FaceType ClassifyWall(Element elem, PlanarFace face)
        {
            var normal = face.FaceNormal;
            if (normal.Z > 0.9) return FaceType.Top;
            if (normal.Z < -0.9) return FaceType.Bottom;
            if (elem is not Wall wall) return FaceType.Side;

            var dot = normal.DotProduct(wall.Orientation);
            if (Math.Abs(dot) > 0.9) return dot > 0 ? FaceType.Right : FaceType.Left;

            // 벽 방향(길이 방향) 법선 → 마구리 또는 오프닝 측면
            var lc = wall.Location as LocationCurve;
            if (lc == null) return FaceType.End;

            var facePt = EvaluateFaceCenter(face);
            var p0 = lc.Curve.GetEndPoint(0);
            var p1 = lc.Curve.GetEndPoint(1);
            double tolerance = wall.Width; // 벽 두께 기준으로 끝단 판별

            bool nearStart = new XYZ(facePt.X - p0.X, facePt.Y - p0.Y, 0).GetLength() < tolerance;
            bool nearEnd   = new XYZ(facePt.X - p1.X, facePt.Y - p1.Y, 0).GetLength() < tolerance;

            return (nearStart || nearEnd) ? FaceType.End : FaceType.OpeningSide;
        }

        private static FaceType ClassifyFloor(Element elem, PlanarFace face)
        {
            var normal = face.FaceNormal;
            if (normal.Z > 0.9) return FaceType.Top;
            if (normal.Z < -0.9) return FaceType.Bottom;

            if (elem is not Floor floor) return FaceType.Side;

            try
            {
                var sketchIds = floor.GetDependentElements(new ElementClassFilter(typeof(Sketch)));
                var sketch = sketchIds.Count > 0
                    ? floor.Document.GetElement(sketchIds.First()) as Sketch
                    : null;
                if (sketch == null || sketch.Profile.Size <= 1) return FaceType.Side;

                var facePt = EvaluateFaceCenter(face);
                double minDistOuter = MinDistToLoop(sketch.Profile.get_Item(0), facePt);
                double minDistInner = double.MaxValue;
                for (int i = 1; i < sketch.Profile.Size; i++)
                    minDistInner = Math.Min(minDistInner, MinDistToLoop(sketch.Profile.get_Item(i), facePt));

                return minDistInner < minDistOuter ? FaceType.OpeningSide : FaceType.Side;
            }
            catch { return FaceType.Side; }
        }

        // Foundation은 오프닝 분리 미적용 (슬래브와 다름)
        private static FaceType ClassifyFoundation(XYZ normal)
        {
            if (normal.Z > 0.9) return FaceType.Top;
            if (normal.Z < -0.9) return FaceType.Bottom;
            return FaceType.Side;
        }

        private static FaceType ClassifyColumn(XYZ normal)
        {
            if (normal.Z > 0.9) return FaceType.Top;
            if (normal.Z < -0.9) return FaceType.Bottom;
            return FaceType.Side;
        }

        private static FaceType ClassifyStairs(Element elem, XYZ normal)
        {
            if (normal.Z < -0.1) return FaceType.Bottom;
            if (normal.Z > 0.9) return FaceType.Top;

            var runDir = GetStairRunDir2D(elem);
            if (runDir != null && Math.Abs(normal.DotProduct(runDir)) > 0.9)
                return FaceType.End;

            return FaceType.Side;
        }

        private static XYZ EvaluateFaceCenter(PlanarFace face)
        {
            var bbox = face.GetBoundingBox();
            var mid = new UV((bbox.Min.U + bbox.Max.U) / 2, (bbox.Min.V + bbox.Max.V) / 2);
            return face.Evaluate(mid);
        }

        private static double MinDistToLoop(CurveArray loop, XYZ point)
        {
            double min = double.MaxValue;
            foreach (Curve c in loop)
            {
                var result = c.Project(point);
                if (result != null)
                    min = Math.Min(min, result.Distance);
            }
            return min;
        }

        private static XYZ? GetStairRunDir2D(Element elem)
        {
            if (elem is not Stairs stairs) return null;

            var runIds = stairs.GetStairsRuns();
            if (runIds.Count == 0) return null;

            var dirs = new List<XYZ>();
            foreach (var id in runIds)
            {
                if (elem.Document.GetElement(id) is not StairsRun run) continue;
                var path = run.GetStairsPath();
                var firstCurve = path?.FirstOrDefault();
                if (firstCurve == null) continue;
                var raw = firstCurve.GetEndPoint(1) - firstCurve.GetEndPoint(0);
                var dir2D = new XYZ(raw.X, raw.Y, 0);
                if (dir2D.GetLength() > 1e-6)
                    dirs.Add(dir2D.Normalize());
            }

            return dirs.Count switch
            {
                0 => null,
                1 => dirs[0],
                _ => dirs.Aggregate((a, b) => a + b).Normalize(),
            };
        }
    }
}
