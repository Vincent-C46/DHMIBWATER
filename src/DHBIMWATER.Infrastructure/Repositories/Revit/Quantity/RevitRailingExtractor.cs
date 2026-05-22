using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using System.Diagnostics;
using System.Windows.Media;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitRailingExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;

        public RevitRailingExtractor(Func<Document?> doc)
        {
            _doc = doc;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;
            var elem = doc.GetElement(new ElementId(elementId));
            return elem is Railing;
        }

        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_StairsRailing)
                        .WhereElementIsNotElementType()
                        .Select(r => r.Id.Value);
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null)
                return Enumerable.Empty<QuantityItem>();

            var listToAdd = new List<QuantityItem>();

            var railing = doc.GetElement(new ElementId(elementId)) as Railing;

            Debug.WriteLine($"railID: {railing.Id.ToString()}");

            var dependentIds = railing.GetDependentElements(null);// 필터없음
            bool isSloped = false;

            var quantityItems = new List<QuantityItem>();

            foreach (var id in dependentIds)
            {
                var dep = doc.GetElement(id);
                if (dep?.GetType().Name == "Path3d")
                {
                    var path = dep as Path3d;
                    var curveArrArray = path.AllCurveLoops;

                    foreach (CurveArray curveArr in curveArrArray)
                    {
                        Debug.WriteLine($"Size: {curveArr.Size}");

                        for (int i = 0; i < curveArr.Size; i++)
                        {
                            Curve c = curveArr.get_Item(i);

                            double zDiff = Math.Abs(c.GetEndPoint(1).Z - c.GetEndPoint(0).Z);

                            if (zDiff > 0.001) isSloped = true;
                            else isSloped = false;

                            var length = UC.FtToM(c.Length);
                            string typeName = railing.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ?? string.Empty;

                            var varDict = new Dictionary<string, double>
                            {
                                ["L"] = length,
                            };

                            const string lenFormula = "L";
                            string? lenRendered = FormulaCalculator.Render(lenFormula, varDict);
                            double lenValue = FormulaCalculator.Calculate(lenFormula, varDict);

                            // 길이
                            var lenItem = new QuantityItem
                            {
                                ElementId = elementId,
                                Category = railing.Category.Name ?? string.Empty,
                                ElementCode = railing.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                                WorkType = "난간",
                                Specification = isSloped ? "경사부" : "수평부",
                                SubSpecification = typeName,
                                RawFormula = lenFormula,
                                RenderedFormula = lenRendered,
                                Value = lenValue,
                                Unit = "m"
                            };

                            listToAdd.Add(lenItem);
                        }
                    }
                }
            }

            quantityItems.AddRange(listToAdd);

            return quantityItems;
        }
    }
}
