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
    }
}
