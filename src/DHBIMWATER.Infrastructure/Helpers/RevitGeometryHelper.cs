using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Helpers
{
    public static class RevitGeometryHelper
    {
        private static readonly Options _options = new Options
        {
            ComputeReferences = true,
            IncludeNonVisibleObjects = false,
            DetailLevel = ViewDetailLevel.Fine,
        };

        // ─────────────────────────────────────────────
        // Solid 추출
        // ─────────────────────────────────────────────

        // Element의 모든 Solid를 반환.
        // GeometryInstance(패밀리 내부 등) 도 재귀적으로 탐색.
        public static IEnumerable<Solid> GetSolids(Element elem)
            => ExtractSolids(elem.get_Geometry(_options));

        private static IEnumerable<Solid> ExtractSolids(GeometryElement geoElem)
        {
            if (geoElem == null) yield break;

            foreach (var obj in geoElem)
            {
                if (obj is Solid solid && solid.Volume > 1e-9)
                    yield return solid;
                else if (obj is GeometryInstance geoInst)
                    foreach (var s in ExtractSolids(geoInst.GetInstanceGeometry()))
                        yield return s;
            }
        }
        // ─────────────────────────────────────────────
        // Face 추출
        // ─────────────────────────────────────────────

        // Solid의 모든 Face를 반환.
        public static IEnumerable<Face> GetFaces(Solid solid)
        {
            foreach (Face face in solid.Faces)
                yield return face;
        }

        // Element의 모든 Solid에서 Face를 반환.
        public static IEnumerable<Face> GetFaces(Element elem)
            => GetSolids(elem).SelectMany(GetFaces);

        // 수평면(상면·하면): 법선벡터의 Z 성분이 threshold 이상인 PlanarFace.
        public static IEnumerable<PlanarFace> GetHorizontalFaces(Element elem, double threshold = 0.9)
            => GetFaces(elem)
                .OfType<PlanarFace>()
                .Where(f => Math.Abs(f.FaceNormal.Z) >= threshold);

        // 수직면(측면): 법선벡터의 Z 성분이 threshold 미만인 PlanarFace.
        public static IEnumerable<PlanarFace> GetVerticalFaces(Element elem, double threshold = 0.1)
            => GetFaces(elem)
                .OfType<PlanarFace>()
                .Where(f => Math.Abs(f.FaceNormal.Z) < threshold);

        // 가장 높은 수평면(상면)을 반환. 없으면 null.
        public static PlanarFace? GetTopFace(Element elem)
            => GetHorizontalFaces(elem)
                .Where(f => f.FaceNormal.Z > 0)
                .OrderByDescending(f => f.Origin.Z)
                .FirstOrDefault();

        // 가장 낮은 수평면(하면)을 반환. 없으면 null.
        public static PlanarFace? GetBottomFace(Element elem)
            => GetHorizontalFaces(elem)
                .Where(f => f.FaceNormal.Z < 0)
                .OrderBy(f => f.Origin.Z)
                .FirstOrDefault();

        // ─────────────────────────────────────────────
        // 중앙 단면적 추출
        // ─────────────────────────────────────────────

        // Solid의 중앙을 direction 법선으로 자른 단면적(ft²)을 반환.
        // 끝면이 다른 요소에 의해 잘려있어도 중앙부 단면을 정확하게 추출함.
        public static double GetMidSectionArea(Solid solid, XYZ direction)
        {
            const double slabThk = 0.01; // feet

            try
            {
                var projections = solid.Edges.Cast<Edge>()
                    .SelectMany(e => new[] { e.AsCurve().GetEndPoint(0), e.AsCurve().GetEndPoint(1) })
                    .Select(v => v.DotProduct(direction))
                    .ToList();
                double midProj = (projections.Min() + projections.Max()) / 2;

                var refPt = solid.Edges.Cast<Edge>().First().AsCurve().GetEndPoint(0);
                var center = refPt + direction * (midProj - refPt.DotProduct(direction));

                var perp1 = direction.CrossProduct(XYZ.BasisZ);
                if (perp1.GetLength() < 1e-6)
                    perp1 = direction.CrossProduct(XYZ.BasisX);
                perp1 = perp1.Normalize();
                var perp2 = direction.CrossProduct(perp1).Normalize();

                double half = 10.0; // feet
                var p1 = center + perp1 * half + perp2 * half;
                var p2 = center - perp1 * half + perp2 * half;
                var p3 = center - perp1 * half - perp2 * half;
                var p4 = center + perp1 * half - perp2 * half;

                var loop = CurveLoop.Create(new List<Curve>
                {
                    Line.CreateBound(p1, p2),
                    Line.CreateBound(p2, p3),
                    Line.CreateBound(p3, p4),
                    Line.CreateBound(p4, p1),
                });

                var slab = GeometryCreationUtilities.CreateExtrusionGeometry(
                    new[] { loop }, direction, slabThk);

                var intersection = BooleanOperationsUtils.ExecuteBooleanOperation(
                    solid, slab, BooleanOperationsType.Intersect);

                if (intersection == null || intersection.Volume < 1e-10) return 0;
                return intersection.Volume / slabThk;
            }
            catch { return 0; }
        }
    }
}
