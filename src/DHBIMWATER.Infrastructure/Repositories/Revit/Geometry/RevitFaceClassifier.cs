using Autodesk.Revit.DB;
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
                BuiltInCategory.OST_StructuralFraming                            => ClassifyBeam(elem, normal),
                BuiltInCategory.OST_Walls                                        => ClassifyWall(elem, normal),
                BuiltInCategory.OST_Floors or BuiltInCategory.OST_StructuralFoundation => ClassifyFloor(normal),
                BuiltInCategory.OST_StructuralColumns                            => ClassifyColumn(normal),
                _ => FaceType.Side,
            };

        private static FaceType ClassifyBeam(Element elem, XYZ normal)
        {
            if (normal.Z < -0.9) return FaceType.Bottom;
            if (normal.Z > 0.9)  return FaceType.Top;

            if (elem.Location is not LocationCurve lc) return FaceType.Side;
            var dir = (lc.Curve.GetEndPoint(1) - lc.Curve.GetEndPoint(0)).Normalize();
            if (Math.Abs(normal.DotProduct(dir)) > 0.9) return FaceType.End;

            var right = dir.CrossProduct(XYZ.BasisZ).Normalize();
            return normal.DotProduct(right) >= 0 ? FaceType.Right : FaceType.Left;
        }

        private static FaceType ClassifyWall(Element elem, XYZ normal)
        {
            if (normal.Z > 0.9)  return FaceType.Top;
            if (normal.Z < -0.9) return FaceType.Bottom;
            if (elem is not Wall wall) return FaceType.Side;

            var dot = normal.DotProduct(wall.Orientation);
            if (Math.Abs(dot) > 0.9) return dot > 0 ? FaceType.Right : FaceType.Left;
            return FaceType.End;
        }

        private static FaceType ClassifyFloor(XYZ normal)
        {
            if (normal.Z > 0.9)  return FaceType.Top;
            if (normal.Z < -0.9) return FaceType.Bottom;
            return FaceType.Side;
        }

        private static FaceType ClassifyColumn(XYZ normal)
        {
            if (normal.Z > 0.9)  return FaceType.Top;
            if (normal.Z < -0.9) return FaceType.Bottom;
            return FaceType.Side;
        }
    }
}
