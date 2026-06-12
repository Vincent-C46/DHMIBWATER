using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class PumpingStationAnnotationService
    {
        private readonly Document _doc;

        private static readonly string[] PlanViewNames = { "상부슬래브", "기초(유입부)" };
        private const string SpotElevSymbolPlan    = "DH_평면";
        private const string SpotElevSymbolSection = "DH_단면";

        // D 단면뷰는 일반도 타입으로 지정되어 E 단면기호가 자동으로 개별 숨김 처리됨 → 다시 표시
        private const string SectionMarkerHostView = "D";
        private const string SectionMarkerView     = "E";

        // 바닥 관련 카테고리 (DirectShape 포함 다양한 타입 대응)
        private static readonly BuiltInCategory[] FloorCategories =
        {
            BuiltInCategory.OST_Floors,
            BuiltInCategory.OST_StructuralFoundation,
            BuiltInCategory.OST_Roofs,
            BuiltInCategory.OST_GenericModel,
        };

        public PumpingStationAnnotationService(Document doc)
        {
            _doc = doc;
        }

        public void Apply()
        {
            using var tx = new Transaction(_doc, "Pumping Station Annotations");
            tx.Start();
            ApplyWaterLevelTypesAndVisibility();
            ApplySpotElevations();
            ApplySectionMarkerVisibility();
            tx.Commit();
        }

        // ── D 단면뷰의 E 단면기호 표시 ───────────────────────────────────────────

        private void ApplySectionMarkerVisibility()
        {
            var hostView = GetAllPumpingStationViews()
                .FirstOrDefault(v => v.Name.Equals($"{SectionMarkerHostView}_시트", StringComparison.OrdinalIgnoreCase));
            if (hostView == null) return;

            var markerViews = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewSection))
                .Cast<View>()
                .Where(v => v.Name.Equals(SectionMarkerView, StringComparison.OrdinalIgnoreCase)
                         || v.Name.Equals($"{SectionMarkerView}_시트", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (markerViews.Count == 0) return;

            // 단면(Sections) 카테고리 자체가 꺼져있을 경우 대비
            try
            {
                var catId = new ElementId(BuiltInCategory.OST_Sections);
                if (hostView.GetCategoryHidden(catId))
                    hostView.SetCategoryHidden(catId, false);
            }
            catch { }

            foreach (var markerView in markerViews)
            {
                try
                {
                    if (markerView.IsHidden(hostView))
                        hostView.UnhideElements(new List<ElementId> { markerView.Id });
                }
                catch { }
            }
        }

        // ── HWL/LWL LevelType 적용 + 전체 뷰 표시 (E I J K 포함) ─────────────────

        private void ApplyWaterLevelTypesAndVisibility()
        {
            var hwlType = GetLevelType("HWL");
            var lwlType = GetLevelType("LWL");

            var allLevels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level)).Cast<Level>().ToList();

            var waterLevels = new List<Level>();
            foreach (var level in allLevels)
            {
                if (level.Name.Equals("HWL", StringComparison.OrdinalIgnoreCase) && hwlType != null)
                { level.ChangeTypeId(hwlType.Id); waterLevels.Add(level); }
                else if (level.Name.Equals("LWL", StringComparison.OrdinalIgnoreCase) && lwlType != null)
                { level.ChangeTypeId(lwlType.Id); waterLevels.Add(level); }
                else if (GetLevelTypeName(level).Equals("HWL", StringComparison.OrdinalIgnoreCase) ||
                         GetLevelTypeName(level).Equals("LWL", StringComparison.OrdinalIgnoreCase))
                { waterLevels.Add(level); }
            }

            if (waterLevels.Count == 0) return;

            var waterIds  = waterLevels.Select(l => l.Id).ToList();
            var allViews  = GetAllPumpingStationViews();

            foreach (var view in allViews)
            {
                // 레벨 카테고리 강제 활성화
                try
                {
                    var catId = new ElementId(BuiltInCategory.OST_Levels);
                    if (view.GetCategoryHidden(catId))
                        view.SetCategoryHidden(catId, false);
                }
                catch { }

                // 개별 레벨 언하이드 (숨긴/숨기지않은 무관하게 시도)
                try
                {
                    var toUnhide = waterIds
                        .Where(id => _doc.GetElement(id) is Level l && l.CanBeHidden(view))
                        .ToList();
                    if (toUnhide.Count > 0)
                        view.UnhideElements(toUnhide);
                }
                catch { }

                // 섹션뷰에서 레벨 선이 뷰 전체에 나오지 않도록 1mm stub으로 트림
                if (view is ViewSection)
                {
                    foreach (var wl in waterLevels)
                        TrimLevelInView(wl, view);
                }
            }
        }

        private void TrimLevelInView(Level level, View view)
        {
            try
            {
                // ViewSpecific으로 전환하기 전에 Model 익스텐트 기준 곡선을 먼저 계산
                // (전환 후 조회하면 이전에 한 번도 표시된 적 없는 뷰에서는 빈 곡선이 반환되어
                //  레벨이 통째로 사라지는 문제가 있음)
                var curves = level.GetCurvesInView(DatumExtentType.Model, view);
                if (curves.Count == 0) return;
                if (curves[0] is not Line line) return;

                var p0       = line.GetEndPoint(0);
                var p1       = line.GetEndPoint(1);
                var rightDir = view.RightDirection.Normalize();

                var    cb       = view.CropBox;
                var    t        = cb.Transform;
                double cropMinR = t.OfPoint(new XYZ(cb.Min.X, 0, 0)).DotProduct(rightDir);
                double cropMaxR = t.OfPoint(new XYZ(cb.Max.X, 0, 0)).DotProduct(rightDir);
                double centerR  = (cropMinR + cropMaxR) * 0.5;

                double r0     = p0.DotProduct(rightDir);
                double r1     = p1.DotProduct(rightDir);
                double tParam = Math.Abs(r1 - r0) > 1e-6
                    ? Math.Clamp((centerR - r0) / (r1 - r0), 0.05, 0.95)
                    : 0.5;
                XYZ headPt  = p0 + (p1 - p0).Multiply(tParam);

                double stub   = 0.001 / 0.3048;
                XYZ startPt   = headPt - rightDir.Multiply(stub);

                level.SetDatumExtentType(DatumEnds.End0, view, DatumExtentType.ViewSpecific);
                level.SetDatumExtentType(DatumEnds.End1, view, DatumExtentType.ViewSpecific);
                level.SetCurveInView(DatumExtentType.ViewSpecific, view, Line.CreateBound(startPt, headPt));
            }
            catch { }
        }

        // ── 지정점 레벨 배치 ──────────────────────────────────────────────────────

        private void ApplySpotElevations()
        {
            var planSymbol    = GetSpotElevationType(SpotElevSymbolPlan);
            var sectionSymbol = GetSpotElevationType(SpotElevSymbolSection);

            foreach (var view in GetAllPumpingStationViews())
            {
                RemoveExistingSpotElevations(view, planSymbol, sectionSymbol);

                bool isPlan = IsPlanView(view);
                var symbol  = isPlan ? planSymbol : sectionSymbol;
                if (symbol == null) continue;

                PlaceSpotElevationsInView(view, symbol);
            }
        }

        // 재실행 시 누적되지 않도록 이전에 배치된 지정점을 먼저 제거
        private void RemoveExistingSpotElevations(View view, SpotDimensionType planSymbol, SpotDimensionType sectionSymbol)
        {
            var typeIds = new HashSet<ElementId>();
            if (planSymbol != null) typeIds.Add(planSymbol.Id);
            if (sectionSymbol != null) typeIds.Add(sectionSymbol.Id);
            if (typeIds.Count == 0) return;

            var toDelete = new FilteredElementCollector(_doc, view.Id)
                .OfClass(typeof(SpotDimension))
                .Cast<SpotDimension>()
                .Where(sd => typeIds.Contains(sd.GetTypeId()))
                .Select(sd => sd.Id)
                .ToList();

            if (toDelete.Count > 0)
                _doc.Delete(toDelete);
        }

        private void PlaceSpotElevationsInView(View view, SpotDimensionType symbol)
        {
            // 다양한 카테고리에서 바닥 요소 수집 (중복 제거)
            var elems = new List<Element>();
            foreach (var cat in FloorCategories)
            {
                try
                {
                    elems.AddRange(
                        new FilteredElementCollector(_doc, view.Id)
                            .OfCategory(cat)
                            .WhereElementIsNotElementType()
                            .ToElements());
                }
                catch { }
            }

            foreach (var elem in elems.GroupBy(e => e.Id).Select(g => g.First()))
            {
                // 계단식 바닥 대응: 상부면 여러 개를 각각 처리
                var faceInfos = GetAllTopFaceInfos(elem, view);
                foreach (var (faceRef, facePt) in faceInfos)
                {
                    try
                    {
                        var spot = _doc.Create.NewSpotElevation(
                            view, faceRef, facePt, facePt, facePt, facePt, false);
                        if (spot != null)
                            spot.ChangeTypeId(symbol.Id);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// 요소에서 상부 수평면(법선이 Z+)을 모두 찾아 각 면의 중심점과 Reference 반환.
        /// 계단식 바닥처럼 여러 높이의 면이 있는 경우 모두 처리.
        /// </summary>
        private List<(Reference FaceRef, XYZ FaceCenter)> GetAllTopFaceInfos(Element elem, View view)
        {
            var result = new List<(Reference, XYZ)>();
            // View 지정 없이 전체 geometry 취득: section view는 View 지정 시
            // 단면 절단면만 반환하여 Z+ 법선 수평면이 하나도 잡히지 않음
            var opt    = new Options { ComputeReferences = true };
            var geo    = elem.get_Geometry(opt);
            if (geo == null) return result;

            void ProcessSolid(Solid solid, Transform transform)
            {
                if (solid == null || solid.Faces.IsEmpty) return;
                foreach (Face face in solid.Faces)
                {
                    if (face is not PlanarFace pf || pf.Reference == null) continue;
                    var n = pf.FaceNormal.Normalize();
                    // transform이 있으면 법선도 변환
                    var worldNormal = transform != null ? transform.OfVector(n).Normalize() : n;
                    if (worldNormal.DotProduct(XYZ.BasisZ) < 0.9) continue;

                    var bb2     = pf.GetBoundingBox();
                    var uvMid   = (bb2.Min + bb2.Max) * 0.5;
                    var localPt = pf.Evaluate(uvMid);
                    var worldPt = transform != null ? transform.OfPoint(localPt) : localPt;

                    result.Add((pf.Reference, worldPt));
                }
            }

            foreach (var obj in geo)
            {
                if (obj is Solid solid)
                    ProcessSolid(solid, null);
                else if (obj is GeometryInstance gi)
                {
                    var instGeo = gi.GetInstanceGeometry();
                    var tf      = gi.Transform;
                    foreach (var inner in instGeo)
                        if (inner is Solid s) ProcessSolid(s, tf);
                }
            }

            // 같은 Z 위치에서 중복 제거 (1mm 허용오차)
            var distinct = new List<(Reference, XYZ)>();
            foreach (var item in result)
            {
                if (!distinct.Any(d => Math.Abs(d.Item2.Z - item.Item2.Z) < 0.001 / 0.3048 &&
                                       d.Item2.DistanceTo(item.Item2) < 0.1))
                    distinct.Add(item);
            }
            return distinct;
        }

        // ── 뷰 수집 ──────────────────────────────────────────────────────────────

        private List<View> GetAllPumpingStationViews() =>
            new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate && IsPumpingStationView(v))
                .ToList();

        private bool IsPumpingStationView(View view)
        {
            var name = view.Name;
            if (!name.EndsWith("_시트", StringComparison.OrdinalIgnoreCase)) return false;
            var baseName = name[..^"_시트".Length];
            return PlanViewNames.Any(p => p.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                || (baseName.Length == 1 && char.IsUpper(baseName[0]));
        }

        private bool IsPlanView(View view)
        {
            var baseName = view.Name[..^"_시트".Length];
            return PlanViewNames.Any(p => p.Equals(baseName, StringComparison.OrdinalIgnoreCase));
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────────────────

        private LevelType GetLevelType(string name) =>
            new FilteredElementCollector(_doc).OfClass(typeof(LevelType)).Cast<LevelType>()
                .FirstOrDefault(lt => lt.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        private string GetLevelTypeName(Level level) =>
            _doc.GetElement(level.GetTypeId()) is LevelType lt ? lt.Name ?? string.Empty : string.Empty;

        private SpotDimensionType GetSpotElevationType(string name) =>
            new FilteredElementCollector(_doc).OfClass(typeof(SpotDimensionType)).Cast<SpotDimensionType>()
                .FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
