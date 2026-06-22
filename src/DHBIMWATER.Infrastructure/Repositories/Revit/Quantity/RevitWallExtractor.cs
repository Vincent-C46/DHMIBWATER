using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Windows.Controls;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitWallExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;
        private readonly IFaceClassifier _classifier;

        public RevitWallExtractor(Func<Document?> doc, IIntersectingElementFinder finder, IFaceClassifier classifier)
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

            return elem is Wall;
        }
        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
            .OfClass(typeof(Wall))
            .WhereElementIsNotElementType()
            .Select(r => r.Id.Value);
        }
        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<QuantityItem>();

            var wall = doc.GetElement(new ElementId(elementId)) as Wall;
            if (wall == null) return Enumerable.Empty<QuantityItem>();

            var quantityItems = new List<QuantityItem>();
            var refFaceDict = _classifier.GetFaceAreas(elementId);
            var deductionByFaceType = QuantityExtractorHelper.GroupDeductions(_finder.FindContactAreas(elementId));

            #region 벽체 정보 추출
            var cs = wall.WallType.GetCompoundStructure();
            var area = UC.Ft2ToM2(wall.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble());
            double thickness = UC.FtToM(cs.GetLayers()
                                          .Where(l => l.Function == MaterialFunctionAssignment.Structure)
                                          .Sum(l => l.Width));

            var structureLayer = cs.GetLayers().FirstOrDefault(l => l.Function == MaterialFunctionAssignment.Structure);
            var material = doc.GetElement(structureLayer?.MaterialId) as Material;
            var materialName = material?.Name ?? string.Empty;

            var lc = wall.Location as LocationCurve;
            double wallLength = lc != null ? UC.FtToM(lc.Curve.Length) : 0;
            double wallHeight = UC.FtToM(wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 0);
            int concreteJointNum = 1;   // 시공이음 개수
            double volume = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(wall).Sum(s => s.Volume)); //M3

            Dictionary<string, double> varDict = new Dictionary<string, double>
            {
                ["V"]            = volume,
                ["H"]            = wallHeight,
                ["L"]            = wallLength,
                ["A"]            = area,
                ["Thk"]          = thickness,
                ["CJ"]           = concreteJointNum,
                ["A_left_gross"]  = refFaceDict.GetValueOrDefault(FaceType.Left,  0),
                ["A_right_gross"] = refFaceDict.GetValueOrDefault(FaceType.Right, 0),
                ["A_end_gross"]   = refFaceDict.GetValueOrDefault(FaceType.End,   0),
                ["A_left_net"]    = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Left),
                ["A_right_net"]   = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Right),
                ["A_end_net"]     = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.End),
            };

            // H x L 이 A 와 5% 이내 일치하면 치수 수식, 아니면 A x Thk
            const double tolerance = 0.05;
            bool useDimensions = wallHeight > 0 && wallLength > 0
                && area > 0
                && Math.Abs(wallHeight * wallLength - area) / area < tolerance;
            #endregion

            #region 콘크리트
            string concFormula = useDimensions ? "H x L x Thk" : "A x Thk";

            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            var materialClass = FamilyInstanceHelper.GetStructuralAssetClass(wall);
            var matWorkType = materialClass switch
            {
                StructuralAssetClass.Concrete => "철근콘크리트",
                StructuralAssetClass.Metal => "강재",
                StructuralAssetClass.Generic => "기타",
                _ => "철근콘크리트"
            };
            quantityItems.Add(new QuantityItem
            {
                ElementId = elementId,
                Category = wall.Category.Name ?? string.Empty,
                ElementCode = wall.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = matWorkType,
                Specification = materialName,
                RawFormula = concFormula,
                RenderedFormula = concRendered,
                Value = volume,
                Unit = "m³"
            });
            #endregion

            #region 거푸집 및 스페이서
            if (matWorkType == "철근콘크리트")
            {
                // DH_IsExterior: 1 = 외측벽, 0 = 내측벽
                bool isExterior = wall.LookupParameter("DH_IsExterior")?.AsInteger() == 1;

                // 거푸집 - 양면(내부/외부) + 마구리
                // Right = wall.Orientation 방향 = 외측면, Left = 반대 = 내측면
                var formworkFaces = new[] { FaceType.Left, FaceType.Right, FaceType.End };

                foreach (var faceType in formworkFaces)
                {
                    var grossArea = refFaceDict.GetValueOrDefault(faceType, 0);
                    if (grossArea < 0.001) continue;

                    var deducts = deductionByFaceType.TryGetValue(faceType, out var dl) ? dl : null;
                    var netArea = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, faceType);
                    var rawFormula = QuantityExtractorHelper.GetDeductionRawFormula(refFaceDict, deductionByFaceType, faceType);
                    var renderedFormula = QuantityExtractorHelper.GetDeductionRenderedFormula(refFaceDict, deductionByFaceType, faceType);

                    var formwork = (isExterior, faceType) switch
                    {
                        (true,  FaceType.Right) => FormworkType.Euroform,
                        (true,  FaceType.Left)  => FormworkType.Euroform,
                        (false, FaceType.Left)  => FormworkType.Euroform,
                        (false, FaceType.Right) => FormworkType.Euroform,
                        (_,     FaceType.End)   => FormworkType.Plywood3,
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                    var spec = formwork.ToSpecification();

                    var formworkItem = new QuantityItem
                    {
                        ElementId = elementId,
                        Category = wall.Category.Name ?? string.Empty,
                        ElementCode = wall.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
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

                // 스페이서 - 거푸집과 동일하게 공제된 양면 합산
                var netLeft  = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Left);
                var netRight = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Right);
                var spacerRenderedLeft  = QuantityExtractorHelper.GetDeductionRenderedFormula(refFaceDict, deductionByFaceType, FaceType.Left);
                var spacerRenderedRight = QuantityExtractorHelper.GetDeductionRenderedFormula(refFaceDict, deductionByFaceType, FaceType.Right);
                double spacerValue   = netLeft + netRight;
                string spacerFormula = "내측 + 외측";

                var leftSpacer = new QuantityItem
                {
                    ElementId = elementId,
                    Category = wall.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                    ElementCode = wall.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                    WorkType = "스페이서",
                    Specification = "수직",
                    RawFormula = "A",
                    RenderedFormula = spacerRenderedLeft,
                    Value = netLeft,
                    Unit = "m²"
                };

                quantityItems.Add(leftSpacer);

                var rightSpacer = new QuantityItem
                {
                    ElementId = elementId,
                    Category = wall.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                    ElementCode = wall.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                    WorkType = "스페이서",
                    Specification = "수직",
                    RawFormula = "A",
                    RenderedFormula = spacerRenderedRight,
                    Value = netRight,
                    Unit = "m²"
                };

                quantityItems.Add(rightSpacer);
            }
            #endregion

            #region 벽체 길이 기반
            var lenFormula = "L x CJ";
            string? lenRendered = FormulaCalculator.Render(lenFormula, varDict);
            var lenValue = FormulaCalculator.Calculate(lenFormula, varDict);

            quantityItems.Add(new QuantityItem
            {
                ElementId = elementId,
                Category = wall.Category.Name ?? string.Empty,
                ElementCode = wall.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "벽체 길이",
                Specification = "벽체 길이",
                RawFormula = lenFormula,
                RenderedFormula = lenRendered,
                Value = lenValue,
                Unit = "m"
            });
            #endregion
            return quantityItems;
        }
    }
}
