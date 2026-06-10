using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using DHBIMWATER.Infrastructure.Repositories.Revit.Geometry;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using System.Diagnostics;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;


namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitBeamExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;
        private readonly IFaceClassifier _classifier;

        public RevitBeamExtractor(Func<Document?> doc, IIntersectingElementFinder finder, IFaceClassifier classifier)
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

            return elem is FamilyInstance fi && fi.Category.Id.Value == (int)BuiltInCategory.OST_StructuralFraming;
        }
        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .WhereElementIsNotElementType()
                        .Select(r => r.Id.Value);
        }
        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<QuantityItem>();

            var beam = doc.GetElement(new ElementId(elementId)) as FamilyInstance;
            if (beam == null) return Enumerable.Empty<QuantityItem>();

            var quantityItems = new List<QuantityItem>();
            var refFaceDict = _classifier.GetFaceAreas(elementId);
            var deductionByFaceType = QuantityExtractorHelper.GroupDeductions(_finder.FindContactAreas(elementId));

            // 객체 추출값
            var length = UC.FtToM(beam.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM)?.AsDouble() ?? 0);
            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(beam, "b") ?? FamilyInstanceHelper.FindParameter(beam, "width") ?? FamilyInstanceHelper.FindParameter(beam, "폭") ?? 0);
            var h = UC.FtToM(FamilyInstanceHelper.FindParameter(beam, "h") ?? FamilyInstanceHelper.FindParameter(beam, "d") ?? FamilyInstanceHelper.FindParameter(beam, "높이") ?? FamilyInstanceHelper.FindParameter(beam, "Height") ?? 0);

            var lc = beam.Location as LocationCurve;
            var beamDirection = (lc.Curve.GetEndPoint(1) - lc.Curve.GetEndPoint(0)).Normalize();

            var splitSolids = RevitGeometryHelper.GetSolids(beam)
                .SelectMany(s => { try { return SolidUtils.SplitVolumes(s); } catch { return [s]; } })
                .Where(s => s.Volume > 1e-9)
                .ToList();

            // 유효 길이: 각 SplitSolid에서 보 방향 최장 Edge 합산
            double effectiveLength = length;
            double totalLength = splitSolids
                .Select(solid => solid.Edges.Cast<Edge>()
                    .Select(e => e.AsCurve()).OfType<Line>()
                    .Where(l => Math.Abs(((l.GetEndPoint(1) - l.GetEndPoint(0)).Normalize()).DotProduct(beamDirection)) > 0.99)
                    .Select(l => l.Length)
                    .DefaultIfEmpty(0).Max())
                .Sum();
            if (totalLength > 0) effectiveLength = UC.FtToM(totalLength);

            // 단면적: 첫 SplitSolid 중앙을 beamDirection 법선으로 자른 단면
            double actualCrossSection = 0;
            var firstSolid = splitSolids.FirstOrDefault();
            if (firstSolid != null)
                actualCrossSection = UC.Ft2ToM2(RevitGeometryHelper.GetMidSectionArea(firstSolid, beamDirection));

            string materialName = string.Empty;
            var materialId = FamilyInstanceHelper.GetMaterialId(beam);

            if (materialId == null || materialId == ElementId.InvalidElementId)
                materialName = string.Empty;
            else
                materialName = (doc.GetElement(materialId) as Material).Name;

            var materialClass = FamilyInstanceHelper.GetStructuralAssetClass(beam);
            var workType = materialClass switch
            {
                StructuralAssetClass.Concrete => "철근콘크리트",
                StructuralAssetClass.Metal => "강재",
                StructuralAssetClass.Generic => "기타",
                _ => "미분류"
            };

            // B x D 가 실제 단면적 A와 5% 이내 일치할 때만 B x D x L, 아니면 A x L
            const double tolerance = 0.05;
            bool useDimensions = b > 0 && h > 0
                && actualCrossSection > 0
                && Math.Abs(b * h - actualCrossSection) / actualCrossSection < tolerance;

            string concFormula;
            Dictionary<string, double> varDict;

            if (useDimensions)
            {
                concFormula = "B x D x L";
                varDict = new Dictionary<string, double>
                {
                    ["B"] = b,
                    ["D"] = h,
                    ["L"] = effectiveLength,
                };
            }
            else
            {
                concFormula = "A x L";
                varDict = new Dictionary<string, double>
                {
                    ["A"] = actualCrossSection,
                    ["L"] = effectiveLength,
                };
            }

            double volumeM3 = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(beam).Sum(s => s.Volume));
            string concRendered = FormulaCalculator.Render(concFormula, varDict);

            switch (workType)
            {
                case "철근콘크리트":
                    var concreteItem = new QuantityItem
                    {
                        ElementId = elementId,
                        Category = beam.Category.Name ?? string.Empty,
                        ElementCode = beam.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                        WorkType = workType,
                        Specification = materialName,
                        RawFormula = concFormula,
                        RenderedFormula = concRendered,
                        Value = volumeM3,
                        Unit = "m³"
                    };
                    quantityItems.Add(concreteItem);
                    // 거푸집 - 각 FaceType별로 항목 생성
                    var formworkFaces = new[] { FaceType.Bottom, FaceType.Left, FaceType.Right, FaceType.End };

                    foreach (var faceType in formworkFaces)
                    {
                        var grossArea = refFaceDict.GetValueOrDefault(faceType, 0);
                        if (grossArea < 0.001) continue; // 면적이 없으면 skip

                        var deducts = deductionByFaceType.TryGetValue(faceType, out var dl) ? dl : null;
                        var netArea = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, faceType);
                        var rawFormula = QuantityExtractorHelper.GetDeductionRawFormula(refFaceDict, deductionByFaceType, faceType);
                        var renderedFormula = QuantityExtractorHelper.GetDeductionRenderedFormula(refFaceDict, deductionByFaceType, faceType);

                        var spec = faceType switch
                        {
                            FaceType.Bottom => "합판4회",      // 추후 세팅값으로 연동
                            FaceType.Left => "합판3회",
                            FaceType.Right => "합판3회",
                            FaceType.End => "합판3회",
                            _ => throw new ArgumentOutOfRangeException(),
                        };

                        var formworkItem = new QuantityItem
                        {
                            ElementId = elementId,
                            Category = beam.Category.Name ?? string.Empty,
                            ElementCode = beam.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
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
                    break;
                case "강재":
                    var steelFormula = concFormula + " x UW";
                    var steelVarDict = new Dictionary<string, double>(varDict) { ["UW"] = 7.850 };
                    var steelItem = new QuantityItem
                    {
                        ElementId = elementId,
                        Category = beam.Category.Name ?? string.Empty,
                        ElementCode = beam.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                        WorkType = workType,
                        Specification = materialName,
                        RawFormula = steelFormula,
                        RenderedFormula = FormulaCalculator.Render(steelFormula, steelVarDict),
                        Value = volumeM3 * 7.850,
                        Unit = "ton"
                    };
                    quantityItems.Add(steelItem);
                    break;
                default:
                    quantityItems.Add(new QuantityItem
                    {
                        ElementId = elementId,
                        Category = beam.Category.Name ?? string.Empty,
                        ElementCode = beam.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                        WorkType = workType,
                        Specification = materialName,
                        RawFormula = concFormula,
                        RenderedFormula = concRendered,
                        Value = volumeM3,
                        Unit = "m³"
                    });
                    break;
            }

            return quantityItems;
        }

    }
}
