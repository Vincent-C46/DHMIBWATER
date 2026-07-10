using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class PumpingStationDimensionService
    {
        private readonly Document    _doc;
        private readonly UIDocument  _uidoc;

        private const double SegmentOffset = 5.25;   // ft
        private const double OverallOffset = 6.89;   // ft

        public PumpingStationDimensionService(Document doc, UIDocument uidoc)
        {
            _doc   = doc;
            _uidoc = uidoc;
        }

        // ── 진입점 ────────────────────────────────────────────────────────────

        public void ApplyToSheet(string sheetId, string dimensionTypeName)
        {
            if (!long.TryParse(sheetId, out var sid)) return;
            var sheet = _doc.GetElement(new ElementId(sid)) as ViewSheet;
            if (sheet == null) return;

            _uidoc.ActiveView = sheet;

            using var tx = new Transaction(_doc, "Pumping Station Dimensions");
            tx.Start();
            _doc.Regenerate();

            var dimTypeId = GetDimensionTypeId(dimensionTypeName);

            foreach (var vpId in sheet.GetAllViewports())
            {
                _doc.Regenerate();
                var vp   = _doc.GetElement(vpId) as Viewport;
                if (vp == null) continue;
                var view = _doc.GetElement(vp.ViewId) as View;
                if (view is not ViewSection && view is not ViewPlan) continue;
                if (view.Name.Contains("KeyMap",   StringComparison.OrdinalIgnoreCase) ||
                    view.Name.Contains("KEY PLAN", StringComparison.OrdinalIgnoreCase)) continue;

                ApplyToView(view, dimTypeId);
            }

            tx.Commit();
        }

        // ── 뷰별 치수선 배치 ─────────────────────────────────────────────────

        private void ApplyToView(View view, ElementId dimTypeId)
        {
            var preset = GetPreset(view);
            if (preset == null) return;

            _doc.Regenerate();

            var right = view.RightDirection.Normalize();
            var up    = view.UpDirection.Normalize();

            GetCropBoxExtents(view, right, up, out var cropMinR, out var cropMaxR, out var cropMinU, out var cropMaxU);

            var allCandidates = new FilteredElementCollector(_doc, view.Id)
                .WhereElementIsNotElementType()
                .Where(e => e.Category != null)
                .OrderBy(e => e.Id.Value)
                .ToList();

            var topTargets    = preset.UseTop    ? allCandidates.Where(e => MatchesRule(e, preset.TopRule)).ToList()    : new List<Element>();
            var bottomTargets = preset.UseBottom ? allCandidates.Where(e => MatchesRule(e, preset.BottomRule)).ToList() : new List<Element>();
            var leftTargets   = preset.UseLeft   ? allCandidates.Where(e => MatchesRule(e, preset.LeftRule)).ToList()   : new List<Element>();
            var rightTargets  = preset.UseRight  ? allCandidates.Where(e => MatchesRule(e, preset.RightRule)).ToList()  : new List<Element>();

            bool any = topTargets.Count > 0 || bottomTargets.Count > 0 ||
                       leftTargets.Count > 0 || rightTargets.Count > 0;
            if (!any) return;

            var topRefs    = new List<FaceRef>();
            var bottomRefs = new List<FaceRef>();
            var leftRefs   = new List<FaceRef>();
            var rightRefs  = new List<FaceRef>();

            // 치수선 위치를 바운딩박스가 아닌 실제 면(face) 위치 기준으로 계산
            double topLineU   = double.MinValue;
            double botLineU   = double.MaxValue;
            double leftLineR  = double.MaxValue;
            double rightLineR = double.MinValue;

            const double lineTol = 1.0; // ft – 치수선 위치 제한 (크롭 경계 ±1ft)
            const double refTol  = 3.0; // ft – face ref 후처리 필터 (크롭 경계 ±3ft)

            foreach (var e in topTargets)
            {
                TryGetFaceRefs(view, e, right, up, out var minR, out var maxR, out _, out var maxU);
                if (minR != null) topRefs.Add(minR);
                if (maxR != null) topRefs.Add(maxR);
                if (maxU != null && maxU.Projection > topLineU && maxU.Projection <= cropMaxU + lineTol) topLineU = maxU.Projection;
            }
            foreach (var e in bottomTargets)
            {
                TryGetFaceRefs(view, e, right, up, out var minR, out var maxR, out var minU, out _);
                if (minR != null) bottomRefs.Add(minR);
                if (maxR != null) bottomRefs.Add(maxR);
                if (minU != null && minU.Projection < botLineU && minU.Projection >= cropMinU - lineTol) botLineU = minU.Projection;
            }
            foreach (var e in leftTargets)
            {
                TryGetFaceRefs(view, e, right, up, out var minR, out _, out var minU, out var maxU);
                if (minU != null) leftRefs.Add(minU);
                if (maxU != null) leftRefs.Add(maxU);
                if (minR != null && minR.Projection < leftLineR && minR.Projection >= cropMinR - lineTol) leftLineR = minR.Projection;
            }
            foreach (var e in rightTargets)
            {
                TryGetFaceRefs(view, e, right, up, out _, out var maxR, out var minU, out var maxU);
                if (minU != null) rightRefs.Add(minU);
                if (maxU != null) rightRefs.Add(maxU);
                if (maxR != null && maxR.Projection > rightLineR && maxR.Projection <= cropMaxR + lineTol) rightLineR = maxR.Projection;
            }

            // 완전히 크롭 범위 밖(±3ft 초과)에 있는 face ref 제거
            topRefs    = topRefs   .Where(r => r.Projection >= cropMinR - refTol && r.Projection <= cropMaxR + refTol).ToList();
            bottomRefs = bottomRefs.Where(r => r.Projection >= cropMinR - refTol && r.Projection <= cropMaxR + refTol).ToList();
            leftRefs   = leftRefs  .Where(r => r.Projection >= cropMinU - refTol && r.Projection <= cropMaxU + refTol).ToList();
            rightRefs  = rightRefs .Where(r => r.Projection >= cropMinU - refTol && r.Projection <= cropMaxU + refTol).ToList();

            // face를 찾지 못한 방향은 바운딩박스로 fallback
            if (topLineU == double.MinValue || botLineU == double.MaxValue ||
                leftLineR == double.MaxValue || rightLineR == double.MinValue)
            {
                var allTargets = topTargets.Concat(bottomTargets).Concat(leftTargets).Concat(rightTargets).Distinct().ToList();
                GetModelExtents(view, allTargets,   right, up, out _, out _, out var minUpFb, out var maxUpFb);
                GetModelExtents(view, leftTargets,  right, up, out var minRFb, out _, out _, out _);
                GetModelExtents(view, rightTargets, right, up, out _, out var maxRFb, out _, out _);
                if (topLineU   == double.MinValue) topLineU   = maxUpFb;
                if (botLineU   == double.MaxValue) botLineU   = minUpFb;
                if (leftLineR  == double.MaxValue) leftLineR  = minRFb;
                if (rightLineR == double.MinValue) rightLineR = maxRFb;
            }

            leftRefs  = CollapseNearbyRefs(leftRefs,  1.0, preferSmallerArea: true);
            rightRefs = CollapseNearbyRefs(rightRefs, 1.0, preferSmallerArea: true);

            _doc.Regenerate();

            if (preset.UseTop           && topRefs.Count    >= 2) CreateSegmentDimensionsAtTop(view,    topRefs,    right, up,    topLineU   + SegmentOffset, dimTypeId);
            if (preset.UseTopOverall    && topRefs.Count    >= 2) CreateOverallDimensionAtTop(view,     topRefs,    right, up,    topLineU   + OverallOffset,  dimTypeId);
            if (preset.UseBottom        && bottomRefs.Count >= 2) CreateSegmentDimensionAtBottom(view,  bottomRefs, right, up,    botLineU   - SegmentOffset, dimTypeId);
            if (preset.UseBottomOverall && bottomRefs.Count >= 2) CreateOverallDimensionAtBottom(view,  bottomRefs, right, up,    botLineU   - OverallOffset,  dimTypeId);
            if (preset.UseLeft          && leftRefs.Count   >= 2) CreateSegmentDimensionsAtLeft(view,   leftRefs,   up,    right, leftLineR  - SegmentOffset, dimTypeId);
            if (preset.UseLeftOverall   && leftRefs.Count   >= 2) CreateOverallDimensionsAtLeft(view,   leftRefs,   up,    right, leftLineR  - OverallOffset,  dimTypeId);
            if (preset.UseRight         && rightRefs.Count  >= 2) CreateSegmentDimensionAtRight(view,   rightRefs,  up,    right, rightLineR + SegmentOffset, dimTypeId);
            if (preset.UseRightOverall  && rightRefs.Count  >= 2) CreateOverallDimensionAtRight(view,   rightRefs,  up,    right, rightLineR + OverallOffset,  dimTypeId);
        }

        // ── 프리셋 조회 ───────────────────────────────────────────────────────

        private ViewDimensionPreset GetPreset(View view)
        {
            var section = ExtractSection(view.Name);
            if (section == null) return null;
            var psType = DetectType(view);
            if (psType == null) return null;
            return Presets.TryGetValue($"{psType}_{section}", out var p) ? p : null;
        }

        private static string ExtractSection(string viewName)
        {
            const string suffix = "_시트";
            if (string.IsNullOrEmpty(viewName) || !viewName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return null;
            return viewName[..^suffix.Length];
        }

        private string DetectType(View view)
        {
            var elem = new FilteredElementCollector(_doc, view.Id)
                .WhereElementIsNotElementType()
                .FirstOrDefault(e => IsKnownType(GetDescription(e)));
            if (elem == null) return null;
            return GetDescription(elem)?.Trim();
        }

        private string GetDescription(Element e)
        {
            var val = e.get_Parameter(BuiltInParameter.ALL_MODEL_DESCRIPTION)?.AsString();
            if (!string.IsNullOrWhiteSpace(val)) return val;

            val = e.LookupParameter("설명")?.AsString();
            if (!string.IsNullOrWhiteSpace(val)) return val;

            var typeElem = _doc.GetElement(e.GetTypeId());
            if (typeElem == null) return null;

            val = typeElem.get_Parameter(BuiltInParameter.ALL_MODEL_DESCRIPTION)?.AsString();
            if (!string.IsNullOrWhiteSpace(val)) return val;

            return typeElem.LookupParameter("설명")?.AsString();
        }

        private static bool IsKnownType(string s) =>
            KnownTypes.Any(t => string.Equals(s?.Trim(), t, StringComparison.OrdinalIgnoreCase));

        private static readonly string[] KnownTypes =
            { "TYPE1_좌안부", "TYPE1_우안부", "TYPE1_측면부", "TYPE2_측면부", "TYPE3_측면부" };

        // ── 코드 설정 테이블 ─────────────────────────────────────────────────
        // (타입, 단면, 상부코드, 하부코드, 좌측코드, 우측코드)
        // 빈 문자열 = 해당 방향 치수선 미생성 / 쉼표로 여러 코드 구분
        private static readonly (string Type, string Section, string Top, string Bottom, string Left, string Right)[] CodeTable =
        {
            // ── TYPE1_좌안부 ──────────────────────────────────────────────────
            ("TYPE1_좌안부", "상부슬래브",    "W1-1,W1-2,W1-3,W2,G1",     "W1,W2,W4,G1,SO1,SO2",         "W1,W1-1,W1-2,W1-3,W3",    "W1,W1-3,W2,W5,WO1"         ),
            ("TYPE1_좌안부", "기초(유입부)",  "W1-1,W1-2,W1-3,W2,F2,WO3", "W1,W2,AVW,WO2",               "W1,W1-1,W3",              "W1,W1-3,W5,AVW"            ),
            ("TYPE1_좌안부", "A",             "S1,W2,W4,G1,S01,S02",      "W2,F1,F2,AVW",                "S1,W2,MS1,F1,F2,WO1",     "S1,G1,F1,F2"               ),
            ("TYPE1_좌안부", "B",             "S1,G1,F1,F2,SO1",          "F1,F2,W3-1,WO2",              "S1,MS1,F1,F2",            "S1,F1,F2,G1"               ),
            ("TYPE1_좌안부", "C",             "S1,W1-2,W1-3,W2",          "F1,F2,W1-2,W1-3,W2",          "S1,F1,F2,W1-3,W2",        "S1,F1,F2,W1-2,W1-3"        ),
            ("TYPE1_좌안부", "D",             "S1,W1-2,W2,G1,MS1",        "F1,F2,W1-2,W2,MS1",           "S1,F1,F2,W2,MS1",         "S1,F1,F2,G1,MS1"           ),
            ("TYPE1_좌안부", "E",             "F1,F2,W1-2,W1-3,W2,WO3",   "F2,W5,W1-2,W2,WO3",           "F2,W1-1,W1-2,W1-3",       "F2,W1-3,W2,W5"             ),
            ("TYPE1_좌안부", "F",             "S1,W1,W1-1,W3",            "F1,F2,W1,W1-1,W3",            "S1,W1-1,F1,F2",           "S1,W1,F1,F2"               ),
            ("TYPE1_좌안부", "G",             "S1,W1,W1-1,W3,G1",         "F1,F2,W1,W1-1,W3",            "S1,G1,W1-1,F1,F2,SO1",    "S1,G1,W1,F1,F2,SO1"        ),
            ("TYPE1_좌안부", "H",             "S1,W1,W1-3,W3,W5,G1",      "W1,W1-3,F1,F2,WO3",           "S1,W1-3,F1,F2,WO3",       "S1,W1,W3,F1,F2,G1,WO2"     ),
            ("TYPE1_좌안부", "I",             "S1,W1,W1-3,W3,W5,SO2",     "S1,W1,W1-3,W3,W5",            "S1,W1-3,F1,F2,WO3",       "S1,W1,F1,F2"               ),
            ("TYPE1_좌안부", "J",             "S1,W1,W1-3,W3,W5,WO1",     "F1,F1,W1,W1-3,W3,W5,AVW",     "S1,W1-3,F1,F2,MS1",       "S1,W1,AVW,F1,F2,MS1"       ),
            ("TYPE1_좌안부", "K",             "S1,W1,W1-3,W5,WO1",        "F1,F2,W1,W1-3,W3-1,W5,AVW",   "S1,F1,F2,W1-3",           "S1,F1,F2,W1,MS1"           ),

            // ── TYPE1_우안부 ──────────────────────────────────────────────────
            ("TYPE1_우안부", "상부슬래브",    "S1,W2,W4,G1,SO1,SO2",      "W1-2,W1-3,W2",                "S1,W1,W1-1,W3 ",          "S1,W1,W1-3,W3,W5,WO1"      ),
            ("TYPE1_우안부", "기초(유입부)",  "F1,F2,W1,W2,AVW,WO2",      "F2,F1,W1-1,W1-2,W1-3,W2,WO3", "F1,F2,W1,W1-1,W3",        "F1,F2,W1,W1-3,W3,W5,AVW"   ),
            ("TYPE1_우안부", "A",             "S1,G1,W2,W4,MS1,SO1,SO2",  "F1,F2,W2,W3,AVW",             "S1,F1,F2",                "S1,W2,F1,F2,MS1,WO1"       ),
            ("TYPE1_우안부", "B",             "S1,G1,W2,W4,MS1,SO1",      "F1,F2,W2,W3,AVW,WO2",         "S1,F1,F2",                "W2,W3,W3-1,F1,F2,WO2"      ),
            ("TYPE1_우안부", "C",             "S1,W1-2,W2",               "F1,F2,W1-2,W2",               "W1-2,S1,F1,F2",           "W2,S1,F1,F2"               ),
            ("TYPE1_우안부", "D",             "S1,W1-2,W2,W5,G1,MS1",     "F1,F2,W1-2,B5,WO3",           "S1,F1,F2,W1-2,G1,W5,WO3", "S1,F1,F2,W2,W5,MS1"        ),
            ("TYPE1_우안부", "E",             "W5,W1-2,W2,W4,WO3",        "W1-2,W1-3,W2",                "W1-1,W1-2,W1-3,W5",       "W2,W1-3,W5"                ),
            ("TYPE1_우안부", "F",             "S1,W1,W1-1,W3",            "F1,F2,W1,W1-1,W3",            "S1,W1,F1,F2",             "S1,W1-1,F1,F2"             ),
            ("TYPE1_우안부", "G",             "S1,W1,W1-1,W3",            "F1,F2,W1,W1-1,W3",            "S1,G1,W1,F1,F2,SO1",      "S1,G1,W1-1,F1,F2,SO1"      ),
            ("TYPE1_우안부", "H",             "S1,W1,W1-3,W3,W5,G1",      "W1,W1-3,F1,F2",               "S1,W1,F1,F2,G1,WO2",      "S1,W1-3,F1,F2,G1,WO3"      ),
            ("TYPE1_우안부", "I",             "S1,W1-3,W3,W5,SO3",        "F1,F2,W1-3,W3,W5",            "S1,F1,F2,W1",             "S1,F1,F2,W1-3,WO3"         ),
            ("TYPE1_우안부", "J",             "S1,W1,W1-3,W3,W5,WO1",     "F1,F2,W1,W1-3,W3,W5,AVW",     "S1,F1,F2,MS1,W1",         "S1  ,F1,F2,W1-3"           ),
            ("TYPE1_우안부", "K",             "S1,W1,W1-3,W5,WO1",        "F1,F2,W1,W1-3,W5,W3-1,AVW",   "S1,F1,F2,MS1,W1",         "S1,F1,F2,W1-3"             ),

            // ── TYPE1_측면부 ──────────────────────────────────────────────────
            ("TYPE1_측면부", "상부슬래브",    "S1,W1,W2,W4,G1,SO1,SO2",   "S1,W1,W2,W4,G1,SO1,SO2",      "S1,W1,W3",                "S1,W1,W2,W3,WO1"           ),
            ("TYPE1_측면부", "기초(유입부)",  "F1,F2,W1,W2,AVW,WO2",      "F1,F2,W1,W2,AVW,WO2",         "F1,F2,W1,W3",             "F1,F2,W1,W3-1,AVW"         ),
            ("TYPE1_측면부", "A",             "S1,G1,W2,W4,MS1,SO1,SO2",  "F1,F2,W2,AVW",                "S1,F1,F2,G1",             "S1,F1,F2,MS1,AVW,WO1"      ),
            ("TYPE1_측면부", "B",             "S1,G1,W2,W4,SO1",          "F1,F2,W2,WO2",                "S1,F1,F2,",               "S1,F1,F2,MS1"              ),
            ("TYPE1_측면부", "C",             "S1,W1,W3",                 "F1,F2,W1,W3",                 "S1,F1,F2,W1",             "S1,F1,F2,W1"               ),
            ("TYPE1_측면부", "D",             "S1,W1,W3,G1",              "F1,F2,W1,W3",                 "S1,F1,F2,W1,G1,SO1",      "S1,F1,F2,W1,G1,SO1"        ),
            ("TYPE1_측면부", "E",             "S1,W1,W3",                 "F1,F2,W1",                    "S1,F1,F2,W1,W3,WO2",      "S1,F1,F2,W1,W3,WO2"        ),
            ("TYPE1_측면부", "F",             "S1,W1,W3,SO2",             "F1,F2,W1,W3",                 "S1,F1,F2,W1",             "S1,F1,F2,W1"               ),
            ("TYPE1_측면부", "G",             "S1,W1,W3,WO1",             "F1,F2,W1,W3,AVW",             "S1,F1,F2,W1,MS1",         "S1,F1,F2,W1,MS"            ),
            ("TYPE1_측면부", "H",             "S1,W1,WO1",                "F1,F2,W1,W3-1,AVW",           "S1,F1,F2,MS1",            "S1,F1,F2,MS1"              ),
            ("TYPE1_측면부", "I",             "",                         "",                            "",                        ""                          ),             
            ("TYPE1_측면부", "J",             "",                         "",                            "",                        ""                          ),
            ("TYPE1_측면부", "K",             "",                         "",                            "",                        ""                          ),

            // ── TYPE2_측면부 ──────────────────────────────────────────────────
            ("TYPE2_측면부", "상부슬래브",    "S1,G1,W2,W4,SO1,SO2",      "S1,G1,W2,W4,SO1",             "S1,W1,W3",                "S1,W1-1,W3,WO1"            ),
            ("TYPE2_측면부", "기초(유입부)",  "F1,F2,W1,W4-1,WO2",        "F1,F2,W1,W4-1,WO2",           "F1,F2,W1,W3,",            "F1,F2,W1,W3"               ),
            ("TYPE2_측면부", "A",             "S1,G1,W2,W4",              "F1,F2,W4-1,W2,MS1",           "S1,F1,F2,G1",             "S1,G1,W2,W4-1,MS1,WO1"     ),
            ("TYPE2_측면부", "B",             "S1,W2,W4,G1,SO1",          "F1,F2,W2,W4-1,MS1,WO1",       "S1,F1,F2,G1",             "S1,F1,F2,G1,W2,MS1,WO1"    ),
            ("TYPE2_측면부", "C",             "S1,W1,W3",                 "F1,F2,W1,W3",                 "S1,F1,F2,W1",             "S1,F1,F2,W1"               ),
            ("TYPE2_측면부", "D",             "S1,W1,W3,SO1",             "F1,F2,W1,W3",                 "S1,W1,F1,F2,G1",          "S1,W1,F1,F2,G1"            ),
            ("TYPE2_측면부", "E",             "S1,W1,W3",                 "F1,F2,W1,WO2",                "S1,F1,F2,W1,W3,WO1",      "S1,F1,F2,W1,W3,WO1"        ),
            ("TYPE2_측면부", "F",             "S1,SO2,W1,W3",             "F1,F2,W1,W3",                 "S1,F1,F2,W1",             "S1,F1,F2,W1"               ),
            ("TYPE2_측면부", "G",             "S1,W1,W3,SO2",             "F1,F2,W1,W3",                 "S1,F1,F2,W1,W4-1,WO1",    "S1,F1,F2,W1,W4-1,WO1"      ),
            ("TYPE2_측면부", "H",             "S1,W1-1,WO1",              "MS1,W1-1,WO1",                "S1,MS1,W1-1,WO1",         "S1,MS1,W1-1,WO1"           ),
            ("TYPE2_측면부", "I",             "",                         "",                            "",                        ""                          ),  
            ("TYPE2_측면부", "J",             "",                         "",                            "",                        ""                          ),
            ("TYPE2_측면부", "K",             "",                         "",                            "",                        ""                          ),

            // ── TYPE3_측면부 ──────────────────────────────────────────────────
            ("TYPE3_측면부", "상부슬래브",    "S1,G1,W1,W2,W4,SO1,SO2",   "S1,W1,W2,W4,G1,SO1,SO2",      "S1,W1,W3",                "S1,W1,W2,W3,WO1"           ),
            ("TYPE3_측면부", "기초(유입부)",  "F1,F2,W2,W4-1,WO2",        "F1,F2,W2,W4-1,WO2",           "F1,F2,W1,W3",             "F1,F2,W1,W2,W3"            ),
            ("TYPE3_측면부", "A",             "S1,G1,SO1,SO2,W2,W4",      "F1,F2,W2,W4-1",               "S1,F1,F2,G1",             "S1,F1,F2,MS1,WO1"          ),
            ("TYPE3_측면부", "B",             "S1,G1,W2,W4,MS1,SO1",      "F1,F2,W2,W4-1,WO2",           "S1,G1,F1,F2",             "S1,G1,F1,F2,W2,MS1"        ),
            ("TYPE3_측면부", "C",             "S1,W1,W3",                 "F1,F2,W1,W3",                 "S1,W1,F1,F2",             "S1,W1,F1,F2"               ),
            ("TYPE3_측면부", "D",             "S1,W1,W3,SO1",             "F1,F2,W1,W3",                 "S1,G1,W1,F1,F2",          "S1,G1,W1,F1,F2"            ),
            ("TYPE3_측면부", "E",             "S1,W1,W3",                 "F1,F2,W1,W3",                 "S1,F1,F2,W1,W3,WO2",      "S1,F1,F2,W1,W3,WO2"        ),
            ("TYPE3_측면부", "F",             "S1,SO2,W1,W3",             "F1,F2,W1,W3",                 "S1,F1,F2,W1",             "S1,F1,F2,W1"               ),
            ("TYPE3_측면부", "G",             "S1,W1,W3,WO1",             "F1,F2,W1,W3",                 "S1,F1,F2,W1",             "S1,F1,F2,W1"               ),
            ("TYPE3_측면부", "H",             "S1,W1,WO1,MS1",            "F1,F2,W1",                    "S1,F1,F2,MS1",            "S1,F1,F2,MS1"              ),
            ("TYPE3_측면부", "I",             "",                         "",                            "",                        ""                          ),
            ("TYPE3_측면부", "J",             "",                         "",                            "",                        ""                          ),
            ("TYPE3_측면부", "K",             "",                         "",                            "",                        ""                          ),
        };

        private static readonly Dictionary<string, ViewDimensionPreset> Presets;

        static PumpingStationDimensionService()
        {
            Presets = new Dictionary<string, ViewDimensionPreset>(StringComparer.OrdinalIgnoreCase);
            foreach (var (type, section, top, bottom, left, right) in CodeTable)
                Presets[$"{type}_{section}"] = MakePreset(top, bottom, left, right);
        }

        private static ViewDimensionPreset MakePreset(string top, string bottom, string left, string right)
        {
            var p = new ViewDimensionPreset
            {
                UseTop           = !string.IsNullOrWhiteSpace(top),
                UseBottom        = !string.IsNullOrWhiteSpace(bottom),
                UseLeft          = !string.IsNullOrWhiteSpace(left),
                UseRight         = !string.IsNullOrWhiteSpace(right),
                UseTopOverall    = !string.IsNullOrWhiteSpace(top),
                UseBottomOverall = !string.IsNullOrWhiteSpace(bottom),
                UseLeftOverall   = !string.IsNullOrWhiteSpace(left),
                UseRightOverall  = !string.IsNullOrWhiteSpace(right),
            };
            ApplyCodeRule(p.TopRule,    top);
            ApplyCodeRule(p.BottomRule, bottom);
            ApplyCodeRule(p.LeftRule,   left);
            ApplyCodeRule(p.RightRule,  right);
            return p;
        }

        private static void ApplyCodeRule(DimensionFilterRule rule, string codes)
        {
            if (string.IsNullOrWhiteSpace(codes)) return;
            rule.IncludeParameterName  = "DH_ElementCode";
            rule.IncludeParameterValue = codes;
            rule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            rule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            rule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };
        }

        // ── 규칙 매칭 ────────────────────────────────────────────────────────

        private bool MatchesRule(Element e, DimensionFilterRule rule)
        {
            if (e?.Category == null) return false;
            var bic = (BuiltInCategory)e.Category.Id.Value;

            if (rule.ExcludeCategories.Contains(bic)) return false;
            if (!string.IsNullOrWhiteSpace(rule.ExcludeParameterName) &&
                !string.IsNullOrWhiteSpace(rule.ExcludeParameterValue) &&
                MatchesParameter(e, rule.ExcludeParameterName, rule.ExcludeParameterValue)) return false;

            var names = new List<string>();
            if (!string.IsNullOrWhiteSpace(e.Name)) names.Add(e.Name);
            var type = _doc.GetElement(e.GetTypeId());
            if (type != null && !string.IsNullOrWhiteSpace(type.Name)) names.Add(type.Name);
            if (e is FamilyInstance fi)
            {
                if (!string.IsNullOrWhiteSpace(fi.Symbol?.Name))       names.Add(fi.Symbol.Name);
                if (!string.IsNullOrWhiteSpace(fi.Symbol?.FamilyName)) names.Add(fi.Symbol.FamilyName);
            }

            if (rule.ExcludeNameKeywords.Length > 0 &&
                names.Any(n => rule.ExcludeNameKeywords.Any(k => n.Contains(k, StringComparison.OrdinalIgnoreCase))))
                return false;

            bool hasCatRule   = rule.IncludeCategories.Count > 0;
            bool hasNameRule  = rule.IncludeNameKeywords.Length > 0;
            bool hasParamRule = !string.IsNullOrWhiteSpace(rule.IncludeParameterName) &&
                                !string.IsNullOrWhiteSpace(rule.IncludeParameterValue);

            bool catOk   = !hasCatRule   || rule.IncludeCategories.Contains(bic);
            bool nameOk  = !hasNameRule  || names.Any(n => rule.IncludeNameKeywords.Any(k => n.Contains(k, StringComparison.OrdinalIgnoreCase)));
            bool paramOk = !hasParamRule || MatchesParameter(e, rule.IncludeParameterName, rule.IncludeParameterValue);

            return catOk && nameOk && paramOk;
        }

        private bool MatchesParameter(Element e, string paramName, string expectedValue)
        {
            if (e == null || string.IsNullOrWhiteSpace(paramName) || string.IsNullOrWhiteSpace(expectedValue)) return false;
            var p = e.LookupParameter(paramName);
            if (p == null) return false;

            string actual = p.AsString();
            if (string.IsNullOrWhiteSpace(actual)) actual = p.AsValueString();
            if (string.IsNullOrWhiteSpace(actual))
            {
                actual = p.StorageType switch
                {
                    StorageType.Integer   => p.AsInteger().ToString(),
                    StorageType.Double    => p.AsDouble().ToString(),
                    StorageType.ElementId => p.AsElementId().Value.ToString(),
                    _                     => actual
                };
            }
            if (string.IsNullOrWhiteSpace(actual)) return false;

            return expectedValue.Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Any(x => string.Equals(actual.Trim(), x, StringComparison.OrdinalIgnoreCase));
        }

        // ── Face ref 수집 ────────────────────────────────────────────────────

        private void TryGetFaceRefs(
            View view, Element e, XYZ right, XYZ up,
            out FaceRef minRight, out FaceRef maxRight,
            out FaceRef minUp,    out FaceRef maxUp)
        {
            minRight = maxRight = minUp = maxUp = null;

            var opt = new Options { View = view, ComputeReferences = true };
            var geo = e.get_Geometry(opt);
            if (geo == null) return;

            var ebb = e.get_BoundingBox(view);
            if (ebb == null) return;

            const double dirTol  = 0.01;
            const double areaTol = 1e-4;
            const double bboxTol = 1.0;

            var corners = new[]
            {
                new XYZ(ebb.Min.X, ebb.Min.Y, ebb.Min.Z), new XYZ(ebb.Min.X, ebb.Max.Y, ebb.Min.Z),
                new XYZ(ebb.Max.X, ebb.Min.Y, ebb.Min.Z), new XYZ(ebb.Max.X, ebb.Max.Y, ebb.Min.Z),
                new XYZ(ebb.Min.X, ebb.Min.Y, ebb.Max.Z), new XYZ(ebb.Min.X, ebb.Max.Y, ebb.Max.Z),
                new XYZ(ebb.Max.X, ebb.Min.Y, ebb.Max.Z), new XYZ(ebb.Max.X, ebb.Max.Y, ebb.Max.Z),
            };
            var elemMinR = corners.Min(p => p.DotProduct(right));
            var elemMaxR = corners.Max(p => p.DotProduct(right));
            var elemMinU = corners.Min(p => p.DotProduct(up));
            var elemMaxU = corners.Max(p => p.DotProduct(up));

            var minRC = new List<FaceRef>(); var maxRC = new List<FaceRef>();
            var minUC = new List<FaceRef>(); var maxUC = new List<FaceRef>();

            var solids = EnumerateSolids(geo).ToList();

            if (solids.Count > 0)
            {
                var code = e.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty;
                var eid  = e.Id.Value;
                CollectAlignedFaceRefs(solids, right, up, elemMinR, elemMaxR, elemMinU, elemMaxU,
                    dirTol, areaTol, bboxTol, code, eid, minRC, maxRC, minUC, maxUC);
            }

            // 자체 지오메트리가 없거나(순수 Void), 있어도 유효한 축을 하나도 못 찾았으면
            // 이 요소가 실제로 뚫고 있는 호스트를 찾아 그 면(터널 벽면/구멍 edge)을 대신 사용
            if (minRC.Count == 0 && maxRC.Count == 0 && minUC.Count == 0 && maxUC.Count == 0)
            {
                CollectHoleBoundaryRefs(view, e, right, up, elemMinR, elemMaxR, elemMinU, elemMaxU,
                    dirTol, bboxTol, minRC, maxRC, minUC, maxUC);
            }

            minRight = minRC.OrderByDescending(x => x.Area).ThenBy(x => x.StableKey).FirstOrDefault();
            maxRight = maxRC.OrderByDescending(x => x.Area).ThenBy(x => x.StableKey).FirstOrDefault();
            minUp    = minUC.OrderByDescending(x => x.Area).ThenBy(x => x.StableKey).FirstOrDefault();
            maxUp    = maxUC.OrderByDescending(x => x.Area).ThenBy(x => x.StableKey).FirstOrDefault();
        }

        // FamilyInstance는 get_Geometry()가 최상위에서 GeometryInstance(심볼 지오메트리 래퍼)를 반환하므로
        // 그 안의 실제 Solid까지 재귀적으로 풀어서 꺼낸다 (호스트 객체는 바로 Solid가 나오므로 영향 없음)
        private static IEnumerable<Solid> EnumerateSolids(GeometryElement geo)
        {
            foreach (var obj in geo)
            {
                switch (obj)
                {
                    case Solid solid when !solid.Faces.IsEmpty:
                        yield return solid;
                        break;
                    case GeometryInstance gi:
                        foreach (var s in EnumerateSolids(gi.GetInstanceGeometry()))
                            yield return s;
                        break;
                }
            }
        }

        // Void 자체 지오메트리(elemMinR/elemMaxR/elemMinU/elemMaxU 위치)와 일치하는 평면을
        // 대상 Solid 목록에서 찾아 face ref로 수집한다. 사각형 개구부를 절단하면 구멍의 "터널 벽면"이
        // 캡 면과 별개인 독립 PlanarFace로 생기므로, 호스트 Solid에도 그대로 재사용 가능하다.
        private void CollectAlignedFaceRefs(
            IEnumerable<Solid> solids, XYZ right, XYZ up,
            double elemMinR, double elemMaxR, double elemMinU, double elemMaxU,
            double dirTol, double areaTol, double bboxTol, string code, long eid,
            List<FaceRef> minRC, List<FaceRef> maxRC, List<FaceRef> minUC, List<FaceRef> maxUC)
        {
            foreach (var solid in solids)
            {
                foreach (Face f in solid.Faces)
                {
                    if (f is not PlanarFace pf || pf.Reference == null || pf.Area < areaTol) continue;
                    var n = pf.FaceNormal.Normalize();
                    var alignedRight = Math.Abs(Math.Abs(n.DotProduct(right)) - 1.0) < dirTol;
                    var alignedUp    = Math.Abs(Math.Abs(n.DotProduct(up))    - 1.0) < dirTol;
                    if (!alignedRight && !alignedUp) continue;

                    var bb  = pf.GetBoundingBox();
                    var uv  = (bb.Min + bb.Max) * 0.5;
                    var pt  = pf.Evaluate(uv);
                    var pr  = pt.DotProduct(right);
                    var pu  = pt.DotProduct(up);
                    var key = pf.Reference.ConvertToStableRepresentation(_doc);

                    if (alignedRight)
                    {
                        if (Math.Abs(pr - elemMinR) <= bboxTol) minRC.Add(new FaceRef(pf.Reference, pr, pf.Area, key, code, eid));
                        if (Math.Abs(pr - elemMaxR) <= bboxTol) maxRC.Add(new FaceRef(pf.Reference, pr, pf.Area, key, code, eid));
                    }
                    if (alignedUp)
                    {
                        if (Math.Abs(pu - elemMinU) <= bboxTol) minUC.Add(new FaceRef(pf.Reference, pu, pf.Area, key, code, eid));
                        if (Math.Abs(pu - elemMaxU) <= bboxTol) maxUC.Add(new FaceRef(pf.Reference, pu, pf.Area, key, code, eid));
                    }
                }
            }
        }

        // Void 요소가 실제로 뚫고 있는 호스트를 찾아 그 지오메트리에서 face ref를 대신 수집한다.
        // 1차: 구멍의 터널 벽면(독립 PlanarFace)을 직접 매칭 — 사각형 개구부의 일반적인 경우
        // 2차: 그래도 못 찾으면 캡 면의 내부 edge loop(구멍 테두리)에서 좌표가 일치하는 변을 사용
        private void CollectHoleBoundaryRefs(
            View view, Element voidElem, XYZ right, XYZ up,
            double elemMinR, double elemMaxR, double elemMinU, double elemMaxU,
            double dirTol, double bboxTol,
            List<FaceRef> minRC, List<FaceRef> maxRC, List<FaceRef> minUC, List<FaceRef> maxUC)
        {
            var host = FindCutHost(voidElem);
            if (host == null) return;

            var opt = new Options { View = view, ComputeReferences = true };
            var hostGeo = host.get_Geometry(opt);
            if (hostGeo == null) return;

            var code = voidElem.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty;
            var eid  = voidElem.Id.Value;
            const double areaTol = 1e-4;
            var hostSolids = EnumerateSolids(hostGeo).ToList();

            CollectAlignedFaceRefs(hostSolids, right, up, elemMinR, elemMaxR, elemMinU, elemMaxU,
                dirTol, areaTol, bboxTol, code, eid, minRC, maxRC, minUC, maxUC);

            // 1차에서 한쪽 축(R 또는 U)만 찾았을 수 있으므로, 어느 하나라도 성공했다고 해서
            // 폴백을 완전히 건너뛰지 않고 항상 시도한다 (누락된 축을 마저 채워준다).
            foreach (var solid in hostSolids)
            {
                foreach (Face f in solid.Faces)
                {
                    if (f is not PlanarFace pf || pf.Area < areaTol) continue;

                    var edgeLoops = pf.EdgeLoops;
                    // 0번은 면의 외곽 루프, 1번부터는 내부 루프(구멍)
                    for (int li = 1; li < edgeLoops.Size; li++)
                    {
                        var loop = edgeLoops.get_Item(li);

                        double loopMinR = double.MaxValue, loopMaxR = double.MinValue;
                        double loopMinU = double.MaxValue, loopMaxU = double.MinValue;
                        foreach (Edge edge in loop)
                        {
                            var c = edge.AsCurve();
                            foreach (var pt in new[] { c.GetEndPoint(0), c.GetEndPoint(1) })
                            {
                                var r = pt.DotProduct(right); var u = pt.DotProduct(up);
                                if (r < loopMinR) loopMinR = r; if (r > loopMaxR) loopMaxR = r;
                                if (u < loopMinU) loopMinU = u; if (u > loopMaxU) loopMaxU = u;
                            }
                        }

                        bool bboxMatches =
                            Math.Abs(loopMinR - elemMinR) <= bboxTol && Math.Abs(loopMaxR - elemMaxR) <= bboxTol &&
                            Math.Abs(loopMinU - elemMinU) <= bboxTol && Math.Abs(loopMaxU - elemMaxU) <= bboxTol;
                        if (!bboxMatches) continue;

                        foreach (Edge edge in loop)
                        {
                            if (edge.AsCurve() is not Line line || edge.Reference == null) continue;
                            var mid  = line.Evaluate(0.5, true);
                            var pr   = mid.DotProduct(right);
                            var pu   = mid.DotProduct(up);
                            var dir  = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
                            var alongUp    = Math.Abs(Math.Abs(dir.DotProduct(up))    - 1.0) < dirTol; // 좌/우측 변
                            var alongRight = Math.Abs(Math.Abs(dir.DotProduct(right)) - 1.0) < dirTol; // 상/하측 변
                            var key = edge.Reference.ConvertToStableRepresentation(_doc);

                            if (alongUp)
                            {
                                if (Math.Abs(pr - elemMinR) <= bboxTol) minRC.Add(new FaceRef(edge.Reference, pr, line.Length, key, code, eid));
                                if (Math.Abs(pr - elemMaxR) <= bboxTol) maxRC.Add(new FaceRef(edge.Reference, pr, line.Length, key, code, eid));
                            }
                            if (alongRight)
                            {
                                if (Math.Abs(pu - elemMinU) <= bboxTol) minUC.Add(new FaceRef(edge.Reference, pu, line.Length, key, code, eid));
                                if (Math.Abs(pu - elemMaxU) <= bboxTol) maxUC.Add(new FaceRef(edge.Reference, pu, line.Length, key, code, eid));
                            }
                        }
                        return; // 매칭되는 구멍을 찾았으므로 종료
                    }
                }
            }
        }

        // RevitOpeningCommandRepo가 NewFamilyInstance(location, symbol, host, level, ...)로 호스트 기반 배치를 하므로,
        // 이 void가 뚫고 있는 호스트는 InstanceVoidCutUtils(별도 Cut Geometry 관계)가 아니라
        // FamilyInstance.Host 속성에 바로 들어있다.
        private static Element FindCutHost(Element voidElem)
        {
            return (voidElem as FamilyInstance)?.Host;
        }

        // ── ref 정렬 / 병합 ───────────────────────────────────────────────────

        private static List<FaceRef> BuildOrderedDistinctRefs(List<FaceRef> refs)
        {
            var ordered = refs
                .OrderBy(r => r.Projection)
                .ThenByDescending(r => r.Area)
                .ThenBy(r => r.ElementIdValue)
                .ThenBy(r => r.StableKey)
                .ToList();
            var result = new List<FaceRef>();
            const double tol = 1.0;
            foreach (var r in ordered)
                if (result.Count == 0 || Math.Abs(r.Projection - result[^1].Projection) > tol)
                    result.Add(r);
            return result;
        }

        private static List<FaceRef> CollapseNearbyRefs(List<FaceRef> refs, double tol, bool preferSmallerArea = false)
        {
            var ordered = refs
                .OrderBy(r => r.Projection)
                .ThenBy(r => preferSmallerArea ? r.Area : -r.Area)
                .ThenBy(r => r.ElementIdValue)
                .ThenBy(r => r.StableKey)
                .ToList();
            var result = new List<FaceRef>();
            foreach (var r in ordered)
            {
                if (result.Count == 0) { result.Add(r); continue; }
                var last = result[^1];
                if (Math.Abs(r.Projection - last.Projection) <= tol)
                {
                    bool replace = preferSmallerArea
                        ? (r.Area < last.Area || (Math.Abs(r.Area - last.Area) < 1e-9 && r.ElementIdValue < last.ElementIdValue))
                        : (r.Area > last.Area || (Math.Abs(r.Area - last.Area) < 1e-9 && r.ElementIdValue < last.ElementIdValue));
                    if (replace) result[^1] = r;
                }
                else result.Add(r);
            }
            return result;
        }

        // ── 범위 계산 ────────────────────────────────────────────────────────

        private void GetModelExtents(View view, List<Element> elems, XYZ right, XYZ up,
            out double minRight, out double maxRight, out double minUp, out double maxUp)
        {
            minRight = minUp =  double.MaxValue;
            maxRight = maxUp = double.MinValue;
            foreach (var e in elems)
            {
                var bb = e.get_BoundingBox(view);
                if (bb == null) continue;
                var corners = new[]
                {
                    new XYZ(bb.Min.X, bb.Min.Y, bb.Min.Z), new XYZ(bb.Min.X, bb.Max.Y, bb.Min.Z),
                    new XYZ(bb.Max.X, bb.Min.Y, bb.Min.Z), new XYZ(bb.Max.X, bb.Max.Y, bb.Min.Z),
                    new XYZ(bb.Min.X, bb.Min.Y, bb.Max.Z), new XYZ(bb.Min.X, bb.Max.Y, bb.Max.Z),
                    new XYZ(bb.Max.X, bb.Min.Y, bb.Max.Z), new XYZ(bb.Max.X, bb.Max.Y, bb.Max.Z),
                };
                foreach (var p in corners)
                {
                    var r = p.DotProduct(right); var u = p.DotProduct(up);
                    if (r < minRight) minRight = r; if (r > maxRight) maxRight = r;
                    if (u < minUp)    minUp    = u; if (u > maxUp)    maxUp    = u;
                }
            }
            if (minRight == double.MaxValue) { minRight = maxRight = minUp = maxUp = 0; }
        }

        private void GetCropBoxExtents(View view, XYZ right, XYZ up,
            out double minR, out double maxR, out double minU, out double maxU)
        {
            minR = minU = double.MaxValue;
            maxR = maxU = double.MinValue;
            var cb = view.CropBox;
            var t  = cb.Transform;
            var locals = new[]
            {
                new XYZ(cb.Min.X, cb.Min.Y, cb.Min.Z), new XYZ(cb.Max.X, cb.Min.Y, cb.Min.Z),
                new XYZ(cb.Min.X, cb.Max.Y, cb.Min.Z), new XYZ(cb.Max.X, cb.Max.Y, cb.Min.Z),
                new XYZ(cb.Min.X, cb.Min.Y, cb.Max.Z), new XYZ(cb.Max.X, cb.Min.Y, cb.Max.Z),
                new XYZ(cb.Min.X, cb.Max.Y, cb.Max.Z), new XYZ(cb.Max.X, cb.Max.Y, cb.Max.Z),
            };
            foreach (var l in locals)
            {
                var w = t.OfPoint(l);
                var r = w.DotProduct(right); var u = w.DotProduct(up);
                if (r < minR) minR = r; if (r > maxR) maxR = r;
                if (u < minU) minU = u; if (u > maxU) maxU = u;
            }
        }

        private bool IsWithinViewRange(Element e, View view, XYZ right, XYZ up,
            double minR, double maxR, double minU, double maxU)
        {
            var bb = e.get_BoundingBox(view);
            if (bb == null) return false;
            var corners = new[]
            {
                new XYZ(bb.Min.X, bb.Min.Y, bb.Min.Z), new XYZ(bb.Min.X, bb.Max.Y, bb.Min.Z),
                new XYZ(bb.Max.X, bb.Min.Y, bb.Min.Z), new XYZ(bb.Max.X, bb.Max.Y, bb.Min.Z),
                new XYZ(bb.Min.X, bb.Min.Y, bb.Max.Z), new XYZ(bb.Min.X, bb.Max.Y, bb.Max.Z),
                new XYZ(bb.Max.X, bb.Min.Y, bb.Max.Z), new XYZ(bb.Max.X, bb.Max.Y, bb.Max.Z),
            };
            var eMinR = corners.Min(p => p.DotProduct(right));
            var eMaxR = corners.Max(p => p.DotProduct(right));
            var eMinU = corners.Min(p => p.DotProduct(up));
            var eMaxU = corners.Max(p => p.DotProduct(up));
            return eMaxR >= minR && eMinR <= maxR && eMaxU >= minU && eMinU <= maxU;
        }

        // ── 치수선 생성 ──────────────────────────────────────────────────────

        private void CreateSegmentDimensionsAtTop(View view, List<FaceRef> refs, XYZ lineDir, XYZ upDir, double coord, ElementId dimTypeId)
        {
            var list = BuildOrderedDistinctRefs(refs);
            for (int i = 0; i < list.Count - 1; i++)
                TryCreateDimension(view, lineDir * list[i].Projection + upDir * coord, lineDir * list[i + 1].Projection + upDir * coord, list[i].Reference, list[i + 1].Reference, dimTypeId);
        }

        private void CreateOverallDimensionAtTop(View view, List<FaceRef> refs, XYZ lineDir, XYZ upDir, double coord, ElementId dimTypeId)
        {
            var list = BuildOrderedDistinctRefs(refs);
            if (list.Count < 2) return;
            TryCreateDimension(view, lineDir * list.First().Projection + upDir * coord, lineDir * list.Last().Projection + upDir * coord, list.First().Reference, list.Last().Reference, dimTypeId);
        }

        private void CreateSegmentDimensionAtBottom(View view, List<FaceRef> refs, XYZ lineDir, XYZ upDir, double coord, ElementId dimTypeId)
            => CreateSegmentDimensionsAtTop(view, refs, lineDir, upDir, coord, dimTypeId);

        private void CreateOverallDimensionAtBottom(View view, List<FaceRef> refs, XYZ lineDir, XYZ upDir, double coord, ElementId dimTypeId)
            => CreateOverallDimensionAtTop(view, refs, lineDir, upDir, coord, dimTypeId);

        private void CreateSegmentDimensionsAtLeft(View view, List<FaceRef> refs, XYZ lineDir, XYZ rightDir, double coord, ElementId dimTypeId)
        {
            var list = BuildOrderedDistinctRefs(refs);
            for (int i = 0; i < list.Count - 1; i++)
                TryCreateDimension(view, lineDir * list[i].Projection + rightDir * coord, lineDir * list[i + 1].Projection + rightDir * coord, list[i].Reference, list[i + 1].Reference, dimTypeId);
        }

        private void CreateOverallDimensionsAtLeft(View view, List<FaceRef> refs, XYZ lineDir, XYZ rightDir, double coord, ElementId dimTypeId)
        {
            var list = BuildOrderedDistinctRefs(refs);
            if (list.Count < 2) return;
            TryCreateDimension(view, lineDir * list.First().Projection + rightDir * coord, lineDir * list.Last().Projection + rightDir * coord, list.First().Reference, list.Last().Reference, dimTypeId);
        }

        private void CreateSegmentDimensionAtRight(View view, List<FaceRef> refs, XYZ lineDir, XYZ rightDir, double coord, ElementId dimTypeId)
            => CreateSegmentDimensionsAtLeft(view, refs, lineDir, rightDir, coord, dimTypeId);

        private void CreateOverallDimensionAtRight(View view, List<FaceRef> refs, XYZ lineDir, XYZ rightDir, double coord, ElementId dimTypeId)
            => CreateOverallDimensionsAtLeft(view, refs, lineDir, rightDir, coord, dimTypeId);

        // ── 치수선 생성 헬퍼 ─────────────────────────────────────────────────

        private void TryCreateDimension(View view, XYZ p1, XYZ p2, Reference r1, Reference r2, ElementId dimTypeId)
        {
            if (p1.DistanceTo(p2) < 1e-6) return;
            if (HasSameDimension(view, p1, p2, r1, r2)) return;
            var ra = new ReferenceArray();
            ra.Append(r1); ra.Append(r2);
            try
            {
                var dim = _doc.Create.NewDimension(view, Line.CreateBound(p1, p2), ra);
                if (dim != null && dimTypeId != ElementId.InvalidElementId)
                    dim.ChangeTypeId(dimTypeId);
            }
            catch { }
        }

        private bool HasSameDimension(View view, XYZ p1, XYZ p2, Reference r1, Reference r2)
        {
            var k1 = r1.ConvertToStableRepresentation(_doc);
            var k2 = r2.ConvertToStableRepresentation(_doc);
            const double ptTol = 1.0;
            foreach (var dim in new FilteredElementCollector(_doc, view.Id).OfClass(typeof(Dimension)).Cast<Dimension>())
            {
                var refs = dim.References;
                if (refs == null || refs.Size != 2) continue;
                var a = refs.get_Item(0)?.ConvertToStableRepresentation(_doc);
                var b = refs.get_Item(1)?.ConvertToStableRepresentation(_doc);
                if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) continue;
                if (!((a == k1 && b == k2) || (a == k2 && b == k1))) continue;
                if (dim.Curve is not Line line || !line.IsBound) continue;
                var d0 = line.GetEndPoint(0); var d1 = line.GetEndPoint(1);
                if ((d0.DistanceTo(p1) <= ptTol && d1.DistanceTo(p2) <= ptTol) ||
                    (d0.DistanceTo(p2) <= ptTol && d1.DistanceTo(p1) <= ptTol)) return true;
            }
            return false;
        }

        // ── 치수 유형 조회 ───────────────────────────────────────────────────

        private ElementId GetDimensionTypeId(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return ElementId.InvalidElementId;
            var type = new FilteredElementCollector(_doc)
                .OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
                .FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return type?.Id ?? ElementId.InvalidElementId;
        }

        // ── 내부 타입 ────────────────────────────────────────────────────────

        private class ViewDimensionPreset
        {
            public bool UseTop           { get; set; }
            public bool UseBottom        { get; set; }
            public bool UseLeft          { get; set; }
            public bool UseRight         { get; set; }
            public bool UseTopOverall    { get; set; }
            public bool UseBottomOverall { get; set; }
            public bool UseLeftOverall   { get; set; }
            public bool UseRightOverall  { get; set; }

            public DimensionFilterRule TopRule    { get; } = new();
            public DimensionFilterRule BottomRule { get; } = new();
            public DimensionFilterRule LeftRule   { get; } = new();
            public DimensionFilterRule RightRule  { get; } = new();
        }

        private class DimensionFilterRule
        {
            public HashSet<BuiltInCategory> IncludeCategories   { get; } = new();
            public HashSet<BuiltInCategory> ExcludeCategories   { get; } = new();
            public string[] IncludeNameKeywords { get; set; } = Array.Empty<string>();
            public string[] ExcludeNameKeywords { get; set; } = Array.Empty<string>();
            public string IncludeParameterName  { get; set; }
            public string IncludeParameterValue { get; set; }
            public string ExcludeParameterName  { get; set; }
            public string ExcludeParameterValue { get; set; }
        }

        private class FaceRef
        {
            public Reference Reference    { get; }
            public double    Projection   { get; }
            public double    Area         { get; }
            public string    StableKey    { get; }
            public string    ElementCode  { get; }
            public long      ElementIdValue { get; }

            public FaceRef(Reference reference, double projection, double area, string stableKey, string elementCode, long elementIdValue)
            {
                Reference      = reference;
                Projection     = projection;
                Area           = area;
                StableKey      = stableKey      ?? string.Empty;
                ElementCode    = elementCode    ?? string.Empty;
                ElementIdValue = elementIdValue;
            }
        }
    }
}
