using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using DHBIMWATER.Application.Interfaces.Geometry;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Geometry
{
    public class RevitIntersectingElementFinder : IIntersectingElementFinder
    {
        private readonly Func<Document?> _doc;

        public RevitIntersectingElementFinder(Func<Document?> doc)
        {
            _doc = doc;
        }

        // ElementIntersectsElementFilter는 볼륨 겹침만 감지하므로 면 접촉(face-to-face)은 누락됨
        // BoundingBox를 epsilon만큼 팽창시켜 면 접촉 요소도 포함
        private const double Epsilon = 0.01; // feet 단위 0.01ft = 약 3mm

        public IEnumerable<long> FindIntersecting(long referenceElementId)
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            var refElem = doc.GetElement(new ElementId(referenceElementId));
            if (refElem == null) return Enumerable.Empty<long>();
            Debug.WriteLine($"RefElemId: {refElem.Id.Value} / 카테고리: {refElem.Category.Name}");

            // 기준 객체 Solid
            var refSolid = GetSolid(refElem);

            if (refSolid == null) return Enumerable.Empty<long>();

            var bbox = refElem.get_BoundingBox(null);
            if (bbox == null) return Enumerable.Empty<long>();

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
                                .Where(e => e.Id.Value != referenceElementId)
                                .ToList();

            foreach (var c in candidates)
            {

                Debug.WriteLine($"CandidateElemId: {c.Id.Value} / 카테고리: {c.Category.Name}");

                var cSolid = GetSolid(c);
                var contactArea = GetContactFaceArea(refSolid, cSolid);

                Debug.WriteLine($"Contact Area: {contactArea}");

                // 2단계: Solid 취득 여부
                //Debug.WriteLine($"  candidate {c.Id.Value} solid: {(cSolid == null ? "NULL" : "OK")}");
                //Debug.WriteLine($"  candidate 카테고리: {c.Category.Name} Volumne: {cSolid.Volume}");

                if (cSolid == null) continue;

                try
                {
                    var intersection = BooleanOperationsUtils.ExecuteBooleanOperation(
                        refSolid, cSolid, BooleanOperationsType.Intersect);

                    //// 3단계: Boolean 결과
                    //Debug.WriteLine($"  intersection Volume={intersection?.Volume}, Faces={intersection?.Faces.Size}");
                    //Debug.WriteLine($"  ContactFaceArea= {GetContactFaceArea(refSolid, cSolid)}");
                }
                catch (Exception ex)
                {
                    // 3단계: 예외 발생 여부
                    Debug.WriteLine($"  Boolean 예외: {ex.Message}");
                }
            }

            //var result = candidates.Where(e => IsTouching(refSolid, e))
            //                 .Select(e => e.Id.Value)
            //                 .ToList();

            //Debug.WriteLine($"result: {result[0]}");

            var result = new List<long>();
            return result;
        }

        //private bool IsTouching(Solid refSolid, Element candidate)
        //{
        //    var candidateSolid = GetSolid(candidate);
        //    if (candidateSolid == null) return false;

        //    try
        //    {
        //        var intersection = BooleanOperationsUtils.ExecuteBooleanOperation(
        //            refSolid,
        //            candidateSolid,
        //            BooleanOperationsType.Intersect);

        //        if (intersection == null) return false;

        //        return intersection.Volume > 1e-6
        //            || intersection.Faces.Size > 0;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
        private Solid? GetSolid(Element elem)
        {
            var solids = elem.get_Geometry(new Options { ComputeReferences = true })
                                .OfType<Solid>()
                                .Where(s => s.Volume > 1e-6)
                                .ToList();

            if (!solids.Any()) return null; // 솔리드가 하나도 없으면 null 반환
            Debug.WriteLine($"=================================");
            Debug.WriteLine($"솔리드 개수: {solids.Count}개 / Solid Face 개수 {solids.FirstOrDefault().Faces.Size}개");

            if (solids.Count == 1) return solids.FirstOrDefault();  // 솔리드가 1개면 해당 솔리드 반환

            var result = solids[0];


            foreach (var solid in solids)
            {
                try
                {
                    result = BooleanOperationsUtils.ExecuteBooleanOperation(result, solid, BooleanOperationsType.Union);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", $"{ex.Message}\n솔리드 Merge 실패. ElementId: {elem.Id.Value}");
                }
            }

            return result;
        }
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
                    BuiltInCategory.OST_StructuralColumns,
                    BuiltInCategory.OST_StructuralFraming,
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
                },
                _ => FallbackCategories,
            };
        private static double GetContactFaceArea(Solid refSolid, Solid candidateSolid)
        {
            double totalArea = 0;
            double thickness = 0.01;

            foreach (Face refFace in refSolid.Faces)
            {
                var refNormal = refFace.ComputeNormal(new UV(0.5, 0.5));

                foreach (Face candidateFace in candidateSolid.Faces)
                {
                    // 반대 Normal 인 면만 처리
                    var candidateNormal = candidateFace.ComputeNormal(new UV(0.5, 0.5));
                    if (refNormal.DotProduct(candidateNormal) > -0.9) continue;

                    // FaceIntersectionFaceResult로 실제 겹치는지 확인
                    var result = refFace.Intersect(candidateFace);
                    if (result != FaceIntersectionFaceResult.Intersecting) continue;
                    try
                    {
                        var thinSolid = CreateExtrusionSolid(candidateFace, thickness);
                        var intersectingSolid = BooleanOperationsUtils.ExecuteBooleanOperation(refSolid, thinSolid, BooleanOperationsType.Intersect);
                        Debug.WriteLine($"Intersecting 체적: {intersectingSolid.Volume}");

                        if (intersectingSolid == null || intersectingSolid.Volume < 1e-10) continue;
                        totalArea += UC.Ft2ToM2(intersectingSolid.Volume / thickness);
                    }
                    catch { continue; }
                }

                //if (refFace is not PlanarFace planarRef) continue;  // 평평한 면이 아니면 건너뛰기

                //foreach (Face candidateFace in candidateSolid.Faces)
                //{
                //    var result = refFace.Intersect(candidateFace);
                //    //if (result == FaceIntersectionFaceResult.Intersecting) continue;

                //    refFace.GetEdges

                //    // 두 Face의 Normal 벡터가 반대방향인지 확인
                //    var candidateNormal = candidateFace.ComputeNormal(new UV(0.5, 0.5));
                //    if (refNormal.DotProduct(candidateNormal) > -0.99) continue;

                //    try
                //    {

                //        var refArea = UC.Ft2ToM2(refFace.Area);
                //        var candidateArea = UC.Ft2ToM2(candidateFace.Area);
                //        totalArea += Math.Min(refArea, candidateArea);
                //    }
                //    catch
                //    {
                //    }
                //}
            }
            Debug.WriteLine($"접촉면적: {totalArea}");
            return totalArea;
        }

        private static Solid CreateExtrusionSolid(Face face, double thickness)
        {
            var curveLoops = face.GetEdgesAsCurveLoops().FirstOrDefault();
            var faceNormal = face.ComputeNormal(new UV(0.5, 0.5));
            var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new[] { curveLoops }, faceNormal, thickness);

            return solid;
        }
    }
}
