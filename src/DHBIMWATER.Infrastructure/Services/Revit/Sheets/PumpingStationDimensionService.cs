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

        private const double SegmentOffset = 2.5;   // ft
        private const double OverallOffset  = 5.0;   // ft

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
            ("TYPE1_좌안부", "상부슬래브",    "W1-1,W1-2W,W1-3,W2,G1",  "W1,W2,W4,G1",               "W1,W1-1,W1-2,W1-3,W3",  "W1,W1-3,W2,W5,"            ),
            ("TYPE1_좌안부", "기초(유입부)",  "W1-1,W1-2W,W1-3,W2,F2",  "W1,W2,AVW",                 "W1,W1-1,W3",            "W1.W1-3,W5,AVW"            ),
            ("TYPE1_좌안부", "A",             "S1,W2,W4,G1",            "W2,F1,F2,AVW",              "S1,W2,MS1,F1,F2",       "S1,G1,F1,F2"               ),
            ("TYPE1_좌안부", "B",             "S1,G1,F1,F2",            "F1,F2,W3-1",                "S1,MS1,F1,F2",          "S1,F1,F2,G1"               ),
            ("TYPE1_좌안부", "C",             "S1,W1-2,W1-3,W2",        "F1,F2,W1-2,W1-3,W2",        "S1,F1,F2,W1-3,W2",      "S1,F1,F2,W1-2,W1-3"        ),
            ("TYPE1_좌안부", "D",             "S1,W1-2,W2,G1,MS1",      "F1,F2,W1-2,W2,MS1",         "S1,F1,F2,W2,MS1",       "S1,F1,F2,G1,MS1"           ),
            ("TYPE1_좌안부", "E",             "F1,F2,W1-2,W1-3,W2",     "F2,W5,W1-2,W2",             "F2,W1-1,W1-2,W1-3",     "F2,W1-3,W2,W5"             ),
            ("TYPE1_좌안부", "F",             "S1,W1,W1-1,W3",          "F1,F2,W1,W1-1,W3",          "S1,W1-1,F1,F2",         "S1,W1,F1,F2"               ),
            ("TYPE1_좌안부", "G",             "S1,W1,W1-1,W3,G1",       "F1,F2,W1,W1-1,W3",          "S1,G1,W1-1,F1,F2",      "S1,G1,W1,F1,F2"            ),
            ("TYPE1_좌안부", "H",             "S1,W1,W1-3,W3,W5,G1",    "W1,W1-3,F1,F2",             "S1,W1-3,F1,F2",         "S1,W1,W3,F1,F2,G1"         ),
            ("TYPE1_좌안부", "I",             "S1,W1,W1-3,W3,W5",       "S1,W1,W1-3,W3,W5",          "S1,W1-3,F1,F2",         "S1,W1,F1,F2"               ),
            ("TYPE1_좌안부", "J",             "S1,W1,W1-3,W3,W5",       "F1,F1,W1,W1-3,W3,W5,AVW",   "S1,W1-3,F1,F2,MS1",     "S1,W1,AVW,F1,F2,MS1"       ),
            ("TYPE1_좌안부", "K",             "S1,W1,W1-3,W5",          "F1,F2,W1,W1-3,W3-1,W5,AVW", "S1,F1,F2,W1-3",         "S1,F1,F2,W1,MS1"           ),

            // ── TYPE1_우안부 ──────────────────────────────────────────────────
            ("TYPE1_우안부", "상부슬래브",    "S1,W1,W2,W4,G1",         "W1-2,W1-3,W2",              "S1,W1,W1-1,W3 ",        "S1,W1,W1-3,W3,W5"          ),
            ("TYPE1_우안부", "기초(유입부)",  "F1,F2,W1,W2,AVW",        "F2,F1,W1-1,W1-2,W1-3,W2",   "F1,F2,W1,W1-1,W3",      "F1,F2,W1,W1-3,W3,W5,AVW"   ),
            ("TYPE1_우안부", "A",             "S1,G1,W2,W4,MS1",        "F1,F2,W2,W3,AVW",           "S1,F1,F2",              "S1,W2,F1,F2,MS1"           ),
            ("TYPE1_우안부", "B",             "S1,G1,W2,W4,MS1",        "F1,F2,W2,W3,AVW",           "S1,F1,F2",              "S1,W2,F1,F2MS1"            ),
            ("TYPE1_우안부", "C",             "S1,W1-2,W2",             "F1,F2,W1-2,W2",             "W1-2,S1,F1,F2",         "W2,S1,F1,F2"               ),
            ("TYPE1_우안부", "D",             "S1,W1-2,W2,W5,G1,MS1",   "F1,F2,W1-2,B5",             "S1,F1,F2,W1-2,G1,W5",   "S1,F1,F2,W2,W5,MS1"        ),
            ("TYPE1_우안부", "E",             "W5,W1-2,W2,W4",          "W1-2,W1-3,W2",              "W1-1,W1-2,W1-3,W5",     "W2,W1-3,W5"                ),
            ("TYPE1_우안부", "F",             "S1,W1,W1-1,W3",          "F1,F2,W1,W1-1,W3",          "S1,W1,F1,F2",           "S1,W1-1,F1,F2"             ),
            ("TYPE1_우안부", "G",             "S1,W1,W1-1,W3",          "F1,F2,W1,W1-1,W3",          "S1,G1,W1,F1,F2",        "S1,G1,W1-1,F1,F2"          ),
            ("TYPE1_우안부", "H",             "S1,W1,W1-3,W3,W5,G1",    "W1,W1-3,F1,F2",             "S1,W1,F1,F2,G1",        "S1,W1-3,F1,F2,G1"          ),
            ("TYPE1_우안부", "I",             "S1,W1-3,W3,W5",          "F1,F2,W1-3,W3,W5",          "S1,F1,F2,W1",           "S1,F1,F2,W1-3"             ),
            ("TYPE1_우안부", "J",             "S1,W1,W1-3,W3,W5",       "F1,F2,W1,W1-3,W3,W5,AVW",   "S1,F1,F2,MS1,W1",       "S1,F1,F2,W1-3"             ),
            ("TYPE1_우안부", "K",             "S1,W1,W1-3,W5",          "F1,F2,W1,W1-3,W5,W3-1,AVW", "S1,F1,F2,MS1,W1",       "S1,F1,F2,W1-3"             ),

            // ── TYPE1_측면부 ──────────────────────────────────────────────────
            ("TYPE1_측면부", "상부슬래브",    "S1,W1,W2,W4,G1",         "S1,W1,W2,W4,G1",            "S1,W1,W3",              "S1,W1,W2,W3"               ),
            ("TYPE1_측면부", "기초(유입부)",  "F1,F2,W1,W2,AVW",        "F1,F2,W1,W2,AVW",           "F1,F2,W1,W3",           "F1,F2,W1,W3-1,AVW"         ),
            ("TYPE1_측면부", "A",             "S1,G1,W2,W4,MS1",        "F1,F2,W2,AVW",              "S1,F1,F2,G1",           "S1,F1,F2,MS1,AVW"          ),
            ("TYPE1_측면부", "B",             "S1,G1,W2,W4",            "F1,F2,W2",                  "S1,F1,F2,",             "S1,F1,F2,MS1"              ),
            ("TYPE1_측면부", "C",             "",                       "",                          "",                      ""                          ),
            ("TYPE1_측면부", "D",             "",                       "",                          "",                      ""                          ),
            ("TYPE1_측면부", "E",             "",                       "",                          "",                      ""                          ),
            ("TYPE1_측면부", "F",             "S1,W1,W3",               "F1,F2,W1,W3",               "S1,F1,F2,W1",           "S1,F1,F2,W1"               ),
            ("TYPE1_측면부", "G",             "S1,W1,W3,G1",            "F1,F2,W1,W3",               "S1,F1,F2,W1,G1",        "S1,F1,F2,W1,G1"            ),
            ("TYPE1_측면부", "H",             "S1,W1,W3",               "F1,F2,W1",                  "S1,F1,F2,W1,W3",        "S1,F1,F2,W1,W3"            ),
            ("TYPE1_측면부", "I",             "S1,W1,W3",               "F1,F2,W1,W3",               "S1,F1,F2,W1",           "S1,F1,F2,W1"               ),
            ("TYPE1_측면부", "J",             "S1,W1,W3,",              "F1,F2,W1,W3,AVW",           "S1,F1,F2,W1,MS1",       "S1,F1,F2,W1,MS1"           ),
            ("TYPE1_측면부", "K",             "S1,W1",                  "F1,F2,W1,W3-1,AVW",         "S1,F1,F2,MS1",          "S1,F1,F2,MS1"              ),

            // ── TYPE2_측면부 ──────────────────────────────────────────────────
            ("TYPE2_측면부", "상부슬래브",    "",                       "",                          "",                      ""                          ),
            ("TYPE2_측면부", "기초(유입부)",  "",                       "",                          "",                      ""                          ),
            ("TYPE2_측면부", "A",             "",                       "",                          "",                      ""                          ),
            ("TYPE2_측면부", "B",             "",                       "",                          "",                      ""                          ),
            ("TYPE2_측면부", "C",             "",                       "",                          "",                      ""                          ),
            ("TYPE2_측면부", "D",             "",                       "",                          "",                      ""                          ),
            ("TYPE2_측면부", "E",             "",                       "",                          "",                      ""                          ),
            ("TYPE2_측면부", "F",             "",                       "",                          "",                      ""                          ),
            ("TYPE2_측면부", "G",             "",                       "",                          "",                      ""                          ),
            ("TYPE2_측면부", "H",             "",                       "",                          "",                      ""                          ),
            ("TYPE2_측면부", "I",             "",                       "",                          "",                      ""                          ),
            ("TYPE2_측면부", "J",             "",                       "",                          "",                      ""                          ),
            ("TYPE2_측면부", "K",             "",                       "",                          "",                      ""                          ),

            // ── TYPE3_측면부 ──────────────────────────────────────────────────
            ("TYPE3_측면부", "상부슬래브",    "",                       "",                          "",                      ""                          ),
            ("TYPE3_측면부", "기초(유입부)",  "",                       "",                          "",                      ""                          ),
            ("TYPE3_측면부", "A",             "",                       "",                          "",                      ""                          ),
            ("TYPE3_측면부", "B",             "",                       "",                          "",                      ""                          ),
            ("TYPE3_측면부", "C",             "",                       "",                          "",                      ""                          ),
            ("TYPE3_측면부", "D",             "",                       "",                          "",                      ""                          ),
            ("TYPE3_측면부", "E",             "",                       "",                          "",                      ""                          ),
            ("TYPE3_측면부", "F",             "",                       "",                          "",                      ""                          ),
            ("TYPE3_측면부", "G",             "",                       "",                          "",                      ""                          ),
            ("TYPE3_측면부", "H",             "",                       "",                          "",                      ""                          ),
            ("TYPE3_측면부", "I",             "",                       "",                          "",                      ""                          ),
            ("TYPE3_측면부", "J",             "",                       "",                          "",                      ""                          ),
            ("TYPE3_측면부", "K",             "",                       "",                          "",                      ""                          ),
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

            foreach (var obj in geo)
            {
                if (obj is not Solid solid || solid.Faces.IsEmpty) continue;
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
                    var code = e.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty;
                    var eid  = e.Id.Value;

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

            minRight = minRC.OrderByDescending(x => x.Area).ThenBy(x => x.StableKey).FirstOrDefault();
            maxRight = maxRC.OrderByDescending(x => x.Area).ThenBy(x => x.StableKey).FirstOrDefault();
            minUp    = minUC.OrderByDescending(x => x.Area).ThenBy(x => x.StableKey).FirstOrDefault();
            maxUp    = maxUC.OrderByDescending(x => x.Area).ThenBy(x => x.StableKey).FirstOrDefault();
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
