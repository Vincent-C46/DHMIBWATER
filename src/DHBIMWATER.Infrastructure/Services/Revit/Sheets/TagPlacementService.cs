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

        public TagPlacementService(Document doc)
        {
            _doc = doc;
        }

        public void Apply(IList<string> selectedFamilyIds = null)
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
                ProcessView(view, tagTypes);

            tx.Commit();
        }

        // 현재 활성 뷰에 선택한 태그 패밀리만 배치 (targetElementIds가 있으면 해당 요소만 대상)
        public void ApplyToCurrentView(IList<string> selectedFamilyIds, IList<string> targetElementIds = null)
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

            using var tx = new Transaction(_doc, "DH 태그 배치");
            tx.Start();

            RemoveExistingTags(new List<View> { view }, allTagTypes);

            var placedBoxes = new List<Outline>();
            foreach (var tagType in tagTypes)
                TryPlaceTag(view, tagType, placedBoxes, targetIds);

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

        private void ProcessView(View view, List<FamilySymbol> tagTypes)
        {
            var placedBoxes = new List<Outline>();
            foreach (var tagType in tagTypes)
                TryPlaceTag(view, tagType, placedBoxes);
        }

        private void TryPlaceTag(View view, FamilySymbol tagType, List<Outline> placedBoxes, HashSet<long> targetIds = null)
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
            }
            catch { return; }

            var right = view.RightDirection.Normalize();
            var up    = view.UpDirection.Normalize();

            foreach (var elem in elems)
            {
                try
                {
                    var bb = elem.get_BoundingBox(view);
                    if (bb == null) continue;
                    var center = (bb.Min + bb.Max) * 0.5;
                    var tagPos = center + right.Multiply(300.0 / 304.8) + up.Multiply(300.0 / 304.8);

                    var @ref = new Reference(elem);
                    var tag  = IndependentTag.Create(_doc, tagType.Id, view.Id, @ref, true, TagOrientation.Horizontal, tagPos);
                    if (tag == null) continue;

                    tag.LeaderEndCondition = LeaderEndCondition.Free;
                    try { tag.SetLeaderEnd(@ref, center); } catch { }

                    AvoidOverlap(tag, view, up, placedBoxes);
                }
                catch { }
            }
        }

        // 같은 뷰 안에서 이미 배치된 태그와 겹치면 위쪽으로 밀어 올려서 겹침 방지
        private void AvoidOverlap(IndependentTag tag, View view, XYZ shiftDir, List<Outline> placedBoxes)
        {
            const int maxAttempts = 12;
            double step = 300.0 / 304.8;

            for (int i = 0; i < maxAttempts; i++)
            {
                var bb = tag.get_BoundingBox(view);
                if (bb == null) return;
                var outline = new Outline(bb.Min, bb.Max);

                if (!placedBoxes.Any(o => o.Intersects(outline, 1e-6)))
                {
                    placedBoxes.Add(outline);
                    return;
                }

                try { tag.TagHeadPosition += shiftDir.Multiply(step); }
                catch { break; }
            }

            var finalBb = tag.get_BoundingBox(view);
            if (finalBb != null)
                placedBoxes.Add(new Outline(finalBb.Min, finalBb.Max));
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
