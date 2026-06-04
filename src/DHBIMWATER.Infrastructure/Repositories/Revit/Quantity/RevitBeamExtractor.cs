using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using DHBIMWATER.Infrastructure.Repositories.Revit.Geometry;
using System.Diagnostics;
using System.Reflection;
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
            _doc        = doc;
            _finder     = finder;
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
            if (doc == null)
                return Enumerable.Empty<QuantityItem>();

            var beam = doc.GetElement(new ElementId(elementId)) as FamilyInstance;
            if (beam == null)
                return Enumerable.Empty<QuantityItem>();

            var grossAreas = _classifier.GetFaceAreas(elementId);
            var deductions = _finder.FindContactAreas(elementId);

            // FaceType별 공제 면적 그룹화
            var deductionByFaceType = deductions
                .GroupBy(d => d.FaceType)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 순 면적 계산
            double GetNetArea(FaceType faceType)
            {
                var gross = grossAreas.GetValueOrDefault(faceType, 0);
                var deductTotal = deductionByFaceType.GetValueOrDefault(faceType, new List<(FaceType, long, double)>())
                                                     .Sum(d => d.Item3);
                return Math.Max(0, gross - deductTotal);
            }

            // 공제 상세 문자열 생성 (Formula용)
            string GetDeductionFormula(FaceType faceType)
            {
                var gross = grossAreas.GetValueOrDefault(faceType, 0);
                if (!deductionByFaceType.ContainsKey(faceType) || gross == 0) 
                    return $"{gross:F3}";
                
                var deductParts = deductionByFaceType[faceType]
                    .Select(d => $"{d.Item3:F3} (Id:{d.Item2})")
                    .ToList();
                
                return $"{gross:F3} - " + string.Join(" - ", deductParts);
            }

            var quantityItems = new List<QuantityItem>();

            // 객체 추출값
            var length = UC.FtToM(beam.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM)?.AsDouble() ?? 0);

            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(beam, "b") ?? FamilyInstanceHelper.FindParameter(beam, "width") ?? FamilyInstanceHelper.FindParameter(beam, "폭") ??0) ;
            var h = UC.FtToM(FamilyInstanceHelper.FindParameter(beam, "h") ?? FamilyInstanceHelper.FindParameter(beam, "d") ?? FamilyInstanceHelper.FindParameter(beam, "높이") ?? FamilyInstanceHelper.FindParameter(beam, "Height") ?? 0) ;
            string typeName = beam.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ?? string.Empty;

            string materialName = string.Empty;
            var materialId = beam.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId()
                                ?? beam.Document.GetElement(beam.GetTypeId())?.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();

            if (materialId == null || materialId == ElementId.InvalidElementId)
                materialName = string.Empty;
            else
                materialName = (doc.GetElement(materialId) as Material).Name;

            var varDict = new Dictionary<string, double>
            {
                ["B"] = b,
                ["H"] = h,
                ["L"] = length,
            };

            const string concFormula = "B x H x L";
            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            //double concValue = FormulaCalculator.Calculate(concFormula, varDict);
            double concValue = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(beam).Sum(s => s.Volume));

            // 철근콘크리트
            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = beam.Category.Name ?? string.Empty,
                ElementCode = beam.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = materialName,
                RawFormula = concFormula,
                RenderedFormula = concRendered,
                Value = concValue,
                Unit = "m³"
            };

            quantityItems.Add(concreteItem);

            // 거푸집 - 각 FaceType별로 항목 생성
            var formworkFaces = new[] { FaceType.Bottom, FaceType.Left, FaceType.Right, FaceType.End };
            
            foreach (var faceType in formworkFaces)
            {
                var grossArea = grossAreas.GetValueOrDefault(faceType, 0);
                if (grossArea < 0.001) continue; // 면적이 없으면 skip

                var netArea = GetNetArea(faceType);
                var formula = GetDeductionFormula(faceType);

                var formworkItem = new QuantityItem
                {
                    ElementId = elementId,
                    Category = beam.Category.Name ?? string.Empty,
                    ElementCode = beam.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                    WorkType = "거푸집",
                    Specification = $"{faceType}면",
                    RawFormula = formula,
                    RenderedFormula = formula,
                    Value = netArea,
                    Unit = "m²"
                };

                quantityItems.Add(formworkItem);
            }

            return quantityItems;
        }
    }
}
