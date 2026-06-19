using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class TagService
    {
        private const string DhElementCodeParameterName = "DH_ElementCode";
        private const string ElementCodeParameterName = "ElementCode";
        private const int MaxTagsPerRepeatedCode = 3;
        private const double MinTagSpacingRatio = 0.28;

        private readonly Document _doc;

        public TagService(Document doc)
        {
            _doc = doc;
        }

        private XYZ GetTagPoint(Element element, View view)
        {
            if (element is Wall wall)
            {
                var lc = wall.Location as LocationCurve;
                if (lc?.Curve == null)
                    return null;

                return lc.Curve.Evaluate(0.5, true);
            }

            var bb = element.get_BoundingBox(view);
            if (bb == null)
                return null;

            return (bb.Min + bb.Max) * 0.5;
        }

        private void ApplyTagsToAllOnView(View view)
        {
            var ids = GetTaggableElementsOnView(view)
                .Select(e => e.Id)
                .ToList();

            ApplyTagsToElementsOnView(view, ids);
        }

        private void ApplyTagsToElementsOnView(View view, IList<ElementId> ids)
        {
            if (ids == null || ids.Count == 0)
                return;

            var elements = ids
                .Select(id => _doc.GetElement(id))
                .Where(IsTaggableModelElement)
                .Select(e => new TagTarget(e, GetElementCode(e), GetTagPoint(e, view)))
                .Where(x => !string.IsNullOrWhiteSpace(x.ElementCode))
                .Where(x => x.Point != null)
                .ToList();

            if (elements.Count == 0)
                return;

            using (var tx = new Transaction(_doc, "Apply Tags On Current View"))
            {
                tx.Start();

                RemoveDuplicateTagsOnView(view);
                var existingTagCounts = BuildExistingTagCounts(view);
                var existingTaggedIdsByKey = BuildExistingTaggedIdsByKey(view);

                foreach (var target in SelectMultiCategoryTagTargets(elements))
                {
                    var key = BuildMultiCategoryTagKey(target.ElementCode);
                    if (GetTagCount(existingTagCounts, key) >= GetMaxTagCountForKey(key))
                        continue;

                    if (IsElementAlreadyTagged(existingTaggedIdsByKey, key, target.Element.Id))
                        continue;

                    if (TryCreateTag(view, target.Element, TagMode.TM_ADDBY_MULTICATEGORY, false))
                    {
                        IncrementTagCount(existingTagCounts, key);
                        AddTaggedElement(existingTaggedIdsByKey, key, target.Element.Id);
                    }
                }

                foreach (var target in SelectCategoryTagTargets(elements))
                {
                    var key = BuildCategoryTagKey(target.Element, target.ElementCode);
                    if (GetTagCount(existingTagCounts, key) >= GetMaxTagCountForKey(key))
                        continue;

                    if (IsElementAlreadyTagged(existingTaggedIdsByKey, key, target.Element.Id))
                        continue;

                    if (TryCreateTag(view, target.Element, TagMode.TM_ADDBY_CATEGORY, ShouldUseLeader(target.Element)))
                    {
                        IncrementTagCount(existingTagCounts, key);
                        AddTaggedElement(existingTaggedIdsByKey, key, target.Element.Id);
                    }
                }

                RemoveDuplicateTagsOnView(view);

                tx.Commit();
            }
        }

        private IList<Element> GetTaggableElementsOnView(View view)
        {
            return new FilteredElementCollector(_doc, view.Id)
                .WhereElementIsNotElementType()
                .Where(IsTaggableModelElement)
                .Where(e => !string.IsNullOrWhiteSpace(GetElementCode(e)))
                .ToList();
        }

        private static bool IsTaggableModelElement(Element element)
        {
            return element?.Category != null &&
                   element.Category.CategoryType == CategoryType.Model;
        }

        private static bool ShouldUseLeader(Element element)
        {
            if (element?.Category == null)
                return true;

            var bic = (BuiltInCategory)element.Category.Id.Value;
            return bic != BuiltInCategory.OST_Floors;
        }

        private static string GetElementCode(Element element)
        {
            if (element == null)
                return string.Empty;

            var parameter = element.LookupParameter(DhElementCodeParameterName)
                            ?? element.LookupParameter(ElementCodeParameterName);

            return parameter?.AsString()?.Trim()
                   ?? parameter?.AsValueString()?.Trim()
                   ?? string.Empty;
        }

        private static IEnumerable<TagTarget> SelectMultiCategoryTagTargets(IList<TagTarget> targets)
        {
            return targets
                .GroupBy(x => x.ElementCode, StringComparer.OrdinalIgnoreCase)
                .Select(g => SelectSpatialRepresentatives(g).First());
        }

        private static IEnumerable<TagTarget> SelectCategoryTagTargets(IList<TagTarget> targets)
        {
            return targets
                .GroupBy(x => BuildCategoryTagKey(x.Element, x.ElementCode), StringComparer.OrdinalIgnoreCase)
                .SelectMany(SelectSpatialRepresentatives);
        }

        private static IEnumerable<TagTarget> SelectSpatialRepresentatives(IEnumerable<TagTarget> group)
        {
            var targets = group
                .Where(x => x.Point != null)
                .GroupBy(x => x.Element.Id.Value)
                .Select(g => g.First())
                .ToList();

            if (targets.Count <= 1)
                return targets;

            var maxCount = Math.Min(MaxTagsPerRepeatedCode, targets.Count);
            var center = new XYZ(
                targets.Average(x => x.Point.X),
                targets.Average(x => x.Point.Y),
                targets.Average(x => x.Point.Z));

            var minX = targets.Min(x => x.Point.X);
            var maxX = targets.Max(x => x.Point.X);
            var minY = targets.Min(x => x.Point.Y);
            var maxY = targets.Max(x => x.Point.Y);
            var extent = Math.Sqrt(Math.Pow(maxX - minX, 2) + Math.Pow(maxY - minY, 2));
            var minSpacing = extent * MinTagSpacingRatio;

            var selected = new List<TagTarget>
            {
                targets.OrderBy(x => x.Point.DistanceTo(center)).First()
            };

            while (selected.Count < maxCount)
            {
                var next = targets
                    .Where(x => selected.All(s => s.Element.Id != x.Element.Id))
                    .Select(x => new
                    {
                        Target = x,
                        Distance = selected.Min(s => s.Point.DistanceTo(x.Point))
                    })
                    .OrderByDescending(x => x.Distance)
                    .FirstOrDefault();

                if (next == null || next.Distance < minSpacing)
                    break;

                selected.Add(next.Target);
            }

            return selected;
        }

        private bool TryCreateTag(View view, Element element, TagMode tagMode, bool hasLeader)
        {
            try
            {
                var reference = new Reference(element);
                var point = GetTagPoint(element, view);
                if (point == null)
                    return false;

                var tag = IndependentTag.Create(
                    _doc,
                    view.Id,
                    reference,
                    hasLeader,
                    tagMode,
                    TagOrientation.Horizontal,
                    point);

                if (tag != null)
                {
                    if (tagMode == TagMode.TM_ADDBY_CATEGORY)
                    {
                        var tagTypeId = GetPreferredCategoryTagTypeId(element);
                        if (tagTypeId != ElementId.InvalidElementId)
                            tag.ChangeTypeId(tagTypeId);
                    }

                    tag.HasLeader = hasLeader;
                }

                return tag != null;
            }
            catch
            {
                return false;
            }
        }

        private ElementId GetPreferredCategoryTagTypeId(Element element)
        {
            if (element?.Category == null)
                return ElementId.InvalidElementId;

            var elementCategory = (BuiltInCategory)element.Category.Id.Value;
            var tagCategory = GetTagCategory(elementCategory);
            if (tagCategory == null)
                return ElementId.InvalidElementId;

            var preferredNames = GetPreferredTagTypeNames(elementCategory);
            var tagTypes = new FilteredElementCollector(_doc)
                .WhereElementIsElementType()
                .OfCategory(tagCategory.Value)
                .OfType<ElementType>()
                .ToList();

            foreach (var preferredName in preferredNames)
            {
                var exact = tagTypes.FirstOrDefault(x =>
                    string.Equals(GetTagTypeDisplayName(x), preferredName, StringComparison.OrdinalIgnoreCase));

                if (exact != null)
                    return exact.Id;
            }

            foreach (var preferredName in preferredNames)
            {
                var contains = tagTypes.FirstOrDefault(x =>
                    GetTagTypeDisplayName(x).IndexOf(preferredName, StringComparison.OrdinalIgnoreCase) >= 0);

                if (contains != null)
                    return contains.Id;
            }

            var dhType = tagTypes.FirstOrDefault(x =>
                GetTagTypeDisplayName(x).StartsWith("DH_", StringComparison.OrdinalIgnoreCase));

            return dhType?.Id ?? ElementId.InvalidElementId;
        }

        private static BuiltInCategory? GetTagCategory(BuiltInCategory elementCategory)
        {
            return elementCategory switch
            {
                BuiltInCategory.OST_Walls => BuiltInCategory.OST_WallTags,
                BuiltInCategory.OST_Floors => BuiltInCategory.OST_FloorTags,
                BuiltInCategory.OST_StructuralFoundation => BuiltInCategory.OST_StructuralFoundationTags,
                BuiltInCategory.OST_StructuralColumns => BuiltInCategory.OST_StructuralColumnTags,
                BuiltInCategory.OST_StructuralFraming => BuiltInCategory.OST_StructuralFramingTags,
                BuiltInCategory.OST_Doors => BuiltInCategory.OST_DoorTags,
                BuiltInCategory.OST_Windows => BuiltInCategory.OST_WindowTags,
                BuiltInCategory.OST_PipeCurves => BuiltInCategory.OST_PipeTags,
                BuiltInCategory.OST_GenericModel => BuiltInCategory.OST_GenericModelTags,
                _ => null
            };
        }

        private static IList<string> GetPreferredTagTypeNames(BuiltInCategory elementCategory)
        {
            return elementCategory switch
            {
                BuiltInCategory.OST_Walls => new[] { "DH_벽 태그" },
                BuiltInCategory.OST_Floors => new[] { "DH_바닥 태그" },
                BuiltInCategory.OST_StructuralFoundation => new[] { "DH_구조 기초 태그" },
                BuiltInCategory.OST_StructuralColumns => new[] { "DH_구조 기둥 태그" },
                BuiltInCategory.OST_StructuralFraming => new[] { "DH_구조 프레임 태그" },
                BuiltInCategory.OST_Doors => new[] { "DH_문 태그" },
                BuiltInCategory.OST_Windows => new[] { "DH_창 태그" },
                BuiltInCategory.OST_PipeCurves => new[] { "DH_배관 태그" },
                _ => Array.Empty<string>()
            };
        }

        private static string GetTagTypeDisplayName(ElementType tagType)
        {
            if (tagType is FamilySymbol symbol)
                return $"{symbol.FamilyName} {symbol.Name}";

            return tagType.Name ?? string.Empty;
        }

        private Dictionary<string, int> BuildExistingTagCounts(View view)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var tag in GetTagsOnView(view))
            {
                var key = TryBuildTagKey(tag);
                if (!string.IsNullOrWhiteSpace(key))
                    IncrementTagCount(counts, key);
            }

            return counts;
        }

        private Dictionary<string, HashSet<long>> BuildExistingTaggedIdsByKey(View view)
        {
            var taggedIdsByKey = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase);

            foreach (var tag in GetTagsOnView(view))
            {
                var key = TryBuildTagKey(tag);
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                foreach (var taggedId in GetTaggedElementIds(tag))
                    AddTaggedElement(taggedIdsByKey, key, taggedId);
            }

            return taggedIdsByKey;
        }

        private static int GetTagCount(Dictionary<string, int> counts, string key)
        {
            return counts.TryGetValue(key, out var count) ? count : 0;
        }

        private static int GetMaxTagCountForKey(string key)
        {
            return IsMultiCategoryTagKey(key) ? 1 : MaxTagsPerRepeatedCode;
        }

        private static bool IsMultiCategoryTagKey(string key)
        {
            return key.StartsWith("MULTI|", StringComparison.OrdinalIgnoreCase);
        }

        private static void IncrementTagCount(Dictionary<string, int> counts, string key)
        {
            counts[key] = GetTagCount(counts, key) + 1;
        }

        private static bool IsElementAlreadyTagged(
            Dictionary<string, HashSet<long>> taggedIdsByKey,
            string key,
            ElementId elementId)
        {
            return taggedIdsByKey.TryGetValue(key, out var taggedIds) &&
                   taggedIds.Contains(elementId.Value);
        }

        private static void AddTaggedElement(
            Dictionary<string, HashSet<long>> taggedIdsByKey,
            string key,
            ElementId elementId)
        {
            if (!taggedIdsByKey.TryGetValue(key, out var taggedIds))
            {
                taggedIds = new HashSet<long>();
                taggedIdsByKey[key] = taggedIds;
            }

            taggedIds.Add(elementId.Value);
        }

        private void RemoveDuplicateTagsOnView(View view)
        {
            var keptCountByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var taggedIdsByKey = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase);
            var duplicateIds = new List<ElementId>();

            foreach (var tag in GetTagsOnView(view))
            {
                var key = TryBuildTagKey(tag);
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                var taggedElementIds = GetTaggedElementIds(tag).ToList();
                var hasAlreadyTaggedElement = taggedElementIds.Any(x => IsElementAlreadyTagged(taggedIdsByKey, key, x));

                if (hasAlreadyTaggedElement || GetTagCount(keptCountByKey, key) >= GetMaxTagCountForKey(key))
                {
                    duplicateIds.Add(tag.Id);
                    continue;
                }

                IncrementTagCount(keptCountByKey, key);
                foreach (var taggedElementId in taggedElementIds)
                    AddTaggedElement(taggedIdsByKey, key, taggedElementId);
            }

            if (duplicateIds.Count > 0)
                _doc.Delete(duplicateIds);
        }

        private IList<IndependentTag> GetTagsOnView(View view)
        {
            return new FilteredElementCollector(_doc, view.Id)
                .OfClass(typeof(IndependentTag))
                .Cast<IndependentTag>()
                .ToList();
        }

        private static IEnumerable<ElementId> GetTaggedElementIds(IndependentTag tag)
        {
            try
            {
                return tag.GetTaggedLocalElementIds()
                    .Where(x => x != ElementId.InvalidElementId)
                    .ToList();
            }
            catch
            {
                return Enumerable.Empty<ElementId>();
            }
        }

        private string TryBuildTagKey(IndependentTag tag)
        {
            try
            {
                var taggedElementId = tag.GetTaggedLocalElementIds().FirstOrDefault();
                if (taggedElementId == ElementId.InvalidElementId)
                    return string.Empty;

                var taggedElement = _doc.GetElement(taggedElementId);
                if (!IsTaggableModelElement(taggedElement))
                    return string.Empty;

                var elementCode = GetElementCode(taggedElement);
                if (string.IsNullOrWhiteSpace(elementCode))
                    return string.Empty;

                return IsMultiCategoryTag(tag)
                    ? BuildMultiCategoryTagKey(elementCode)
                    : BuildCategoryTagKey(taggedElement, elementCode);
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool IsMultiCategoryTag(IndependentTag tag)
        {
            var tagType = _doc.GetElement(tag.GetTypeId()) as ElementType;
            var tagCategoryId = tagType?.Category?.Id;

            return tagCategoryId != null &&
                   tagCategoryId.Value == (long)BuiltInCategory.OST_MultiCategoryTags;
        }

        private static string BuildMultiCategoryTagKey(string elementCode)
        {
            return $"MULTI|{elementCode}";
        }

        private static string BuildCategoryTagKey(Element element, string elementCode)
        {
            return $"CATEGORY|{element.Category.Id.Value}|{elementCode}";
        }

        public void ApplyReservoirTags(string sheetId)
        {
            if (!long.TryParse(sheetId, out var sid))
                return;

            var sheet = _doc.GetElement(new ElementId(sid)) as ViewSheet;
            if (sheet == null)
                return;

            foreach (var vpId in sheet.GetAllViewports())
            {
                var vp = _doc.GetElement(vpId) as Viewport;
                if (vp == null)
                    continue;

                var view = _doc.GetElement(vp.ViewId) as View;
                if (view == null)
                    continue;

                if (view is not ViewPlan && view is not ViewSection)
                    continue;

                if (view.Name.Contains("KeyMap", StringComparison.OrdinalIgnoreCase) ||
                    view.Name.Contains("KEY PLAN", StringComparison.OrdinalIgnoreCase))
                    continue;

                ApplyTagsToAllOnView(view);
            }
        }

        private class TagTarget
        {
            public TagTarget(Element element, string elementCode)
                : this(element, elementCode, null)
            {
            }

            public TagTarget(Element element, string elementCode, XYZ point)
            {
                Element = element;
                ElementCode = elementCode;
                Point = point;
            }

            public Element Element { get; }
            public string ElementCode { get; }
            public XYZ Point { get; }
        }
    }
}
