using System.Collections.Generic;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.Application.DTOs.Revit.Sheets;

namespace DHBIMWATER.Application.UseCases.Sheets
{
    public interface IWaterReservoirUseCase
    {
        WaterReservoirCreateResult CreateReservoirSheets(string startSheetNumber, int totalSheetCount);
        void PlaceReservoirViews();
        void DeleteReservoirSheetsAndViews();
        void DeleteReservoirSheets();
        void DeleteReservoirViews();
        IList<DimensionTypeDto> GetDimensionTypes();
        void ApplyReservoirDimensions(string dimensionTypeName);
        void OpenReservoirSheets();
        void OpenFirstReservoirSheet();
        void CloseReservoirSheets();
        void ApplyReservoirTags();
        void SetReservoirSheetRange(string startSheetNumber, int totalSheetCount);
        (string hwl, string lwl) GetWaterLevels();
        void CreateOrUpdateWaterLevels(string hwl, string lwl);
    }

    public class WaterReservoirCreateResult
    {
        public int CreatedCount { get; set; }
        public List<string> DuplicateSheetNumbers { get; } = new();
        public List<string> DuplicateSheetNames { get; } = new();
        public bool HasDuplicates =>
            DuplicateSheetNumbers.Count > 0 || DuplicateSheetNames.Count > 0;
    }
}

