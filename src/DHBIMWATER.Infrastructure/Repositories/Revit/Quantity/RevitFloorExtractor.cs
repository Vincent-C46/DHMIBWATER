using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using System.Data.Common;
using System.Reflection.Metadata.Ecma335;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitFloorExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;
        private readonly IFaceClassifier _classifier;

        public RevitFloorExtractor(Func<Document?> doc, IIntersectingElementFinder finder, IFaceClassifier classifier)
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
            //TaskDialog.Show("Debug", elem?.GetType().Name ?? "null");

            return elem is Floor;
        }
        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
            .OfClass(typeof(Floor))
            .WhereElementIsNotElementType()
            .Select(r => r.Id.Value);
        }
        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<QuantityItem>();

            var floor = (Floor)doc.GetElement(new ElementId(elementId));
            if (floor == null) return Enumerable.Empty<QuantityItem>();

            var quantityItems = new List<QuantityItem>();
            var refFaceDict = _classifier.GetFaceAreas(elementId);
            var deductionByFaceType = QuantityExtractorHelper.GroupDeductions(_finder.FindContactAreas(elementId));

            // 객체 추출값
            var cs = floor.FloorType.GetCompoundStructure();
            var area = UC.Ft2ToM2(floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble());
            double thickness = UC.FtToM(cs.GetLayers()
                                          .Where(l => l.Function == MaterialFunctionAssignment.Structure)
                                          .Sum(l => l.Width));
            var structureLayer = cs.GetLayers().FirstOrDefault(l => l.Function == MaterialFunctionAssignment.Structure);

            // 재료
            var material = doc.GetElement(structureLayer?.MaterialId) as Material;
            var materialName = material?.Name ?? string.Empty;

            var varDict = new Dictionary<string, double>
            {
                ["A"] = area,
                ["Thk"] = thickness,
            };

            // 콘크리트
            string plainConcreteName = "무근콘크리트";
            string reinforcedConcreteName = "철근콘크리트";

            var concFormula = "A x Thk";
            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            //double concValue = FormulaCalculator.Calculate(concFormula, varDict);
            double concValue = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(floor).Sum(s => s.Volume)); // 실제값은 Solid 체적으로 대체
            string concWorkType = (thickness < 0.15 || materialName.Contains("무근")) ? plainConcreteName : reinforcedConcreteName;

            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = floor.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                ElementCode = floor.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = concWorkType,
                Specification = materialName,
                RawFormula = concFormula,
                RenderedFormula = concRendered,
                Value = concValue,
                Unit = "m³"
            };

            // 스페이서
            var spacerFormula = "A";
            string? spacerRendered = FormulaCalculator.Render(spacerFormula, varDict);
            double spacerValue = FormulaCalculator.Calculate(spacerFormula, varDict);

            var spacerItem = new QuantityItem
            {
                ElementId = elementId,
                Category = floor.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                ElementCode = floor.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "스페이서",
                Specification = "수평",
                RawFormula = spacerFormula,
                RenderedFormula = spacerRendered,
                Value = spacerValue,
                Unit = "m²"
            };
            if (concWorkType != plainConcreteName) quantityItems.Add(spacerItem);

            //var formFormula = "A";
            //string? formRendered = FormulaCalculator.Render(formFormula, varDict);
            //double formValue = FormulaCalculator.Calculate(formFormula, varDict);

            // 거푸집 - 각 FaceType별로 항목 생성
            var formworkFaces = new[] { FaceType.Bottom, FaceType.Side };

            foreach (var faceType in formworkFaces)
            {
                var grossArea = refFaceDict.GetValueOrDefault(faceType, 0);
                if (grossArea < 0.001) continue; // 면적이 없으면 skip

                var netArea = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, faceType);
                var formFormula = QuantityExtractorHelper.GetDeductionFormula(refFaceDict, deductionByFaceType, faceType);

                var spec = faceType switch
                {
                    FaceType.Bottom => "슬래브하부",
                    FaceType.Side => "슬래브옆면",
                    _ => throw new ArgumentOutOfRangeException(),
                };

                var formworkItem = new QuantityItem
                {
                    ElementId = elementId,
                    Category = floor.Category.Name ?? string.Empty,
                    ElementCode = floor.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                    WorkType = "거푸집",
                    Specification = spec,
                    RawFormula = formFormula,
                    RenderedFormula = formFormula,
                    Value = netArea,
                    Unit = "m²"
                };

                if (formworkItem.Value > 1e-6) quantityItems.Add(formworkItem);
            }

            var listToAdd = new List<QuantityItem>() { concreteItem, };
            quantityItems.AddRange(listToAdd);

            return quantityItems;
        }
    }
}
