using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using DHBIMWATER.Application.Interfaces.Geometry;
using System.Diagnostics;

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
        private const double Epsilon = 0.01; // feet 단위

        public IEnumerable<long> FindIntersecting(long referenceElementId)
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            var refElem = doc.GetElement(new ElementId(referenceElementId));
            if (refElem == null) return Enumerable.Empty<long>();

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

            var candidates = new FilteredElementCollector(doc)
                                .WhereElementIsNotElementType()
                                .WherePasses(new ElementMulticategoryFilter(targetCategories))
                                .WherePasses(new BoundingBoxIntersectsFilter(outline))
                                .Where(e => e.Id.Value != referenceElementId)
                                .ToList();

            Debug.WriteLine($"candidates count: {candidates.Count}");

            foreach (var c in candidates)
            {
                var cSolid = GetSolid(c);

                // 2단계: Solid 취득 여부
                Debug.WriteLine($"  candidate {c.Id.Value} solid: {(cSolid == null ? "NULL" : "OK")}");

                if (cSolid == null) continue;

                try
                {
                    var intersection = BooleanOperationsUtils.ExecuteBooleanOperation(
                        refSolid, cSolid, BooleanOperationsType.Intersect);

                    // 3단계: Boolean 결과
                    Debug.WriteLine($"  intersection Volume={intersection?.Volume}, Faces={intersection?.Faces.Size}");
                }
                catch (Exception ex)
                {
                    // 3단계: 예외 발생 여부
                    Debug.WriteLine($"  Boolean 예외: {ex.Message}");
                }
            }


            var result = candidates.Where(e => IsTouching(refSolid, e))
                             .Select(e => e.Id.Value)
                             .ToList();

            //Debug.WriteLine($"result: {result[0]}");

            return result;
        }

        private bool IsTouching(Solid refSolid, Element candidate)
        {
            var candidateSolid = GetSolid(candidate);
            if (candidateSolid == null) return false;

            try
            {
                var intersection = BooleanOperationsUtils.ExecuteBooleanOperation(
                    refSolid,
                    candidateSolid,
                    BooleanOperationsType.Intersect);

                if (intersection == null) return false;

                return intersection.Volume > 1e-6
                    || intersection.Faces.Size > 0;
            }
            catch
            {
                return false;
            }
        }

        private Solid? GetSolid(Element elem)
        {
            var solids = elem.get_Geometry(new Options { ComputeReferences = true })
                                .OfType<Solid>()
                                .Where(s => s.Volume > 1e-6)
                                .ToList();

            if (!solids.Any()) return null;
            if (solids.Count == 1) return solids.FirstOrDefault();

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
    }
}
