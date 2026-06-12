using System.Collections.Generic;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.Application.DTOs.Revit.Sheets;

namespace DHBIMWATER.Application.UseCases.Sheets
{
    public interface IPumpingStationUseCase
    {
        PumpingStationCreateResult CreatePumpingStationSheets(string titleBlockId);
        PumpingStationPlaceViewsResult PlacePumpingStationViews();
        int DeletePumpingStationSheets();
        void PlacePumpingStationDimensions(string dimensionTypeName);
        IList<DimensionTypeDto> GetDimensionTypes();
        void CreateOrUpdateWaterLevels(string hwl, string lwl);
        (string hwl, string lwl) GetWaterLevels();
        void ApplyPumpingStationAnnotations();
        void ApplyDHTags(IList<string> selectedFamilyIds);
        IList<TagFamilyDto> GetAvailableTagFamilies();
        IList<TitleBlockDto> GetTitleBlocks();
    }

    public class PumpingStationCreateResult
    {
        public int CreatedCount { get; set; }
        public List<string> DuplicateSheetNumbers { get; } = new();
        public bool HasDuplicates => DuplicateSheetNumbers.Count > 0;
    }

    public class PumpingStationPlaceViewsResult
    {
        public int PlacedCount { get; set; }
        public List<string> NotFoundSheets { get; } = new();
    }
}
