using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Revit.Sheets;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class TagPlacementService
    {
        private readonly Document _doc;

        private static readonly string[] PlanViewNames = { "상부슬래브", "기초(유입부)" };

        // 태그 카테고리(ElementId) → 요소 카테고리 매핑을 BuiltInCategory 명명 규칙으로 자동 생성
        // (예: OST_WallTags → OST_Walls, OST_RoomTags → OST_Rooms) — 특정 8개 카테고리에 한정하지 않음
        private static readonly Dictionary<ElementId, BuiltInCategory> TagCategoryMap = BuildTagCategoryMap();

        private static Dictionary<ElementId, BuiltInCategory> BuildTagCategoryMap()
        {
            var map = new Dictionary<ElementId, BuiltInCategory>();
            var names = Enum.GetNames(typeof(BuiltInCategory));
            var nameSet = new HashSet<string>(names);

            foreach (var name in names)
            {
                if (!name.EndsWith("Tags", StringComparison.Ordinal)) continue;
                var baseName = name[..^"Tags".Length];

                string hostName = null;
                if (nameSet.Contains(baseName)) hostName = baseName;
                else if (nameSet.Contains(baseName + "s")) hostName = baseName + "s";
                else if (baseName.EndsWith("y") && nameSet.Contains(baseName[..^1] + "ies")) hostName = baseName[..^1] + "ies";

                if (hostName == null) continue;

                try
                {
                    var tagCat = (BuiltInCategory)Enum.Parse(typeof(BuiltInCategory), name);
                    var hostCat = (BuiltInCategory)Enum.Parse(typeof(BuiltInCategory), hostName);
                    map[new ElementId(tagCat)] = hostCat;
                }
                catch { }
            }

            return map;
        }

        private const string DhElementCodeParameterName = "DH_ElementCode";
        private const string DhPartParameterName = "DH_Part";

        public TagPlacementService(Document doc)
        {
            _doc = doc;
        }

        // 단면 뷰(A, B, C...)처럼 개별 키가 없을 때 공통으로 적용할 기본 필터 키
        public const string DefaultSectionFilterKey = "*";

        // viewFilters: 뷰 이름(예: "상부슬래브", "기초(유입부)", "A", "B"...) → (허용 ElementCode 목록, 허용 Part 목록)
        // 특정 뷰가 딕셔너리에 없으면 DefaultSectionFilterKey("*") 항목을 찾고, 그것도 없으면 필터 없이 전체 허용
        public void Apply(IList<string> selectedFamilyIds = null, IDictionary<string, (IList<string> Codes, IList<string> Parts)> viewFilters = null)
        {
            var allTagTypes = GetTagFamilySymbols();
            var tagTypes = selectedFamilyIds != null
                ? allTagTypes.Where(fs => selectedFamilyIds.Contains(fs.Id.Value.ToString())).ToList()
                : allTagTypes;

            var views = GetAllPumpingStationViews();
            if (views.Count == 0) return;

            using var tx = new Transaction(_doc, "DH 태그 배치");
            tx.Start();

            RemoveExistingTags(views, allTagTypes);

            foreach (var view in views)
            {
                (IList<string> Codes, IList<string> Parts) filter = (null, null);
                var baseName = GetBaseViewName(view);
                if (viewFilters == null || !viewFilters.TryGetValue(baseName, out filter))
                    viewFilters?.TryGetValue(DefaultSectionFilterKey, out filter);

                // null = 필터 없음(전체 허용), 빈 배열([])이면 아무것도 매칭 안 됨(전체 차단)
                var codeSet = filter.Codes != null ? new HashSet<string>(filter.Codes, StringComparer.OrdinalIgnoreCase) : null;
                var partSet = filter.Parts != null ? new HashSet<string>(filter.Parts, StringComparer.OrdinalIgnoreCase) : null;

                ProcessView(view, tagTypes, codeSet, partSet);
            }

            tx.Commit();
        }

        // 뷰에 배치된 대상 카테고리 요소들 중 DH_ElementCode/DH_Part 값의 고유 목록 조회 (필터 UI 구성용)
        public List<string> GetAvailableElementCodes() => GetDistinctParameterValues(DhElementCodeParameterName);
        public List<string> GetAvailableParts() => GetDistinctParameterValues(DhPartParameterName);

        private List<string> GetDistinctParameterValues(string parameterName)
        {
            var elemCats = TagCategoryMap.Values.Distinct().ToList();
            var views = GetAllPumpingStationViews();
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var view in views)
            {
                foreach (var elemCat in elemCats)
                {
                    IList<Element> elems;
                    try
                    {
                        elems = new FilteredElementCollector(_doc, view.Id)
                            .OfCategory(elemCat)
                            .WhereElementIsNotElementType()
                            .ToElements();
                    }
                    catch { continue; }

                    foreach (var elem in elems)
                    {
                        var val = elem.LookupParameter(parameterName)?.AsString();
                        if (!string.IsNullOrWhiteSpace(val))
                            values.Add(val);
                    }
                }
            }

            return values.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();
        }

        // 현재 활성 뷰에 선택한 태그 패밀리만 배치 (targetElementIds가 있으면 해당 요소만 대상)
        public void ApplyToCurrentView(IList<string> selectedFamilyIds, IList<string> targetElementIds = null, IList<string> allowedElementCodes = null, IList<string> allowedParts = null)
        {
            var view = _doc.ActiveView;
            if (view == null) return;

            var allTagTypes = GetTagFamilySymbols();
            var tagTypes = selectedFamilyIds != null
                ? allTagTypes.Where(fs => selectedFamilyIds.Contains(fs.Id.Value.ToString())).ToList()
                : allTagTypes;
            if (tagTypes.Count == 0) return;

            HashSet<long> targetIds = null;
            if (targetElementIds != null)
            {
                targetIds = targetElementIds
                    .Select(x => long.TryParse(x, out var v) ? v : (long?)null)
                    .Where(v => v.HasValue)
                    .Select(v => v.Value)
                    .ToHashSet();

                if (targetIds.Count == 0) return;
            }

            var codeSet = allowedElementCodes != null ? new HashSet<string>(allowedElementCodes, StringComparer.OrdinalIgnoreCase) : null;
            var partSet = allowedParts != null ? new HashSet<string>(allowedParts, StringComparer.OrdinalIgnoreCase) : null;

            using var tx = new Transaction(_doc, "DH 태그 배치");
            tx.Start();

            RemoveExistingTags(new List<View> { view }, allTagTypes);

            foreach (var tagType in tagTypes)
                TryPlaceTag(view, tagType, targetIds, codeSet, partSet);

            tx.Commit();
        }

        // 재실행 시 누적되지 않도록 이전에 배치된 태그를 먼저 제거
        private void RemoveExistingTags(List<View> views, List<FamilySymbol> managedTagTypes)
        {
            var familyIds = managedTagTypes.Select(fs => fs.Family.Id).ToHashSet();
            if (familyIds.Count == 0) return;

            foreach (var view in views)
            {
                var toDelete = new FilteredElementCollector(_doc, view.Id)
                    .OfClass(typeof(IndependentTag))
                    .Cast<IndependentTag>()
                    .Where(tag =>
                    {
                        try
                        {
                            return _doc.GetElement(tag.GetTypeId()) is FamilySymbol fs
                                && familyIds.Contains(fs.Family.Id);
                        }
                        catch { return false; }
                    })
                    .Select(tag => tag.Id)
                    .ToList();

                if (toDelete.Count > 0)
                    _doc.Delete(toDelete);
            }
        }

        public List<TagFamilyDto> GetAvailableTagFamilies()
        {
            return GetTagFamilySymbols()
                .Select(fs => new TagFamilyDto
                {
                    Id = fs.Id.Value.ToString(),
                    Name = fs.Family.Name
                })
                .ToList();
        }

        private void ProcessView(View view, List<FamilySymbol> tagTypes, HashSet<string> allowedCodes = null, HashSet<string> allowedParts = null)
        {
            foreach (var tagType in tagTypes)
                TryPlaceTag(view, tagType, targetIds: null, allowedCodes, allowedParts);
        }

        private void TryPlaceTag(View view, FamilySymbol tagType, HashSet<long> targetIds = null, HashSet<string> allowedCodes = null, HashSet<string> allowedParts = null)
        {
            if (tagType.Category?.Id == null) return;
            if (!TagCategoryMap.TryGetValue(tagType.Category.Id, out var elemCat)) return;

            try { if (!tagType.IsActive) tagType.Activate(); }
            catch { return; }

            IList<Element> elems;
            try
            {
                elems = new FilteredElementCollector(_doc, view.Id)
                    .OfCategory(elemCat)
                    .WhereElementIsNotElementType()
                    .ToElements();

                if (targetIds != null)
                    elems = elems.Where(e => targetIds.Contains(e.Id.Value)).ToList();

                // DH_ElementCode 또는 DH_Part 둘 중 하나라도 허용 목록에 매칭되면 대상 (OR)
                // 두 파라미터 자체가 없는 요소(예: 레벨)는 필터 대상이 아니므로 항상 통과
                if (allowedCodes != null || allowedParts != null)
                {
                    elems = elems.Where(e =>
                    {
                        var codeParam = e.LookupParameter(DhElementCodeParameterName);
                        var partParam = e.LookupParameter(DhPartParameterName);
                        if (codeParam == null && partParam == null) return true;

                        var codeMatch = allowedCodes != null && codeParam != null && allowedCodes.Contains(codeParam.AsString() ?? string.Empty);
                        var partMatch = allowedParts != null && partParam != null && allowedParts.Contains(partParam.AsString() ?? string.Empty);
                        return codeMatch || partMatch;
                    }).ToList();
                }
            }
            catch { return; }

            var right = view.RightDirection.Normalize();
            var up    = view.UpDirection.Normalize();
            var offset = 600.0 / 304.8;

            foreach (var elem in elems)
            {
                try
                {
                    var bb = elem.get_BoundingBox(view);
                    if (bb == null) continue;
                    var center = (bb.Min + bb.Max) * 0.5;
                    var tagPos = center + right.Multiply(offset) + up.Multiply(offset);

                    var @ref = new Reference(elem);

                    // addLeader=true -> Revit이 직각(ㄴ자) 리더를 자동으로 배치
                    var tag = IndependentTag.Create(_doc, tagType.Id, view.Id, @ref, true, TagOrientation.Horizontal, tagPos);
                    if (tag == null) continue;

                    _doc.Regenerate();

                    // 자동 배치된 리더 형태(엘보우 위치)는 그대로 두고,
                    // 끝점 조건만 Free(열린 끝)로 바꿔서 배치 후 수동 드래그가 가능하도록 함
                    try { tag.LeaderEndCondition = LeaderEndCondition.Free; } catch { }
                }
                catch { }
            }
        }

        private List<FamilySymbol> GetTagFamilySymbols()
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(fs =>
                {
                    try
                    {
                        if (fs.Category?.Id == null) return false;
                        return TagCategoryMap.ContainsKey(fs.Category.Id);
                    }
                    catch { return false; }
                })
                // 같은 패밀리에 유형이 여러 개면 이름 오름차순 첫번째 유형만 사용
                .GroupBy(fs => fs.Family.Id)
                .Select(g => g
                    .OrderBy(fs => fs.Name, StringComparer.OrdinalIgnoreCase)
                    .First())
                .ToList();
        }

        private List<View> GetAllPumpingStationViews() =>
            new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate && IsPumpingStationView(v))
                .ToList();

        // "상부슬래브_시트" → "상부슬래브", "A_시트" → "A" 처럼 뷰 필터 키로 쓰는 기본 이름 추출
        private string GetBaseViewName(View view)
        {
            var name = view.Name;
            return name.EndsWith("_시트", StringComparison.OrdinalIgnoreCase)
                ? name[..^"_시트".Length]
                : name;
        }

        private bool IsPumpingStationView(View view)
        {
            var name = view.Name;
            if (!name.EndsWith("_시트", StringComparison.OrdinalIgnoreCase)) return false;
            var baseName = name[..^"_시트".Length];
            return PlanViewNames.Any(p => p.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                || (baseName.Length == 1 && char.IsUpper(baseName[0]));
        }
    }
}
