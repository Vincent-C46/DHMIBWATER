using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitFoundationExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;
        private readonly IFaceClassifier _classifier;

        public RevitFoundationExtractor(Func<Document?> doc, IIntersectingElementFinder finder, IFaceClassifier classifier)
        {
            _doc = doc;
            _finder = finder;
            _classifier = classifier;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;
            var elem = doc.GetElement(new ElementId(elementId));
            return elem is FamilyInstance fi && fi.Category.Id.Value == (int)BuiltInCategory.OST_StructuralFoundation;
        }

        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                        .WhereElementIsNotElementType()
                        .Select(r => r.Id.Value);
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<QuantityItem>();

            var fnd = doc.GetElement(new ElementId(elementId)) as FamilyInstance;
            if (fnd == null) return Enumerable.Empty<QuantityItem>();

            // 구조 기초 슬래브는 Floor 에서 산출
            if (fnd is Floor) return Enumerable.Empty<QuantityItem>();

            var quantityItems = new List<QuantityItem>();
            var refFaceDict = _classifier.GetFaceAreas(elementId);
            var deductionByFaceType = QuantityExtractorHelper.GroupDeductions(_finder.FindContactAreas(elementId));

            var materialId = fnd.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();
            if (materialId == null || materialId == ElementId.InvalidElementId)
                materialId = fnd.Symbol.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();

            string materialName = (materialId != null && materialId != ElementId.InvalidElementId)
                ? (doc.GetElement(materialId) as Material)?.Name ?? string.Empty
                : string.Empty;

            // 객체 추출값
            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(fnd, "b") ??
                             FamilyInstanceHelper.FindParameter(fnd, "w") ??
                             FamilyInstanceHelper.FindParameter(fnd, "width") ??
                             FamilyInstanceHelper.FindParameter(fnd, "폭") ?? 0);
            var d = UC.FtToM(FamilyInstanceHelper.FindParameter(fnd, "d") ??
                             FamilyInstanceHelper.FindParameter(fnd, "l") ??
                             FamilyInstanceHelper.FindParameter(fnd, "Length") ??
                             FamilyInstanceHelper.FindParameter(fnd, "길이") ?? 0);
            var h = UC.FtToM(FamilyInstanceHelper.FindParameter(fnd, "h") ??
                             FamilyInstanceHelper.FindParameter(fnd, "Thickness") ??
                             FamilyInstanceHelper.FindParameter(fnd, "Thk") ??
                             FamilyInstanceHelper.FindParameter(fnd, "두께") ??
                             FamilyInstanceHelper.FindParameter(fnd, "기초 두께") ??
                             FamilyInstanceHelper.FindParameter(fnd, "Height") ?? 0);

            var varDict = new Dictionary<string, double>
            {
                ["B"] = b,
                ["D"] = d,
                ["H"] = h,
            };

            const string concFormula = "B x D x H";
            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            double concValue = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(fnd).Sum(s => s.Volume));
            string workType = materialName.Contains("무근") || h < 0.15 ? "무근콘크리트" : "철근콘크리트";

            quantityItems.Add(new QuantityItem
            {
                ElementId = elementId,
                Category = fnd.Category.Name ?? string.Empty,
                ElementCode = fnd.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = workType,
                Specification = materialName,
                RawFormula = concFormula,
                RenderedFormula = concRendered,
                Value = concValue,
                Unit = "m³"
            });

            // 거푸집 - 측면 (독립기초 노출면)
            var formworkFaces = new[] { FaceType.Side };

            foreach (var faceType in formworkFaces)
            {
                var grossArea = refFaceDict.GetValueOrDefault(faceType, 0);
                if (grossArea < 0.001) continue;

                var deducts = deductionByFaceType.TryGetValue(faceType, out var dl) ? dl : null;
                var netArea = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, faceType);
                var rawFormula = QuantityExtractorHelper.GetDeductionRawFormula(refFaceDict, deductionByFaceType, faceType);
                var renderedFormula = QuantityExtractorHelper.GetDeductionRenderedFormula(refFaceDict, deductionByFaceType, faceType);

                var spec = workType == "무근콘크리트" ? "합판6회" : "합판4회";

                var formworkItem = new QuantityItem
                {
                    ElementId = elementId,
                    Category = fnd.Category.Name ?? string.Empty,
                    ElementCode = fnd.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                    WorkType = "거푸집",
                    Specification = spec,
                    RawFormula = rawFormula,
                    RenderedFormula = renderedFormula,
                    Value = netArea,
                    Unit = "m²",
                    GrossValue = deducts != null ? grossArea : null,
                    Deductions = deducts,
                };

                if (formworkItem.Value > 1e-6) quantityItems.Add(formworkItem);
            }

            return quantityItems;
        }
    }
}
