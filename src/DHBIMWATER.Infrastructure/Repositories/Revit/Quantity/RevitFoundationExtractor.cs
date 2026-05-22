using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using System.Windows.Controls;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitFoundationExtractor : IQuantityExtractor
    {

        private readonly Func<Document?> _doc;

        public RevitFoundationExtractor(Func<Document?> doc)
        {
            _doc = doc;
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
            if (doc == null)
                return Enumerable.Empty<QuantityItem>();

            var fnd = doc.GetElement(new ElementId(elementId)) as FamilyInstance;
            var quantityItems = new List<QuantityItem>();

            // 구조 기초 슬래브는 Floor 에서 산출
            if (fnd is Floor) return Enumerable.Empty<QuantityItem>();

            // 객체 추출값
            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(fnd, "폭") ?? FamilyInstanceHelper.FindParameter(fnd, "b") ?? 0);
            var d = UC.FtToM(FamilyInstanceHelper.FindParameter(fnd, "길이") ?? FamilyInstanceHelper.FindParameter(fnd, "d") ?? 0);
            var h = UC.FtToM(FamilyInstanceHelper.FindParameter(fnd, "기초 두께") ?? FamilyInstanceHelper.FindParameter(fnd, "h") ?? 0);

            string typeName = fnd.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ?? string.Empty;

            var varDict = new Dictionary<string, double>
            {
                ["B"] = b,
                ["D"] = d,
                ["H"] = h,
            };

            const string concFormula = "B x D x H";
            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            double concValue = FormulaCalculator.Calculate(concFormula, varDict);

            // 철근콘크리트
            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = fnd.Category.Name ?? string.Empty,
                ElementCode = fnd.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = typeName,
                RawFormula = concFormula,
                RenderedFormula = concRendered,
                Value = concValue,
                Unit = "m³"
            };

            var listToAdd = new List<QuantityItem>() { concreteItem, };
            quantityItems.AddRange(listToAdd);

            return quantityItems;
        }
    }
}