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

        private readonly Document _doc;

        public TagService(Document doc)
        {
            _doc = doc;
        }

        public void ApplyTagsToSelectedOnCurrentView(IList<string> elementIds)
        {
            if (elementIds == null || elementIds.Count == 0)
                return;

            var ids = elementIds
                .Select(x => long.TryParse(x, out var v) ? new ElementId(v) : ElementId.InvalidElementId)
                .Where(x => x != ElementId.InvalidElementId)
                .ToList();

            if (ids.Count == 0)
                return;

            var view = _doc.ActiveView;
            if (view == null)
                return;

            ApplyTagsToElementsOnView(view, ids);
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

        public void ApplyTagsToAllOnCurrentView()
        {
            var view = _doc.ActiveView;
            if (view == null)
                return;

            ApplyTagsToAllOnView(view);
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
                .Select(e => new TagTarget(e, GetElementCode(e)))
                .Where(x => !string.IsNullOrWhiteSpace(x.ElementCode))
                .ToList();

            if (elements.Count == 0)
                return;

            using (var tx = new Transaction(_doc, "Apply Tags On Current View"))
            {
                tx.Start();

                RemoveDuplicateTagsOnView(view);
                var existingTagKeys = BuildExistingTagKeys(view);

                foreach (var target in SelectMultiCategoryTagTargets(elements))
                {
                    var key = BuildMultiCategoryTagKey(target.ElementCode);
                    if (existingTagKeys.Contains(key))
                        continue;

                    if (TryCreateTag(view, target.Element, TagMode.TM_ADDBY_MULTICATEGORY, false))
                        existingTagKeys.Add(key);
                }

                foreach (var target in SelectCategoryTagTargets(elements))
                {
                    var key = BuildCategoryTagKey(target.Element, target.ElementCode);
                    if (existingTagKeys.Contains(key))
                        continue;

                    var hasLeader = ShouldUseLeader(target.Element);
                    if (TryCreateTag(view, target.Element, TagMode.TM_ADDBY_CATEGORY, hasLeader))
                        existingTagKeys.Add(key);
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
                .Select(g => g.First());
        }

        private static IEnumerable<TagTarget> SelectCategoryTagTargets(IList<TagTarget> targets)
        {
            return targets
                .GroupBy(x => BuildCategoryTagKey(x.Element, x.ElementCode), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First());
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
                BuiltInCategory.OST_Stairs => new[] { "DH_계단 태그" },
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

        private HashSet<string> BuildExistingTagKeys(View view)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tag in GetTagsOnView(view))
            {
                var key = TryBuildTagKey(tag);
                if (!string.IsNullOrWhiteSpace(key))
                    keys.Add(key);
            }

            return keys;
        }

        private void RemoveDuplicateTagsOnView(View view)
        {
            var firstTagByKey = new Dictionary<string, ElementId>(StringComparer.OrdinalIgnoreCase);
            var duplicateIds = new List<ElementId>();

            foreach (var tag in GetTagsOnView(view))
            {
                var key = TryBuildTagKey(tag);
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (firstTagByKey.ContainsKey(key))
                {
                    duplicateIds.Add(tag.Id);
                    continue;
                }

                firstTagByKey.Add(key, tag.Id);
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
            {
                Element = element;
                ElementCode = elementCode;
            }

            public Element Element { get; }
            public string ElementCode { get; }
        }
    }
}
