using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Diagnostics;

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

        /// <summary>
        /// Element의 모든 Solid를 반환합니다.
        /// GeometryInstance(패밀리 내부 등) 도 재귀적으로 탐색합니다.
        /// </summary>
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
        public static Solid? GetSolid(Element elem)
        {
            var solids = elem.get_Geometry(new Options { ComputeReferences = true })
                                .OfType<Solid>()
                                .Where(s => s.Volume > 1e-6)
                                .ToList();

            if (!solids.Any()) return null; // 솔리드가 하나도 없으면 null 반환
            //Debug.WriteLine($"=================================");
            //Debug.WriteLine($"솔리드 개수: {solids.Count}개 / Solid Face 개수 {solids.FirstOrDefault().Faces.Size}개");

            if (solids.Count == 1) return solids.FirstOrDefault();  // 솔리드가 1개면 해당 솔리드 반환

            var result = solids[0];

            foreach (var solid in solids.Skip(1))
            {
                try
                {
                    result = BooleanOperationsUtils.ExecuteBooleanOperation(result, solid, BooleanOperationsType.Union);
                }
                catch { }
            }
            return result;
        }
        // ─────────────────────────────────────────────
        // Face 추출
        // ─────────────────────────────────────────────

        /// <summary>
        /// Solid의 모든 Face를 반환합니다.
        /// </summary>
        public static IEnumerable<Face> GetFaces(Solid solid)
        {
            foreach (Face face in solid.Faces)
                yield return face;
        }

        /// <summary>
        /// Element의 모든 Solid에서 Face를 반환합니다.
        /// </summary>
        public static IEnumerable<Face> GetFaces(Element elem)
            => GetSolids(elem).SelectMany(GetFaces);

        /// <summary>
        /// 수평면(상면·하면): 법선벡터의 Z 성분이 threshold 이상인 PlanarFace.
        /// </summary>
        public static IEnumerable<PlanarFace> GetHorizontalFaces(Element elem, double threshold = 0.9)
            => GetFaces(elem)
                .OfType<PlanarFace>()
                .Where(f => Math.Abs(f.FaceNormal.Z) >= threshold);

        /// <summary>
        /// 수직면(측면): 법선벡터의 Z 성분이 threshold 미만인 PlanarFace.
        /// </summary>
        public static IEnumerable<PlanarFace> GetVerticalFaces(Element elem, double threshold = 0.1)
            => GetFaces(elem)
                .OfType<PlanarFace>()
                .Where(f => Math.Abs(f.FaceNormal.Z) < threshold);

        /// <summary>
        /// 가장 높은 수평면(상면)을 반환합니다. 없으면 null.
        /// </summary>
        public static PlanarFace? GetTopFace(Element elem)
            => GetHorizontalFaces(elem)
                .Where(f => f.FaceNormal.Z > 0)
                .OrderByDescending(f => f.Origin.Z)
                .FirstOrDefault();

        /// <summary>
        /// 가장 낮은 수평면(하면)을 반환합니다. 없으면 null.
        /// </summary>
        public static PlanarFace? GetBottomFace(Element elem)
            => GetHorizontalFaces(elem)
                .Where(f => f.FaceNormal.Z < 0)
                .OrderBy(f => f.Origin.Z)
                .FirstOrDefault();
    }
}
