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

        public PumpingStationPlaceViewsResult PlacePumpingStationViews()
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
                    suffix: "_시트", targetViewName: null);

                if (placedId == null)
                {
                    result.NotFoundSheets.Add(sheet.SheetName);
                    continue;
                }

                int scale = PlanViewNames.Contains(sheet.SheetName) ? PlanViewScale : SectionViewScale;
                _sheetUseCase.UpdateViewScale(placedId, scale);
                _sheetUseCase.RecenterViewportToSheetCenter(sheet.Id, placedId);
                _sheetUseCase.ApplyViewFormProfile(placedId, "일반도");
                _sheetUseCase.UpdateViewCategory(placedId, "출력");

                result.PlacedCount++;
            }

            _sheetUseCase.HideSectionMarkersOnPumpingStationSectionViews();
            _sheetUseCase.HideCopiedSectionMarkersOnPumpingStationPlanViews();
            _sheetUseCase.HideNonWaterLevels();

            return result;
        }

        public int DeletePumpingStationSheets()
        {
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

            _sheetUseCase.DeleteReservoirViews();

            return count;
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

        public void ApplyDHTags(IList<string> selectedFamilyIds)
        {
            _sheetUseCase.ApplyDHTags(selectedFamilyIds);
        }

        public IList<TagFamilyDto> GetAvailableTagFamilies() => _sheetUseCase.GetAvailableTagFamilies();

        public IList<TitleBlockDto> GetTitleBlocks() => _sheetUseCase.GetTitleBlocks();
    }
}
