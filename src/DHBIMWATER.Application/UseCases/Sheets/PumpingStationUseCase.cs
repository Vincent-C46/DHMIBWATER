using System;
using System.Collections.Generic;
using System.Linq;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.Application.DTOs.Revit.Sheets;

namespace DHBIMWATER.Application.UseCases.Sheets
{
    public class PumpingStationUseCase : IPumpingStationUseCase
    {
        private readonly ISheetUseCase _sheetUseCase;

        private static readonly string[] PlanViewNames = { "상부슬래브", "기초(유입부)" };

        private const int PlanViewScale    = 50;
        private const int SectionViewScale = 50;

        public PumpingStationUseCase(ISheetUseCase sheetUseCase)
        {
            _sheetUseCase = sheetUseCase;
        }

        public PumpingStationCreateResult CreatePumpingStationSheets(string titleBlockId)
        {
            var result = new PumpingStationCreateResult();

            if (string.IsNullOrWhiteSpace(titleBlockId))
                return result;

            var allViews = _sheetUseCase.GetViews();

            var sectionViews = allViews
                .Where(v => v.ViewName != null &&
                            v.ViewName.Length == 1 &&
                            char.IsUpper(v.ViewName[0]))
                .OrderBy(v => v.ViewName)
                .ToList();

            int sheetIndex = 1;

            foreach (var planName in PlanViewNames)
            {
                var sheetNum = sheetIndex.ToString("D3");
                var created = _sheetUseCase.CreateSheet(titleBlockId, sheetNum, planName);
                if (created != null)
                    result.CreatedCount++;
                else
                    result.DuplicateSheetNumbers.Add(sheetNum);
                sheetIndex++;
            }

            foreach (var sectionView in sectionViews)
            {
                var sheetNum = sheetIndex.ToString("D3");
                var sheetName = $"단면 {sectionView.ViewName}";
                var created = _sheetUseCase.CreateSheet(titleBlockId, sheetNum, sheetName);
                if (created != null)
                    result.CreatedCount++;
                else
                    result.DuplicateSheetNumbers.Add(sheetNum);
                sheetIndex++;
            }

            return result;
        }

        public IList<ViewTemplateDto> GetViewTemplates() => _sheetUseCase.GetViewTemplates();

