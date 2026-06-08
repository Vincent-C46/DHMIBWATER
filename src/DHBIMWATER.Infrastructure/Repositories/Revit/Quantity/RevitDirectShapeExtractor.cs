using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using DocumentFormat.OpenXml.Bibliography;
using System.Data.Common;
using System.Reflection.Metadata.Ecma335;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitDirectShapeExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;
        private readonly IFaceClassifier _classifier;

        public RevitDirectShapeExtractor(Func<Document?> doc, IIntersectingElementFinder finder, IFaceClassifier classifier)
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

            return elem is DirectShape;
        }
        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
            .OfClass(typeof(DirectShape))
            .WhereElementIsNotElementType()
            .Select(r => r.Id.Value);
        }
        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null)
                return Enumerable.Empty<QuantityItem>();

            var ds = doc.GetElement(new ElementId(elementId)) as DirectShape;
            if (ds == null) return Enumerable.Empty<QuantityItem>();

            var quantityItems = new List<QuantityItem>();

            IReadOnlyDictionary<FaceType, double> refFaceDict = _classifier.GetFaceAreas(elementId);

            //var solid = RevitGeometryHelper.GetSolid(ds);
            var volume = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(ds).Sum(s => s.Volume));
            //TaskDialog.Show("success", $"{refFaceDict.Keys.FirstOrDefault().ToString()}");

            var varDict = new Dictionary<string, double>
            {
                ["V"] = volume,
            };

            // 콘크리트
            var concFormula = "V";
            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            //double concValue = FormulaCalculator.Calculate(concFormula, varDict);
            double concValue = volume;

            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = ds.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                ElementCode = ds.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = "",
                RawFormula = concFormula,
                RenderedFormula = concRendered,
                Value = concValue,
                Unit = "m³"
            };

            //var formFormula = "A";
            //string? formRendered = FormulaCalculator.Render(formFormula, varDict);
            //double formValue = FormulaCalculator.Calculate(formFormula, varDict);
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
