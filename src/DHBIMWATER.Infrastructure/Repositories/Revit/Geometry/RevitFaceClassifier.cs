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
                var faceType = Classify(elem, face.FaceNormal);
                result[faceType] = result.GetValueOrDefault(faceType) + UC.Ft2ToM2(face.Area);
            }
            return result;
        }

        internal static FaceType Classify(Element elem, XYZ normal) =>
            (BuiltInCategory)elem.Category.Id.Value switch
            {
                BuiltInCategory.OST_StructuralFraming => ClassifyBeam(elem, normal),
                BuiltInCategory.OST_Walls => ClassifyWall(elem, normal),
                BuiltInCategory.OST_Floors or BuiltInCategory.OST_StructuralFoundation => ClassifyFloor(normal),
                BuiltInCategory.OST_StructuralColumns => ClassifyColumn(normal),
                BuiltInCategory.OST_Stairs => ClassifyStairs(elem, normal),
                _ => FaceType.Side,
            };

        private static FaceType ClassifyBeam(Element elem, XYZ normal)
        {
            if (normal.Z < -0.9) return FaceType.Bottom;
            if (normal.Z > 0.9) return FaceType.Top;

            if (elem.Location is not LocationCurve lc) return FaceType.Side;    // Beam���� LC�� ����ȵǴ� ���� ������?
            var dir = (lc.Curve.GetEndPoint(1) - lc.Curve.GetEndPoint(0)).Normalize();
            if (Math.Abs(normal.DotProduct(dir)) > 0.9) return FaceType.End;    // �������� ���� ������ ���� End�� �з�

            var right = dir.CrossProduct(XYZ.BasisZ).Normalize();
            return normal.DotProduct(right) >= 0 ? FaceType.Right : FaceType.Left;
        }

        private static FaceType ClassifyWall(Element elem, XYZ normal)
        {
            if (normal.Z > 0.9) return FaceType.Top;
            if (normal.Z < -0.9) return FaceType.Bottom;
            if (elem is not Wall wall) return FaceType.Side;

            var dot = normal.DotProduct(wall.Orientation);
            if (Math.Abs(dot) > 0.9) return dot > 0 ? FaceType.Right : FaceType.Left;
            return FaceType.End;
        }

        private static FaceType ClassifyFloor(XYZ normal)
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
                return FaceType.End;  // 챌판(riser): 법선이 진행방향과 평행

            return FaceType.Side;    // 계단 측면: 법선이 진행방향과 수직
        }

        private static XYZ? GetStairRunDir2D(Element elem)
        {
            if (elem is not Stairs stairs) return null;

            var runIds = stairs.GetStairsRuns();
            if (runIds.Count == 0) return null;

            // 다수 Run이 있을 경우(L형·U형) 모두 수집
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
                _ => dirs.Aggregate((a, b) => a + b).Normalize(),  // 여러 Run 방향 평균
            };
        }
    }
}
