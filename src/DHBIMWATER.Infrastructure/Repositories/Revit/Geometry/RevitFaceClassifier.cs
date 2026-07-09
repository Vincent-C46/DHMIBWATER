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
                BuiltInCategory.OST_StructuralFraming    => ClassifyBeam(elem, face.FaceNormal),
                BuiltInCategory.OST_Walls                => ClassifyWall(elem, face),
                BuiltInCategory.OST_Floors               => ClassifyFloor(elem, face),
                BuiltInCategory.OST_StructuralFoundation => ClassifyFoundation(face.FaceNormal),
                BuiltInCategory.OST_StructuralColumns    => ClassifyColumn(face.FaceNormal),
                BuiltInCategory.OST_Stairs               => ClassifyStairs(elem, face.FaceNormal),
                _                                        => FaceType.Side,
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

            // 벽 방향(길이 방향) 법선 → End vs OpeningSide
            // Left/Right 메인면의 내부 EdgeLoop 엣지와 공유 여부로 판별
            try
            {
                foreach (var solid in RevitGeometryHelper.GetSolids(wall))
                {
                    var innerEdges = GetInnerLoopEdges(solid,
                        f => f is PlanarFace pf && Math.Abs(pf.FaceNormal.DotProduct(wall.Orientation)) > 0.9);

                    if (innerEdges.Count == 0) continue;
                    if (SharesEdgeWith(face, innerEdges)) return FaceType.OpeningSide;
                }
            }
            catch { }

            return FaceType.End;
        }

        private static FaceType ClassifyFloor(Element elem, PlanarFace face)
        {
            var normal = face.FaceNormal;
            if (normal.Z > 0.9) return FaceType.Top;
            if (normal.Z < -0.9) return FaceType.Bottom;

            // 수평면(Top/Bottom)의 내부 EdgeLoop 엣지와 공유 여부로 판별
            try
            {
                foreach (var solid in RevitGeometryHelper.GetSolids(elem))
                {
                    var innerEdges = GetInnerLoopEdges(solid,
                        f => f is PlanarFace pf && Math.Abs(pf.FaceNormal.Z) > 0.9);

                    if (innerEdges.Count == 0) continue;
                    if (SharesEdgeWith(face, innerEdges)) return FaceType.OpeningSide;
                }
            }
            catch { }

            return FaceType.Side;
        }

        // Foundation은 오프닝 분리 미적용
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

        // 기준면의 내부 EdgeLoop(1+) 엣지를 수집
        private static HashSet<Edge> GetInnerLoopEdges(Solid solid, Func<Face, bool> isFaceMatch)
        {
            var result = new HashSet<Edge>();
            foreach (Face f in solid.Faces)
            {
                if (!isFaceMatch(f) || f.EdgeLoops.Size <= 1) continue;
                for (int i = 1; i < f.EdgeLoops.Size; i++)
                    foreach (Edge e in f.EdgeLoops.get_Item(i))
                        result.Add(e);
            }
            return result;
        }

        // 면의 EdgeLoop 중 innerEdges와 공유되는 엣지가 있는지 확인
        private static bool SharesEdgeWith(PlanarFace face, HashSet<Edge> innerEdges)
        {
            foreach (EdgeArray loop in face.EdgeLoops)
                foreach (Edge e in loop)
                    if (innerEdges.Contains(e))
                        return true;
            return false;
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
