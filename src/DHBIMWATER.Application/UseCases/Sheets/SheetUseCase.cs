using System.Collections.Generic;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.Application.DTOs.Revit.Sheet;
using DHBIMWATER.Application.DTOs.Revit.Sheets;
using DHBIMWATER.Application.Interfaces.Sheets;

namespace DHBIMWATER.Application.UseCases.Sheets
{
    public class SheetUseCase : ISheetUseCase
    {
        private readonly ISheetGateway _gateway;

        public SheetUseCase(ISheetGateway gateway)
        {
            _gateway = gateway;
        }

        public IList<SheetInfoDto> GetSheets()
        {
            return _gateway.GetSheets();
        }

        public SheetInfoDto CreateSheet(string titleBlockId, string sheetNumber, string sheetName)
        {
            return _gateway.CreateSheet(titleBlockId, sheetNumber, sheetName);
        }

        public void DeleteSheet(string sheetId)
        {
            _gateway.DeleteSheet(sheetId);
        }

        public SheetInfoDto CopySheet(string sheetId)
        {
            return _gateway.CopySheet(sheetId);
        }

        public void RenameSheet(string sheetId, string newName)
        {
            _gateway.RenameSheet(sheetId, newName);
        }
        public IList<TitleBlockDto> GetTitleBlocks()
        {
            return _gateway.GetTitleBlocks();
        }

        public IList<ViewInfoDto> GetViews()
        {
            return _gateway.GetViews();
        }
        public string AddViewToSheet(string sheetId, string viewId, string suffix = "_시트", string targetViewName = null, bool duplicate = true)
        {
            return _gateway.AddViewToSheet(sheetId, viewId, suffix, targetViewName, duplicate);
        }
        public void ReplaceViewOnSheet(string sheetId, string oldViewId, string newViewId)
        {
            _gateway.ReplaceViewOnSheet(sheetId, oldViewId, newViewId);
        }
        public void RemoveView(string sheetId, string viewId)
        {
            _gateway.RemoveView(sheetId, viewId);
        }
        public void UpdateViewScale(string viewId, int scale)
        {
            _gateway.UpdateViewScale(viewId, scale);
        }
        public void UpdateViewVisualStyle(string viewId, string visualStyle)
        {
            _gateway.UpdateViewVisualStyle(viewId, visualStyle);
        }
        public void ApplyAutoDimensions(string sheetId)
        {
            _gateway.ApplyAutoDimensions(sheetId);
        }
        public void ApplyDimensions(string sheetId, DimensionMode mode)
        {
            if (mode == DimensionMode.AllObjects)
            {
                _gateway.ApplyAutoDimensions(sheetId);
                return;
            }

            var pickedIds = _gateway.PickDimensionTargetIds();
            if (pickedIds == null || pickedIds.Count == 0) return;

            _gateway.ApplyDimensionsToSelected(sheetId, pickedIds);
        }
        public void ApplyDimensionsOnCurrentView(DimensionMode mode, string dimensionTypeName, DimensionSide sides = DimensionSide.All, bool includeOverall = true)
        {
            if (mode == DimensionMode.AllObjects)
            {
                _gateway.ApplyAutoDimensionsOnCurrentView(dimensionTypeName);
                return;
            }

            var pickedIds = _gateway.PickDimensionTargetIds();
            if (pickedIds == null || pickedIds.Count == 0)
                return;

            _gateway.ApplyDimensionsToSelectedOnCurrentView(pickedIds, dimensionTypeName, sides, includeOverall);
        }

        public void UpdateViewTitleOnSheet(string viewId, string titleOnSheet)
        {
            _gateway.UpdateViewTitleOnSheet(viewId, titleOnSheet);
        }
        public void UpdateViewCategory(string viewId, string category)
        {
            _gateway.UpdateViewCategory(viewId, category);
        }
        public void ApplyViewFormProfile(string viewId, string form)
        {
            _gateway.ApplyViewFormProfile(viewId, form);
        }
        public void RecenterViewportToSheetCenter(string sheetId, string viewId)
        {
            _gateway.RecenterViewportToSheetCenter(sheetId, viewId);
        }
        public void DeleteReservoirSheetsAndViews(string startSheetNumber, int totalSheetCount)
        {
            _gateway.DeleteReservoirSheetsAndViews(startSheetNumber, totalSheetCount);
        }
        public void DeleteReservoirSheets(string startSheetNumber, int totalSheetCount)
        {
            _gateway.DeleteReservoirSheets(startSheetNumber, totalSheetCount);
        }
        public void DeleteReservoirViews()
        {
            _gateway.DeleteReservoirViews();
        }
        public void UpdateSheetParameters(string sheetId, string drawingTitle, string drawingMember, string drawingScale, string drawingNumber)
        {
            _gateway.UpdateSheetParameters(sheetId, drawingTitle, drawingMember, drawingScale, drawingNumber);
        }
        public void FilterKeyMapSections(string viewId, string sectionName)
        {
            _gateway.FilterKeyMapSections(viewId, sectionName);
        }

