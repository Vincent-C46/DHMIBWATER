using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewSheetPreparationService
    {
        private readonly Document _doc;

        private const string PlanViewTemplateName = "DH_평면도";
        private const string SectionViewTemplateName = "DH_단면도";

        public ViewSheetPreparationService(Document doc)
        {
            _doc = doc;
        }

        public string CreateSheetView(string sourceViewId, string suffix = "_시트", string targetViewName = null, string planTemplateId = null, string sectionTemplateId = null)
        {
            var sourceId = new ElementId(long.Parse(sourceViewId));
            var sourceView = _doc.GetElement(sourceId) as View;
            if (sourceView == null)
                throw new InvalidOperationException("원본 뷰를 찾을 수 없습니다.");

            ElementId duplicatedId;

            using (var tx = new Transaction(_doc, "Prepare Sheet View"))
            {
                tx.Start();

                duplicatedId = sourceView.Duplicate(ViewDuplicateOption.WithDetailing);
                var duplicatedView = _doc.GetElement(duplicatedId) as View;

                if (duplicatedView == null)
                    throw new InvalidOperationException("복사된 뷰를 찾을 수 없습니다.");

                var desiredName = string.IsNullOrWhiteSpace(targetViewName)
                    ? sourceView.Name + suffix
                    : targetViewName;

                duplicatedView.Name = GetUniqueViewName(SanitizeViewName(desiredName));
                ViewCategoryService.SetViewCategory(duplicatedView, "출력");
                ApplyViewTemplate(duplicatedView, planTemplateId, sectionTemplateId);

                tx.Commit();
            }

            return duplicatedId.Value.ToString();
        }

        public View CreateSheetViewInOpenTransaction(string sourceViewId, string suffix = "_시트", string targetViewName = null, string planTemplateId = null, string sectionTemplateId = null)
        {
            var sourceId = new ElementId(long.Parse(sourceViewId));
            var sourceView = _doc.GetElement(sourceId) as View;
            if (sourceView == null)
                throw new InvalidOperationException("원본 뷰를 찾을 수 없습니다.");

            var duplicatedId = sourceView.Duplicate(ViewDuplicateOption.WithDetailing);
            var duplicatedView = _doc.GetElement(duplicatedId) as View;

            if (duplicatedView == null)
                throw new InvalidOperationException("복사된 뷰를 찾을 수 없습니다.");

            var desiredName = string.IsNullOrWhiteSpace(targetViewName)
                ? sourceView.Name + suffix
                : targetViewName;

            duplicatedView.Name = GetUniqueViewName(SanitizeViewName(desiredName));
            ViewCategoryService.SetViewCategory(duplicatedView, "출력");
            ApplyViewTemplate(duplicatedView, planTemplateId, sectionTemplateId);

            return duplicatedView;
        }

        /// <summary>
        /// 복제된 뷰 종류에 맞는 뷰 템플릿을 적용한다.
        /// planTemplateId/sectionTemplateId가 null이면 DH_평면도/DH_단면도 이름으로 기본 템플릿을 찾고,
        /// 빈 문자열("")이면 "없음"으로 간주하여 템플릿을 적용하지 않는다.
        /// </summary>
        private void ApplyViewTemplate(View view, string planTemplateId, string sectionTemplateId)
        {
            var templateId = view switch
            {
                ViewPlan => ResolveTemplateId(planTemplateId, PlanViewTemplateName),
                ViewSection => ResolveTemplateId(sectionTemplateId, SectionViewTemplateName),
                _ => null
            };
            if (templateId == null) return;

            try
            {
                if (view.IsValidViewTemplate(templateId))
                    view.ViewTemplateId = templateId;
            }
            catch { }
        }

        private ElementId ResolveTemplateId(string templateId, string fallbackName)
        {
            if (templateId == string.Empty) return null;

            if (!string.IsNullOrWhiteSpace(templateId) && long.TryParse(templateId, out var idValue))
                return new ElementId(idValue);

            var template = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .FirstOrDefault(v => v.IsTemplate && v.Name.Equals(fallbackName, StringComparison.OrdinalIgnoreCase));
            return template?.Id;
        }

        private string GetUniqueViewName(string baseName)
        {
            var names = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Select(v => v.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!names.Contains(baseName))
                return baseName;

            int index = 2;
            while (names.Contains($"{baseName}({index})"))
                index++;

            return $"{baseName}({index})";
        }

        private static string SanitizedFallbackName => "Sheet View";

        private static string SanitizeViewName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return SanitizedFallbackName;

            var invalidChars = new[] { '\\', ':', '{', '}', '[', ']', '|', ';', '<', '>', '?', '`', '~' };
            var sanitized = name;

            foreach (var invalidChar in invalidChars)
                sanitized = sanitized.Replace(invalidChar, '_');

            sanitized = sanitized.Trim();
            return string.IsNullOrWhiteSpace(sanitized) ? SanitizedFallbackName : sanitized;
        }

        public void HideSectionMarkersOnReservoirSectionViews()
        {
            var targetViews = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v =>
                    !v.IsTemplate &&
                    v is ViewSection &&
                    v.Name.Contains("_시트", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (targetViews.Count == 0)
                return;

            var targetBics = new HashSet<BuiltInCategory>
            {
                BuiltInCategory.OST_SectionHeads,
                BuiltInCategory.OST_Sections,
                BuiltInCategory.OST_Viewers,
                BuiltInCategory.OST_Levels
            };

            using var tx = new Transaction(_doc, "Hide Section Markers And Levels On Section Views");
            tx.Start();

            foreach (var view in targetViews)
            {
                var hideIds = new FilteredElementCollector(_doc, view.Id)
                    .WhereElementIsNotElementType()
                    .Where(e => e.Category != null &&
                                targetBics.Contains((BuiltInCategory)e.Category.Id.Value) &&
                                e.CanBeHidden(view))
                    .Select(e => e.Id)
                    .ToList();

                if (hideIds.Count > 0)
                    view.HideElements(hideIds);
            }

            tx.Commit();
        }

        public void HideCopiedSectionMarkersOnReservoirPlanViews()
        {
            var targetViewNamePrefixes = new[]
            {
                "수조부 상부슬래브_시트",
                "수조부 바닥슬래브_시트",
                "밸브실 중간슬래브_시트"
            };

            var targetViews = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v =>
                    !v.IsTemplate &&
                    v is ViewPlan &&
                    (targetViewNamePrefixes.Any(prefix =>
                        v.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) ||
                     v.Name.Contains("KeyMap", StringComparison.OrdinalIgnoreCase) ||
                     v.Name.Contains("KEY PLAN", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (targetViews.Count == 0)
                return;

            using var tx = new Transaction(_doc, "Hide Copied Section Markers");
            tx.Start();

            // 입면도 기호는 무조건 숨김, 단면기호는 복사본만 숨김
            var elevationBics = new HashSet<BuiltInCategory>
            {
                BuiltInCategory.OST_Elev,
                BuiltInCategory.OST_ElevationMarks
            };

            foreach (var targetView in targetViews)
            {
                var hideIds = new List<ElementId>();

                var elements = new FilteredElementCollector(_doc, targetView.Id)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (var element in elements)
                {
                    if (element?.Category == null)
                        continue;

                    var bic = (BuiltInCategory)element.Category.Id.Value;

                    if (!element.CanBeHidden(targetView))
                        continue;

                    if (elevationBics.Contains(bic))
                    {
                        hideIds.Add(element.Id);
                        continue;
                    }

                    if (bic != BuiltInCategory.OST_Viewers &&
                        bic != BuiltInCategory.OST_Sections &&
                        bic != BuiltInCategory.OST_SectionHeads)
                        continue;

                    if (IsCopiedSectionMarkerElement(element))
                        hideIds.Add(element.Id);
                }

                if (hideIds.Count > 0)
                    targetView.HideElements(hideIds);
            }

            tx.Commit();
        }
        public void HideSectionMarkersOnPumpingStationSectionViews()
        {
            var targetViews = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v =>
                    !v.IsTemplate &&
                    v is ViewSection &&
                    v.Name.Contains("_시트", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (targetViews.Count == 0)
                return;

            var targetBics = new HashSet<BuiltInCategory>
            {
                BuiltInCategory.OST_SectionHeads,
                BuiltInCategory.OST_Sections,
                BuiltInCategory.OST_Viewers
            };

            using var tx = new Transaction(_doc, "Hide Section Markers On Pumping Station Section Views");
            tx.Start();

            foreach (var view in targetViews)
            {
                var hideIds = new FilteredElementCollector(_doc, view.Id)
                    .WhereElementIsNotElementType()
                    .Where(e => e.Category != null &&
                                targetBics.Contains((BuiltInCategory)e.Category.Id.Value) &&
                                e.CanBeHidden(view))
                    .Select(e => e.Id)
                    .ToList();

                if (hideIds.Count > 0)
                    view.HideElements(hideIds);
            }

            tx.Commit();
        }

        public void HideCopiedSectionMarkersOnPumpingStationPlanViews()
        {
            var targetViewNamePrefixes = new[]
            {
                "상부슬래브_시트",
                "기초(유입부)_시트"
            };

            var targetViews = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v =>
                    !v.IsTemplate &&
                    v is ViewPlan &&
                    targetViewNamePrefixes.Any(prefix =>
                        v.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (targetViews.Count == 0)
                return;

            using var tx = new Transaction(_doc, "Hide Copied Section Markers On Pumping Station Plan Views");
            tx.Start();

            foreach (var targetView in targetViews)
            {
                var hideIds = new List<ElementId>();

                var elements = new FilteredElementCollector(_doc, targetView.Id)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (var element in elements)
                {
                    if (element?.Category == null)
                        continue;

                    var bic = (BuiltInCategory)element.Category.Id.Value;

                    if (bic != BuiltInCategory.OST_Viewers &&
                        bic != BuiltInCategory.OST_Sections &&
                        bic != BuiltInCategory.OST_SectionHeads)
                        continue;

                    if (!element.CanBeHidden(targetView))
                        continue;

                    if (IsCopiedSectionMarkerElement(element))
                        hideIds.Add(element.Id);
                }

                if (hideIds.Count > 0)
                    targetView.HideElements(hideIds);
            }

            tx.Commit();
        }

        private bool IsCopiedSectionMarkerElement(Element element)
        {
            if (ContainsCopiedSectionName(element.Name))
                return true;

            var type = _doc.GetElement(element.GetTypeId());
            if (ContainsCopiedSectionName(type?.Name))
                return true;

            foreach (Autodesk.Revit.DB.Parameter parameter in element.Parameters)
            {
                var valueString = parameter.AsValueString();
                if (ContainsCopiedSectionName(valueString))
                    return true;

                if (parameter.StorageType == StorageType.String)
                {
                    var stringValue = parameter.AsString();
                    if (ContainsCopiedSectionName(stringValue))
                        return true;
                }

                if (parameter.StorageType == StorageType.ElementId)
                {
                    var refId = parameter.AsElementId();
                    if (refId == ElementId.InvalidElementId)
                        continue;

                    var refElement = _doc.GetElement(refId);
                    if (ContainsCopiedSectionName(refElement?.Name))
                        return true;

                    if (refElement is View refView &&
                        refView.Name.Contains("_시트", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }
        private static bool ContainsCopiedSectionName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return value.Contains("_시트", StringComparison.OrdinalIgnoreCase);
        }

    }
}
