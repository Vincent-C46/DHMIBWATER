using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Application.UseCases.QuantityCalculator
{
    public class ExportQuantityUseCase
    {
        #region Fields
        private readonly IExcelExporter _excelExporter;
        #endregion

        #region Properties

        #endregion

        #region Constructor
        public ExportQuantityUseCase(IExcelExporter excelExporter)
        {
            _excelExporter = excelExporter;
        }
        #endregion

        #region Methods
        public void Execute(
            string filePath,
            IEnumerable<QuantitySummaryItem> summaryItems,
            IEnumerable<QuantityItem> quantityItems)
        {
            _excelExporter.CreateSheet("수량집계표");
            _excelExporter.CreateSheet("상세항목");
            _excelExporter.Save(filePath);
        }
        #endregion
    }
}