        public void MoveViewportToPoint(string sheetId, string viewId, double x, double y)
        {
            _gateway.MoveViewportToPoint(sheetId, viewId, x, y);
        }
        public void MoveViewportBySheetRatio(string sheetId, string viewId, double uRatio, double vRatio)
        {
            _gateway.MoveViewportBySheetRatio(sheetId, viewId, uRatio, vRatio);
        }
        public void ArrangeViewportsByDirection(string sheetId, string directionType)
        {
            _gateway.ArrangeViewportsByDirection(sheetId, directionType);
        }
        public void SetViewportType(string sheetId, string viewId, string viewportTypeName)
        {
            _gateway.SetViewportType(sheetId, viewId, viewportTypeName);
        }
        public IList<DimensionTypeDto> GetDimensionTypes()
        {
            return _gateway.GetDimensionTypes();
        }
        public void ApplyReservoirDimensions(string sheetId, string dimensionTypeName)
        {
            _gateway.ApplyReservoirDimensions(sheetId, dimensionTypeName);
        }
        public void DeleteDimensionsOnSheet(string sheetId)
        {
            _gateway.DeleteDimensionsOnSheet(sheetId);
        }
        public string GetActiveViewId()
        {
            return _gateway.GetActiveViewId();
        }
        public void ActivateView(string viewId)
        {
            _gateway.ActivateView(viewId);
        }
        public void UpdateViewportTitleLayout(string sheetId, string viewId, double offsetX, double offsetY, double lineLength)
        {
            _gateway.UpdateViewportTitleLayout(sheetId, viewId, offsetX, offsetY, lineLength);
        }
        public void UpdateReservoirViewportTitleLayout(string sheetId, string viewId, bool alignRightBottom)
        {
            _gateway.UpdateReservoirViewportTitleLayout(sheetId, viewId, alignRightBottom);
        }
        public void ApplyTagsToSelectedOnCurrentView(IList<string> selectedFamilyIds)
        {
            var pickedIds = _gateway.PickDimensionTargetIds();
            if (pickedIds == null || pickedIds.Count == 0)
                return;

            _gateway.ApplyDHTagsToSelectedOnCurrentView(pickedIds, selectedFamilyIds);
        }
        public void SaveSheetDirection(string sheetId, string directionType)
        {
            _gateway.SaveSheetDirection(sheetId, directionType);
        }

        public void HideSectionMarkersOnReservoirSectionViews()
        {
            _gateway.HideSectionMarkersOnReservoirSectionViews();
        }

        public void HideCopiedSectionMarkersOnReservoirPlanViews()
        {
            _gateway.HideCopiedSectionMarkersOnReservoirPlanViews();
        }

        public void HideSectionMarkersOnPumpingStationSectionViews()
        {
            _gateway.HideSectionMarkersOnPumpingStationSectionViews();
        }

        public void HideCopiedSectionMarkersOnPumpingStationPlanViews()
        {
            _gateway.HideCopiedSectionMarkersOnPumpingStationPlanViews();
        }

        public void ApplyPumpingStationDimensions(string sheetId, string dimensionTypeName)
        {
            _gateway.ApplyPumpingStationDimensions(sheetId, dimensionTypeName);
        }

        public void ApplyTagsToAllOnCurrentView(IList<string> selectedFamilyIds)
        {
            _gateway.ApplyDHTagsToAllOnCurrentView(selectedFamilyIds);
        }
        public void ApplyReservoirTags(string sheetId)
        {
            _gateway.ApplyReservoirTags(sheetId);
        }
        public void CreateOrUpdateWaterLevels(string hwl, string lwl)
        {
            _gateway.CreateOrUpdateWaterLevels(hwl, lwl);
        }

        public (string hwl, string lwl) GetWaterLevels()
        {
            return _gateway.GetWaterLevels();
        }

        public void HideNonWaterLevels()
        {
            _gateway.HideNonWaterLevels();
        }

        public void ApplyPumpingStationAnnotations()
        {
            _gateway.ApplyPumpingStationAnnotations();
        }

        public void ApplyDHTags(IList<string> selectedFamilyIds)
        {
            _gateway.ApplyDHTags(selectedFamilyIds);
        }

        public IList<TagFamilyDto> GetAvailableTagFamilies()
        {
            return _gateway.GetAvailableTagFamilies();
        }
    }
}
