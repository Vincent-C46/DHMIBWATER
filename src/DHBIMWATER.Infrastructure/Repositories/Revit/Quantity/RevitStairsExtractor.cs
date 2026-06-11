using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitStairsExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;
        private readonly IFaceClassifier _classifier;

        public RevitStairsExtractor(Func<Document?> doc, IIntersectingElementFinder finder, IFaceClassifier classifier)
        {
            _doc = doc;
            _finder = finder;
            _classifier = classifier;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;
            return doc.GetElement(new ElementId(elementId)) is Stairs;
        }

        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_Stairs)
                        .WhereElementIsNotElementType()
                        .Select(r => r.Id.Value);
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<QuantityItem>();

            var stair = doc.GetElement(new ElementId(elementId)) as Stairs;
            if (stair == null) return Enumerable.Empty<QuantityItem>();

            var quantityItems = new List<QuantityItem>();

            string materialName = string.Empty;
            var materialId = FamilyInstanceHelper.GetMaterialId(stair);
            if (materialId != null && materialId != ElementId.InvalidElementId)
                materialName = (doc.GetElement(materialId) as Material)?.Name ?? string.Empty;

            var materialClass = FamilyInstanceHelper.GetStructuralAssetClass(stair);
            var workType = materialClass switch
            {
                StructuralAssetClass.Concrete => "철근콘크리트",
                StructuralAssetClass.Metal    => "강재",
                StructuralAssetClass.Generic  => "기타",
                _ => "미분류"
            };

            if (workType != "철근콘크리트") return quantityItems;

            // 콘크리트 수량
            double volumeM3 = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(stair).Sum(s => s.Volume));
            var concVarDict = new Dictionary<string, double> { ["V"] = volumeM3 };
            string concFormula = "V";

            quantityItems.Add(new QuantityItem
            {
                ElementId        = elementId,
                Category         = stair.Category.Name ?? "계단",
                ElementCode      = stair.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType         = workType,
                Specification    = materialName,
                RawFormula       = concFormula,
                RenderedFormula  = FormulaCalculator.Render(concFormula, concVarDict),
                Value            = volumeM3,
                Unit             = "m³"
            });

            // 거푸집 — 하부면(경사 소피트 포함) + 측면
            var refFaceDict = _classifier.GetFaceAreas(elementId);
            var deductionByFaceType = QuantityExtractorHelper.GroupDeductions(_finder.FindContactAreas(elementId));

            foreach (var faceType in new[] { FaceType.Bottom, FaceType.Side })
            {
                var grossArea = refFaceDict.GetValueOrDefault(faceType, 0);
                if (grossArea < 0.001) continue;

                var deducts      = deductionByFaceType.TryGetValue(faceType, out var dl) ? dl : null;
                var netArea      = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, faceType);
                var rawFormula   = QuantityExtractorHelper.GetDeductionRawFormula(refFaceDict, deductionByFaceType, faceType);
                var renderedFormula = QuantityExtractorHelper.GetDeductionRenderedFormula(refFaceDict, deductionByFaceType, faceType);

                var spec = faceType switch
                {
                    FaceType.Bottom => "합판4회",
                    FaceType.Side   => "합판3회",
                    _ => throw new ArgumentOutOfRangeException(),
                };

                var formworkItem = new QuantityItem
                {
                    ElementId        = elementId,
                    Category         = stair.Category.Name ?? "계단",
                    ElementCode      = stair.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                    WorkType         = "거푸집",
                    Specification    = spec,
                    RawFormula       = rawFormula,
                    RenderedFormula  = renderedFormula,
                    Value            = netArea,
                    Unit             = "m²",
                    GrossValue       = deducts != null ? grossArea : null,
                    Deductions       = deducts,
                };

                if (formworkItem.Value > 1e-6) quantityItems.Add(formworkItem);
            }

            return quantityItems;
        }
    }
}
