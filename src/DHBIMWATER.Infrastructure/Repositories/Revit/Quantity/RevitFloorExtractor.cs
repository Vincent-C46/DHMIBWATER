using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using System.Data.Common;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitFloorExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        public RevitFloorExtractor(Func<Document?> doc)
        {
            _doc = doc;
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
            if (doc == null)
                return Enumerable.Empty<QuantityItem>();

            var floor = (Floor)doc.GetElement(new ElementId(elementId));
            var cs = floor.FloorType.GetCompoundStructure();

            var quantityItems = new List<QuantityItem>();

            // 객체 추출값
            var area = UC.Ft2ToM2(floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble());
            double thickness = UC.FtToM(cs.GetLayers()
                                          .Where(l => l.Function == MaterialFunctionAssignment.Structure)
                                          .Sum(l => l.Width));

            var structureLayer = cs.GetLayers().FirstOrDefault(l => l.Function == MaterialFunctionAssignment.Structure);

            var material = doc.GetElement(structureLayer?.MaterialId) as Material;
            var materialName = material?.Name ?? string.Empty;

            var varDict = new Dictionary<string, double>
            {
                ["A"] = area,
                ["Thk"] = thickness,
            };

            var concFormula = "A x Thk";

            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            double concValue = FormulaCalculator.Calculate(concFormula, varDict);
            string workType = thickness < 0.15 || materialName.Contains("무근") ? "무근콘크리트" : "철근콘크리트";

            // 콘크리트
            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = floor.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                ElementCode = floor.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = workType,
                Specification = materialName,
                RawFormula = concFormula,
                RenderedFormula = concRendered,
                Value = concValue,
                Unit = "m³"
            };

            var formFormula = "A";
            string? formRendered = FormulaCalculator.Render(formFormula, varDict);
            double formValue = FormulaCalculator.Calculate(formFormula, varDict);

            //// 거푸집
            //var bottomFormItem = new QuantityItem
            //{
            //    ElementId = elementId,
            //    Category = floor.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
            //    ElementCode = floor.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
            //    WorkType = "거푸집",
            //    Specification = "합판 4회",
            //    RawFormula = formFormula,
            //    RenderedFormula = formRendered,
            //    Value = formValue,
            //    Unit = "m²"
            //};

            var listToAdd = new List<QuantityItem>() { concreteItem, };
            quantityItems.AddRange(listToAdd);

            return quantityItems;
        }
    }
}
