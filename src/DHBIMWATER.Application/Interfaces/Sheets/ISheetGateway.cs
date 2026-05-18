using System.Collections.Generic;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.Application.DTOs.Revit.Sheet;
using DHBIMWATER.Application.DTOs.Revit.Sheets;
using DHBIMWATER.Application.UseCases.Sheets;


namespace DHBIMWATER.Application.Interfaces.Sheets
{
    public interface ISheetGateway
    {
        IList<SheetInfoDto> GetSheets();
        IList<TitleBlockDto> GetTitleBlocks();
        IList<ViewInfoDto> GetViews();
        IList<string> PickDimensionTargetIds();
        IList<DimensionTypeDto> GetDimensionTypes();

        SheetInfoDto CreateSheet(string titleBlockId, string sheetNumber, string sheetName);
        void DeleteSheet(string sheetId);
        SheetInfoDto CopySheet(string sheetId);
        void RenameSheet(string sheetId, string newName);
        string AddViewToSheet(string sheetId, string viewId, string suffix = "_시트", string targetViewName = null);
        void ReplaceViewOnSheet(string sheetId, string oldViewId, string newViewId);
        void RemoveView(string sheetId, string viewId);
        void UpdateViewScale(string viewId, int scale);
        void UpdateViewVisualStyle(string viewId, string visualStyle);
        void ApplyAutoDimensions(string sheetId);
        void ApplyDimensionsToSelected(string sheetId, IList<string> elementIds);
        void ApplyAutoDimensionsOnCurrentView(string dimensionTypeName);
        void ApplyDimensionsToSelectedOnCurrentView(IList<string> elementIds, string dimensionTypeName, DimensionSide sides = DimensionSide.All, bool includeOverall = true);
        void UpdateViewTitleOnSheet(string viewId, string titleOnSheet);
        void UpdateViewCategory(string viewId, string category);
        void ApplyViewFormProfile(string viewId, string form);
        void RecenterViewportToSheetCenter(string sheetId, string viewId);
        void DeleteReservoirSheetsAndViews(string startSheetNumber, int totalSheetCount);
        void DeleteReservoirSheets(string startSheetNumber, int totalSheetCount);
        void DeleteReservoirViews();
        void UpdateSheetParameters(string sheetId, string drawingTitle, string drawingMember, string drawingScale, string drawingNumber);
        void FilterKeyMapSections(string viewId, string sectionName);
        void MoveViewportToPoint(string sheetId, string viewId, double x, double y);
        void MoveViewportBySheetRatio(string sheetId, string viewId, double uRatio, double vRatio);
        void ArrangeViewportsByDirection(string sheetId, string directionType);
        void SetViewportType(string sheetId, string viewId, string viewportTypeName);
        void ApplyReservoirDimensions(string sheetId, string dimensionTypeName);
        void DeleteDimensionsOnSheet(string sheetId);
        string GetActiveViewId();
        void ActivateView(string viewId);
        void UpdateViewportTitleLayout(string sheetId, string viewId, double offsetX, double offsetY, double lineLength);
        void UpdateReservoirViewportTitleLayout(string sheetId, string viewId, bool alignRightBottom);
        void ApplyTagsToSelectedOnCurrentView(IList<string> elementIds);
        void SaveSheetDirection(string sheetId, string directionType);
        void HideSectionMarkersOnReservoirSectionViews();
        void HideCopiedSectionMarkersOnReservoirPlanViews();
        void ApplyTagsToAllOnCurrentView();
        void ApplyReservoirTags(string sheetId);

    }
}
