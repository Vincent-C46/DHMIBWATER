using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.Application.DTOs.Revit.Sheet;
using DHBIMWATER.Application.DTOs.Revit.Sheets;
using DHBIMWATER.Application.Interfaces.Sheets;


namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class SheetGateway : ISheetGateway
    {
        private readonly SheetQueryService _query;
        private readonly SheetCreateService _create;
        private readonly SheetDeleteService _delete;
        private readonly SheetCopyService _copy;
        private readonly SheetRenameService _rename;
        private readonly TitleBlockQueryService _titleBlocks;
        private readonly ViewQueryService _viewQuery;
        private readonly ViewAddService _viewAdd;
        private readonly ViewReplaceService _viewReplace;
        private readonly ViewRemoveService _viewRemove;
        private readonly ViewScaleService _viewScale;
        private readonly ViewVisualStyleService _viewVisualStyle;
        private readonly DimensionService _dimension;
        private readonly DimensionSelectionService _dimSelection;
        private readonly ViewTitleOnSheetService _viewTitleOnSheet;
        private readonly ViewFormProfileService _viewFormProfile;
        private readonly ViewCenteringService _viewCentering;
        private readonly DeleteReservoirSheetsService _deleteReservoir;
        private readonly WaterReservoirDimensionService _waterReservoirDimension;
        private readonly SheetParameterUpdateService _sheetParameterUpdate;
        private readonly KeyMapSectionFilterService _keyMapSectionFilter;
        private readonly ViewportMoveService _viewportMove;
        private readonly ViewportTypeService _viewportType;
        private readonly DimensionTypeQueryService _dimensionTypeQuery;
        private readonly SheetDimensionClearService _sheetDimensionClear;
        private readonly ViewActivationService _viewActivation;
        private readonly TagService _tag;
        public SheetGateway(Document doc, UIDocument uidoc)
        {
            _query = new SheetQueryService(doc);
            _create = new SheetCreateService(doc);
            _delete = new SheetDeleteService(doc);
            _copy = new SheetCopyService(doc);
            _rename = new SheetRenameService(doc);
            _titleBlocks = new TitleBlockQueryService(doc);
            _viewQuery = new ViewQueryService(doc);
            _viewAdd = new ViewAddService(doc);
            _viewReplace = new ViewReplaceService(doc);
            _viewRemove = new ViewRemoveService(doc);
            _viewScale = new ViewScaleService(doc);
            _viewVisualStyle = new ViewVisualStyleService(doc);
            _dimension = new DimensionService(doc);
            _dimSelection = new DimensionSelectionService(uidoc);
            _viewTitleOnSheet = new ViewTitleOnSheetService(doc);
            _viewFormProfile = new ViewFormProfileService(doc);
            _viewCentering = new ViewCenteringService(doc);
            _deleteReservoir = new DeleteReservoirSheetsService(doc);
            _waterReservoirDimension = new WaterReservoirDimensionService(doc);
            _sheetParameterUpdate = new SheetParameterUpdateService(doc);
            _keyMapSectionFilter = new KeyMapSectionFilterService(doc);
            _viewportMove = new ViewportMoveService(doc);
            _viewportType = new ViewportTypeService(doc);
            _dimensionTypeQuery = new DimensionTypeQueryService(doc);
            _sheetDimensionClear = new SheetDimensionClearService(doc);
            _viewActivation = new ViewActivationService(uidoc);
            _tag = new TagService(doc);

        }

        public IList<SheetInfoDto> GetSheets() => _query.GetSheets();
        public IList<ViewInfoDto> GetViews() => _viewQuery.GetViews();
        public SheetInfoDto CreateSheet(string titleBlockId, string sheetNumber, string sheetName)
            => _create.CreateSheet(titleBlockId, sheetNumber, sheetName);
        public void DeleteSheet(string sheetId) => _delete.DeleteSheet(sheetId);
        public SheetInfoDto CopySheet(string sheetId) => _copy.CopySheet(sheetId);
        public void RenameSheet(string sheetId, string newName) => _rename.RenameSheet(sheetId, newName);
        public IList<TitleBlockDto> GetTitleBlocks() => _titleBlocks.GetTitleBlocks();
        public string AddViewToSheet(string sheetId, string viewId, string suffix = "_시트", string targetViewName = null)
            => _viewAdd.AddViewToSheet(sheetId, viewId, suffix, targetViewName);
        public void ReplaceViewOnSheet(string sheetId, string oldViewId, string newViewId)
            => _viewReplace.ReplaceViewOnSheet(sheetId, oldViewId, newViewId);
        public void RemoveView(string sheetId, string viewId)
        {
            _viewRemove.RemoveView(sheetId, viewId);
        }
        public void UpdateViewScale(string viewId, int scale)
        {
            _viewScale.UpdateViewScale(viewId, scale);
        }
        public void UpdateViewVisualStyle(string viewId, string visualStyle)
        {
            _viewVisualStyle.UpdateViewVisualStyle(viewId, visualStyle);
        }
        public void ApplyAutoDimensions(string sheetId)
        {
            _dimension.ApplyAutoDimensions(sheetId);
        }
        public IList<string> PickDimensionTargetIds() => _dimSelection.PickTargetIds();
        public void ApplyDimensionsToSelected(string sheetId, IList<string> elementIds)
            => _dimension.ApplyDimensionsToSelected(sheetId, elementIds);
        public void ApplyAutoDimensionsOnCurrentView(string dimensionTypeName)
        {
            _dimension.ApplyAutoDimensionsOnCurrentView(dimensionTypeName);
        }

        public void ApplyDimensionsToSelectedOnCurrentView(IList<string> elementIds, string dimensionTypeName)
        {
            _dimension.ApplyDimensionsToSelectedOnCurrentView(elementIds, dimensionTypeName);
        }

        public void UpdateViewTitleOnSheet(string viewId, string titleOnSheet)
        {
            _viewTitleOnSheet.Update(viewId, titleOnSheet);
        }
        public void ApplyViewFormProfile(string viewId, string form)
        {
            _viewFormProfile.Apply(viewId, form);
        }
        public void RecenterViewportToSheetCenter(string sheetId, string viewId)
        {
            _viewCentering.RecenterViewportToSheetCenter(sheetId, viewId);
        }
        public void DeleteReservoirSheetsAndViews(string startSheetNumber, int totalSheetCount)
        {
            _deleteReservoir.DeleteAll(startSheetNumber, totalSheetCount);
        }
        public void DeleteReservoirSheets(string startSheetNumber, int totalSheetCount)
        {
            _deleteReservoir.DeleteSheets(startSheetNumber, totalSheetCount);
        }
        public void DeleteReservoirViews()
        {
            _deleteReservoir.DeleteViews();
        }
        public void UpdateSheetParameters(string sheetId, string drawingTitle, string drawingMember, string drawingScale, string drawingNumber)
        {
            _sheetParameterUpdate.Update(sheetId, drawingTitle, drawingMember, drawingScale, drawingNumber);
        }
        public void FilterKeyMapSections(string viewId, string sectionName)
        {
            _keyMapSectionFilter.Apply(viewId, sectionName);
        }
        public void MoveViewportToPoint(string sheetId, string viewId, double x, double y)
        {
            _viewportMove.Move(sheetId, viewId, x, y);
        }
        public void MoveViewportBySheetRatio(string sheetId, string viewId, double uRatio, double vRatio)
        {
            _viewportMove.MoveBySheetRatio(sheetId, viewId, uRatio, vRatio);
        }
        public void SetViewportType(string sheetId, string viewId, string viewportTypeName)
        {
            _viewportType.SetViewportType(sheetId, viewId, viewportTypeName);
        }
        public IList<DimensionTypeDto> GetDimensionTypes()
        {
            return _dimensionTypeQuery.GetDimensionTypes();
        }
        public void ApplyReservoirDimensions(string sheetId, string dimensionTypeName)
        {
            _waterReservoirDimension.ApplyToSheet(sheetId, dimensionTypeName);
        }
        public void DeleteDimensionsOnSheet(string sheetId)
        {
            _sheetDimensionClear.Clear(sheetId);
        }
        public string GetActiveViewId()
        {
            return _viewActivation.GetActiveViewId();
        }
        public void ActivateView(string viewId)
        {
            _viewActivation.ActivateView(viewId);
        }
        public void UpdateViewportTitleLayout(string sheetId, string viewId, double offsetX, double offsetY, double lineLength)
        {
            _viewportMove.UpdateTitleLayout(sheetId, viewId, offsetX, offsetY, lineLength);
        }
        public void ApplyTagsToSelectedOnCurrentView(IList<string> elementIds)
        {
            _tag.ApplyTagsToSelectedOnCurrentView(elementIds);
        }
        public void HideCopiedSectionMarkersOnReservoirPlanViews()
        {
            _viewAdd.HideCopiedSectionMarkersOnReservoirPlanViews();
        }
        public void ApplyTagsToAllOnCurrentView()
        {
            _tag.ApplyTagsToAllOnCurrentView();
        }
        public void ApplyReservoirTags(string sheetId)
        {
            _tag.ApplyReservoirTags(sheetId);
        }
    }
}
