using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using System.Windows;
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

            string materialName = string.Empty;

            var materialId = fnd.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();

            if (materialId == null || materialId == ElementId.InvalidElementId)
                materialId = fnd.Symbol.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();

            materialName = (materialId != null && materialId != ElementId.InvalidElementId) ? 
                (doc.GetElement(materialId) as Material)?.Name ?? string.Empty
                : string.Empty;


            // 구조 기초 슬래브는 Floor 에서 산출
            if (fnd is Floor) return Enumerable.Empty<QuantityItem>();

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

            string typeName = fnd.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ?? string.Empty;

            var varDict = new Dictionary<string, double>
            {
                ["B"] = b,
                ["D"] = d,
                ["H"] = h,
            };

            const string concFormula = "B x D x H";
            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            //double concValue = FormulaCalculator.Calculate(concFormula, varDict);
            double concValue = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(fnd).Sum(s => s.Volume));
            string workType = materialName.Contains("무근") || h < 0.15 ? "무근콘크리트" : "철근콘크리트";

            // 철근콘크리트
            var concreteItem = new QuantityItem
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
            };

            var listToAdd = new List<QuantityItem>() { concreteItem, };
            quantityItems.AddRange(listToAdd);

            return quantityItems;
        }
    }
}