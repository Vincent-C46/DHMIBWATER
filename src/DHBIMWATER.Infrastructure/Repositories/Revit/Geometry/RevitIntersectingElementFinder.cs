using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Storage;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Core.Settings;
using DHBIMWATER.Infrastructure.Helpers;
using System.Diagnostics;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Geometry
{
    public class RevitIntersectingElementFinder : IIntersectingElementFinder
    {
        private readonly Func<Document?> _doc;
        private readonly Dictionary<RevitCategory, List<RevitCategory>> _categoryMatrix;
        private const double Epsilon = 0.01; // feet 단위 0.01ft = 약 3mm
        private const double SolidThk = 0.01;
        public RevitIntersectingElementFinder(Func<Document?> doc, IQuantitySettingsRepository settingsRepo)
        {
            _doc = doc;
            _categoryMatrix = settingsRepo.Load()?.Deduction.CategoryMatrix
                ?? new DeductionSettings().CategoryMatrix;
        }

        public IReadOnlyList<FaceDeduction> FindContactAreas(long refElemId)
        {
            var doc = _doc();
            if (doc == null) return new List<FaceDeduction>();

            var refElem = doc.GetElement(new ElementId(refElemId));
            if (refElem == null) return new List<FaceDeduction>();
            var refSolids = GetSplitSolids(refElem);
            var candidates = FindCandidates(doc, refElem, refElemId);
            if (!refSolids.Any() || !candidates.Any()) return new List<FaceDeduction>();

            var contacts = new List<FaceDeduction>();

            foreach (var candidate in candidates)
            {
                var candidateSolids = GetSplitSolids(candidate);
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
                        if (!IsOpposingAndAdjacent(refNormal, refOrigin, planarCand)) continue;

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

        /// <summary>공제가 반영된 면 인스턴스별 얇은 Solid. Application 계층에는 노출하지 않는다.</summary>
        internal IReadOnlyList<(FaceType FaceType, Solid NetSolid)> ComputeNetFaceSolids(long elementId)
        {
            var doc = _doc();
            if (doc == null) return new List<(FaceType, Solid)>();

            var element = doc.GetElement(new ElementId(elementId));
            if (element == null) return new List<(FaceType, Solid)>();

            var candidates = FindCandidates(doc, element, elementId);
            var results = new List<(FaceType, Solid)>();
            foreach (var referenceSolid in GetSplitSolids(element))
            foreach (var face in referenceSolid.Faces.OfType<PlanarFace>())
            {
                var faceType = RevitFaceClassifier.Classify(element, face);
                if (faceType is FaceType.None or FaceType.OpeningSide) continue;

                Solid netSolid;
                try
                {
                    netSolid = CreateExtrusionSolid(face, SolidThk, GetOrCreateFaceTypeMaterial(doc, faceType));
                }
                catch { continue; }

                foreach (var candidate in candidates)
                foreach (var candidateSolid in GetSplitSolids(candidate))
                {
                    var isContactFace = candidateSolid.Faces
                        .OfType<PlanarFace>()
                        .Any(candidateFace => IsOpposingAndAdjacent(face.FaceNormal, face.Origin, candidateFace));
                    if (!isContactFace) continue;

                    try
                    {
                        var intersecting = BooleanOperationsUtils.ExecuteBooleanOperation(
                            candidateSolid, netSolid, BooleanOperationsType.Intersect);
                        if (intersecting == null || intersecting.Volume <= 1e-10) continue;
                        netSolid = BooleanOperationsUtils.ExecuteBooleanOperation(
                            netSolid, intersecting, BooleanOperationsType.Difference);
                    }
                    catch { continue; }
                }

                if (netSolid.Volume > 1e-10)
                    results.Add((faceType, netSolid));
            }

            return results;
        }

        private static List<Solid> GetSplitSolids(Element element) => RevitGeometryHelper.GetSolids(element)
            .SelectMany(s => { try { return SolidUtils.SplitVolumes(s); } catch { return [s]; } })
            .Where(s => s.Volume > 1e-9)
            .ToList();

        private List<Element> FindCandidates(Document doc, Element referenceElement, long referenceElementId)
        {
            var bbox = referenceElement.get_BoundingBox(null);
            if (bbox == null || referenceElement.Category == null) return new List<Element>();

            var outline = new Outline(
                new XYZ(bbox.Min.X - Epsilon, bbox.Min.Y - Epsilon, bbox.Min.Z - Epsilon),
                new XYZ(bbox.Max.X + Epsilon, bbox.Max.Y + Epsilon, bbox.Max.Z + Epsilon));
            var targetCategories = GetTargetCategories((BuiltInCategory)referenceElement.Category.Id.Value);
            if (targetCategories.Count == 0) return new List<Element>();

            return new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .WherePasses(new ElementMulticategoryFilter(targetCategories))
                .WherePasses(new BoundingBoxIntersectsFilter(outline))
                .Where(e => e.Id.Value != referenceElementId)
                .ToList();
        }

        private static bool IsOpposingAndAdjacent(XYZ referenceNormal, XYZ referenceOrigin, PlanarFace candidateFace)
        {
            if (referenceNormal.DotProduct(candidateFace.FaceNormal) > -0.9) return false;
            return Math.Abs((candidateFace.Origin - referenceOrigin).DotProduct(referenceNormal)) <= Epsilon;
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
        // 설정창(면적 공제 탭)의 CategoryMatrix에서 호스트별 인접 카테고리를 조회.
        // 매트릭스에 없는 카테고리(GenericModel 등 참조 대상)는 FallbackCategories 사용.
        private ICollection<BuiltInCategory> GetTargetCategories(BuiltInCategory refCategory) =>
            _categoryMatrix.TryGetValue((RevitCategory)(int)refCategory, out var adjacent)
                ? adjacent.Select(c => (BuiltInCategory)(int)c).ToList()
                : FallbackCategories;
        private static Solid CreateExtrusionSolid(Face face, double thickness, ElementId? materialId = null)
        {
            var curveLoops = face.GetEdgesAsCurveLoops().FirstOrDefault();
            var faceNormal = face.ComputeNormal(new UV(0.5, 0.5));
            var solidOptions = new SolidOptions(materialId ?? ElementId.InvalidElementId, ElementId.InvalidElementId);
            var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new[] { curveLoops }, faceNormal, thickness, solidOptions);

            return solid;
        }

        private static ElementId GetOrCreateFaceTypeMaterial(Document doc, FaceType faceType)
        {
            var (name, color) = faceType switch
            {
                FaceType.Top => ("DH_순면적_Top", new Color(255, 215, 0)),
                FaceType.Bottom => ("DH_순면적_Bottom", new Color(255, 140, 0)),
                FaceType.Left => ("DH_순면적_Left", new Color(0, 120, 215)),
                FaceType.Right => ("DH_순면적_Right", new Color(0, 180, 0)),
                FaceType.End => ("DH_순면적_End", new Color(160, 32, 240)),
                _ => ("DH_순면적_Side", new Color(150, 150, 150)),
            };

            var materials = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .ToList();
            var existing = materials.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing.Id;

            var baseMaterial = materials.FirstOrDefault();
            if (baseMaterial == null) return ElementId.InvalidElementId;

            var created = baseMaterial.Duplicate(name) as Material;
            if (created == null) return ElementId.InvalidElementId;
            created.Color = color;
            return created.Id;
        }
    }
}
