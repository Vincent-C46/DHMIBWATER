using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using System.Diagnostics;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Geometry
{
    public class RevitIntersectingElementFinder : IIntersectingElementFinder
    {
        private readonly Func<Document?> _doc;
        private const double Epsilon = 0.01; // feet 단위 0.01ft = 약 3mm
        private const double SolidThk = 0.01;
        public RevitIntersectingElementFinder(Func<Document?> doc)
        {
            _doc = doc;
        }

        public IReadOnlyList<FaceDeduction> FindContactAreas(long refElemId)
        {
            var doc = _doc();
            if (doc == null) return new List<FaceDeduction>();

            var refElem = doc.GetElement(new ElementId(refElemId));
            if (refElem == null) return new List<FaceDeduction>();

            //Debug.WriteLine($"RefElemId: {refElem.Id.Value} / 카테고리: {refElem.Category.Name}");

            // 기준 객체 Solid (multi-body solid를 개별 solid로 분리)
            var refSolids = RevitGeometryHelper.GetSolids(refElem)
                .SelectMany(s => { try { return SolidUtils.SplitVolumes(s); } catch { return [s]; } })
                .Where(s => s.Volume > 1e-9)
                .ToList();
            if (!refSolids.Any()) return new List<FaceDeduction>();

            var bbox = refElem.get_BoundingBox(null);
            if (bbox == null) return new List<FaceDeduction>();

            var expandedMin = new XYZ(bbox.Min.X - Epsilon, bbox.Min.Y - Epsilon, bbox.Min.Z - Epsilon);
            var expandedMax = new XYZ(bbox.Max.X + Epsilon, bbox.Max.Y + Epsilon, bbox.Max.Z + Epsilon);
            var outline = new Outline(expandedMin, expandedMax);

            var refCategory = (BuiltInCategory)refElem.Category.Id.Value;
            var targetCategories = GetTargetCategories(refCategory);

            // 1차 필터링: 확장된 BBox와 카테고리로 후보군 추출
            var candidates = new FilteredElementCollector(doc)
                                .WhereElementIsNotElementType()
                                .WherePasses(new ElementMulticategoryFilter(targetCategories))
                                .WherePasses(new BoundingBoxIntersectsFilter(outline))
                                .Where(e => e.Id.Value != refElemId)
                                .ToList();

            var contacts = new List<FaceDeduction>();

            foreach (var candidate in candidates)
            {
                var candidateSolids = RevitGeometryHelper.GetSolids(candidate)
                    .SelectMany(s => { try { return SolidUtils.SplitVolumes(s); } catch { return [s]; } })
                    .Where(s => s.Volume > 1e-9)
                    .ToList();
                if (!candidateSolids.Any()) continue;

                foreach (var refSolid in refSolids)
                foreach (Face refFace in refSolid.Faces)
                {
                    if (refFace is not PlanarFace planarRef) continue;
                    var refNormal = planarRef.FaceNormal;
                    var refOrigin = planarRef.Origin;

                    foreach (var candidateSolid in candidateSolids)
                    foreach (Face candidateFace in candidateSolid.Faces)
                    {
                        if (candidateFace is not PlanarFace planarCand) continue;
                        var candidateNormal = planarCand.FaceNormal;
                        var candidateOrigin = planarCand.Origin;

                        if (refNormal.DotProduct(candidateNormal) > -0.9) continue;

                        var originDiff = candidateOrigin - refOrigin;
                        var distance = Math.Abs(originDiff.DotProduct(refNormal));
                        if (distance > 0.01) continue;

                        try
                        {
                            var thinSolid = CreateExtrusionSolid(refFace, SolidThk);
                            var intersectingSolid = BooleanOperationsUtils.ExecuteBooleanOperation(candidateSolid, thinSolid, BooleanOperationsType.Intersect);

                            if (intersectingSolid == null || intersectingSolid.Volume < 1e-10) continue;
                            var area = Math.Round(UC.Ft2ToM2(intersectingSolid.Volume / SolidThk), 3);
                            var faceType = RevitFaceClassifier.Classify(refElem, planarRef);

                            contacts.Add(new FaceDeduction(faceType, candidate.Id.Value, area));
                        }
                        catch { continue; }
                    }
                }
            }

            return contacts;
        }

        //public IEnumerable<long> FindIntersecting(long referenceElementId)
        //{
        //    var doc = _doc();
        //    if (doc == null) return Enumerable.Empty<long>();

        //    var refElem = doc.GetElement(new ElementId(referenceElementId));
        //    if (refElem == null) return Enumerable.Empty<long>();
        //    Debug.WriteLine($"RefElemId: {refElem.Id.Value} / 카테고리: {refElem.Category.Name}");

        //    // 기준 객체 Solid
        //    var refSolid = RevitGeometryHelper.GetSolid(refElem);

        //    if (refSolid == null) return Enumerable.Empty<long>();

        //    var bbox = refElem.get_BoundingBox(null);
        //    if (bbox == null) return Enumerable.Empty<long>();

        //    var expandedMin = new XYZ(bbox.Min.X - Epsilon, bbox.Min.Y - Epsilon, bbox.Min.Z - Epsilon);
        //    var expandedMax = new XYZ(bbox.Max.X + Epsilon, bbox.Max.Y + Epsilon, bbox.Max.Z + Epsilon);
        //    var outline = new Outline(expandedMin, expandedMax);

        //    var refCategory = (BuiltInCategory)refElem.Category.Id.Value;
        //    var targetCategories = GetTargetCategories(refCategory);

        //    // 1차 필터링: 확장된 BBox와 카테고리로 후보군 추출
        //    var candidates = new FilteredElementCollector(doc)
        //                        .WhereElementIsNotElementType()
        //                        .WherePasses(new ElementMulticategoryFilter(targetCategories))
        //                        .WherePasses(new BoundingBoxIntersectsFilter(outline))
        //                        .Where(e => e.Id.Value != referenceElementId)
        //                        .ToList();

        //    foreach (var c in candidates)
        //    {

        //        Debug.WriteLine($"CandidateElemId: {c.Id.Value} / 카테고리: {c.Category.Name}");

        //        var cSolid = RevitGeometryHelper.GetSolid(c);
        //        var contactArea = GetContactFaceArea(refSolid, cSolid);

        //        Debug.WriteLine($"Contact Area: {contactArea}");

        //        // 2단계: Solid 취득 여부
        //        //Debug.WriteLine($"  candidate {c.Id.Value} solid: {(cSolid == null ? "NULL" : "OK")}");
        //        //Debug.WriteLine($"  candidate 카테고리: {c.Category.Name} Volumne: {cSolid.Volume}");

        //        if (cSolid == null) continue;

        //        try
        //        {
        //            var intersection = BooleanOperationsUtils.ExecuteBooleanOperation(
        //                refSolid, cSolid, BooleanOperationsType.Intersect);

        //            //// 3단계: Boolean 결과
        //            //Debug.WriteLine($"  intersection Volume={intersection?.Volume}, Faces={intersection?.Faces.Size}");
        //            //Debug.WriteLine($"  ContactFaceArea= {GetContactFaceArea(refSolid, cSolid)}");
        //        }
        //        catch (Exception ex)
        //        {
        //            // 3단계: 예외 발생 여부
        //            Debug.WriteLine($"  Boolean 예외: {ex.Message}");
        //        }
        //    }

        //    //var result = candidates.Where(e => IsTouching(refSolid, e))
        //    //                 .Select(e => e.Id.Value)
        //    //                 .ToList();

        //    //Debug.WriteLine($"result: {result[0]}");

        //    var result = new List<long>();
        //    return result;
        //}
        private static readonly ICollection<BuiltInCategory> FallbackCategories = new[]
     {
            BuiltInCategory.OST_Walls,
            BuiltInCategory.OST_Floors,
            BuiltInCategory.OST_StructuralColumns,
            BuiltInCategory.OST_StructuralFraming,
            BuiltInCategory.OST_StructuralFoundation,
            BuiltInCategory.OST_GenericModel,
        };
        private static ICollection<BuiltInCategory> GetTargetCategories(BuiltInCategory refCategory) =>
            refCategory switch
            {
                BuiltInCategory.OST_Walls => new[]
                {
                    BuiltInCategory.OST_Walls,
                    BuiltInCategory.OST_Floors,
                    BuiltInCategory.OST_StructuralColumns,
                    BuiltInCategory.OST_StructuralFraming,
                },
                BuiltInCategory.OST_Floors => new[]
                {
                    BuiltInCategory.OST_Walls,
                    BuiltInCategory.OST_Floors,
                    BuiltInCategory.OST_StructuralFoundation,
                },
                BuiltInCategory.OST_StructuralColumns => new[]
                {
                    BuiltInCategory.OST_Walls,
                    BuiltInCategory.OST_Floors,
                    BuiltInCategory.OST_StructuralFraming,
                    BuiltInCategory.OST_StructuralFoundation,
                },
                BuiltInCategory.OST_StructuralFraming => new[]
                {
                    BuiltInCategory.OST_Walls,
                    BuiltInCategory.OST_Floors,
                    BuiltInCategory.OST_StructuralColumns,
                    BuiltInCategory.OST_StructuralFraming,
                },
                BuiltInCategory.OST_Stairs => new[]
{
                    BuiltInCategory.OST_Walls,
                    BuiltInCategory.OST_Floors,
                    BuiltInCategory.OST_StructuralColumns,
                    BuiltInCategory.OST_StructuralFraming,
                },
                _ => FallbackCategories,
            };
        private static Solid CreateExtrusionSolid(Face face, double thickness)
        {
            var curveLoops = face.GetEdgesAsCurveLoops().FirstOrDefault();
            var faceNormal = face.ComputeNormal(new UV(0.5, 0.5));
            var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new[] { curveLoops }, faceNormal, thickness);

            return solid;
        }
    }
}
