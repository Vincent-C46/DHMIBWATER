using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
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

            var materialId = stair.GetMaterialIds(false).FirstOrDefault();
            var material = (materialId != null && materialId != ElementId.InvalidElementId)
                ? doc.GetElement(materialId) as Material
                : null;

            string materialName = material?.Name ?? string.Empty;

            StructuralAssetClass? materialClass = null;
            if (material != null)
            {
                var assetId = material.StructuralAssetId;
                if (assetId != null && assetId != ElementId.InvalidElementId)
                    materialClass = (doc.GetElement(assetId) as PropertySetElement)
                                        ?.GetStructuralAsset()?.StructuralAssetClass;
            }

            var workType = materialClass switch
            {
                StructuralAssetClass.Concrete => "철근콘크리트",
                StructuralAssetClass.Metal    => "강재",
                StructuralAssetClass.Generic  => "기타",
                _ => "미분류"
            };

            //if (workType != "철근콘크리트") return quantityItems;

            #region 콘크리트 수량
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
            #endregion

            #region 거푸집 수량
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
            #endregion

            #region 논슬립 수량
            var stairRuns = stair.GetStairsRuns();
            foreach (var runId in stairRuns)
            {
                var run = doc.GetElement(runId) as StairsRun;
                if (run == null) continue;

                var treadCount = run.ActualTreadsNumber;
                if (treadCount < 1) continue;

                var runWidth = UC.FtToM(run.ActualRunWidth);
                var nonSlipVarDict = new Dictionary<string, double>
                {
                    ["W"] = runWidth,
                    ["N"] = treadCount,
                };

                const string nonSlipFormula = "W x N";
                var nonSlipRendered = FormulaCalculator.Render(nonSlipFormula, nonSlipVarDict);
                var nonSlipLength = FormulaCalculator.Calculate(nonSlipFormula, nonSlipVarDict);

                quantityItems.Add(new QuantityItem
                {
                    ElementId        = elementId,
                    Category         = stair.Category.Name ?? "계단",
                    ElementCode      = stair.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                    WorkType         = "계단논슬립",
                    Specification    = "논슬립",
                    RawFormula       = nonSlipFormula,
                    RenderedFormula  = nonSlipRendered,
                    Value            = nonSlipLength,
                    Unit             = "m"
                });
            }

            #endregion

            return quantityItems;
        }

    }
}