        public PumpingStationPlaceViewsResult PlacePumpingStationViews(string planTemplateId = null, string sectionTemplateId = null, int? planScale = null, int? sectionScale = null)
        {
            var result = new PumpingStationPlaceViewsResult();

            var sheets = _sheetUseCase.GetSheets();
            var views  = _sheetUseCase.GetViews();

            foreach (var sheet in sheets)
            {
                string targetViewName;

                if (PlanViewNames.Contains(sheet.SheetName))
                {
                    targetViewName = sheet.SheetName;
                }
                else if (sheet.SheetName.StartsWith("단면 ") &&
                         sheet.SheetName.Length == "단면 ".Length + 1)
                {
                    targetViewName = sheet.SheetName.Substring("단면 ".Length);
                }
                else
                {
                    continue;
                }

                var match = views.FirstOrDefault(v =>
                    string.Equals(v.ViewName, targetViewName, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    result.NotFoundSheets.Add(sheet.SheetName);
                    continue;
                }

                var placedId = _sheetUseCase.AddViewToSheet(
                    sheet.Id, match.ViewId,
                    suffix: "_시트", targetViewName: null,
                    planTemplateId: planTemplateId, sectionTemplateId: sectionTemplateId);

                if (placedId == null)
                {
                    result.NotFoundSheets.Add(sheet.SheetName);
                    continue;
                }

                int scale = PlanViewNames.Contains(sheet.SheetName)
                    ? (planScale ?? PlanViewScale)
                    : (sectionScale ?? SectionViewScale);
                _sheetUseCase.UpdateViewScale(placedId, scale);
                _sheetUseCase.RecenterViewportToSheetCenter(sheet.Id, placedId);
                _sheetUseCase.ApplyViewFormProfile(placedId, "일반도");
                _sheetUseCase.UpdateViewCategory(placedId, "출력");
                _sheetUseCase.ApplyViewBorderAndTitle(placedId, sheet.SheetName);

                result.PlacedCount++;
            }

            _sheetUseCase.HideSectionMarkersOnPumpingStationSectionViews();
            _sheetUseCase.HideCopiedSectionMarkersOnPumpingStationPlanViews();
            _sheetUseCase.HideNonWaterLevels();

            return result;
        }

        public int DeletePumpingStationSheets()
        {
            ActivateSafeView();
            int count = DeletePumpingStationSheetsOnly();
            _sheetUseCase.DeleteReservoirViews();
            return count;
        }

        public int DeletePumpingStationSheetsOnly()
        {
            ActivateSafeView();
            var sheets = _sheetUseCase.GetSheets();
            int count = 0;

            foreach (var sheet in sheets)
            {
                bool isPlanSheet = PlanViewNames.Contains(sheet.SheetName);
                bool isSectionSheet = sheet.SheetName.StartsWith("단면 ") &&
                                      sheet.SheetName.Length == "단면 ".Length + 1 &&
                                      char.IsUpper(sheet.SheetName[^1]);

                if (!isPlanSheet && !isSectionSheet) continue;

                _sheetUseCase.DeleteSheet(sheet.Id);
                count++;
            }

            return count;
        }

        public void DeletePumpingStationViewsOnly()
        {
            ActivateSafeView();
            _sheetUseCase.DeleteReservoirViews();
        }

        // 삭제 전 활성 시트/뷰가 삭제 대상이면 오류 발생 → 안전한 뷰로 전환
        private void ActivateSafeView()
        {
            try
            {
                var activeViewId = _sheetUseCase.GetActiveViewId();
                var views = _sheetUseCase.GetViews();
                var safeView = views.FirstOrDefault(v =>
                    v.ViewType == "3D" || v.ViewType == "ThreeD" || v.ViewName == "{3D}");

                safeView ??= views.FirstOrDefault(v =>
                    v.ViewType != "DrawingSheet" && v.ViewId != activeViewId);

                if (safeView != null && safeView.ViewId != activeViewId)
                    _sheetUseCase.ActivateView(safeView.ViewId);
            }
            catch { }
        }

        public void PlacePumpingStationDimensions(string dimensionTypeName)
        {
            var sheets = _sheetUseCase.GetSheets();

            foreach (var sheet in sheets)
            {
                bool isPlanSheet    = PlanViewNames.Contains(sheet.SheetName);
                bool isSectionSheet = sheet.SheetName.StartsWith("단면 ") &&
                                      sheet.SheetName.Length == "단면 ".Length + 1 &&
                                      char.IsUpper(sheet.SheetName[^1]);

                if (!isPlanSheet && !isSectionSheet) continue;

                _sheetUseCase.ApplyPumpingStationDimensions(sheet.Id, dimensionTypeName);
            }
        }

        public IList<DimensionTypeDto> GetDimensionTypes() => _sheetUseCase.GetDimensionTypes();

        public void CreateOrUpdateWaterLevels(string hwl, string lwl)
        {
            _sheetUseCase.CreateOrUpdateWaterLevels(hwl, lwl);
        }

        public (string hwl, string lwl) GetWaterLevels()
        {
            return _sheetUseCase.GetWaterLevels();
        }

        public void ApplyPumpingStationAnnotations()
        {
            _sheetUseCase.ApplyPumpingStationAnnotations();
        }

        public void ApplyDHTags(IList<string> selectedFamilyIds, IDictionary<string, (IList<string> Codes, IList<string> Parts)> viewFilters = null)
        {
            _sheetUseCase.ApplyDHTags(selectedFamilyIds, viewFilters);
        }

        public IList<TagFamilyDto> GetAvailableTagFamilies() => _sheetUseCase.GetAvailableTagFamilies();

        public IList<string> GetAvailableDHElementCodes() => _sheetUseCase.GetAvailableDHElementCodes();

        public IList<string> GetAvailableDHParts() => _sheetUseCase.GetAvailableDHParts();

        public IList<TitleBlockDto> GetTitleBlocks() => _sheetUseCase.GetTitleBlocks();
    }
}
