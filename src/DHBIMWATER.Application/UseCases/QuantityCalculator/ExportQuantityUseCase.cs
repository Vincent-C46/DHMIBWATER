using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Application.UseCases.QuantityCalculator
{
    public class ExportQuantityUseCase
    {
        #region Fields
        private readonly IExcelExporter _excelExporter;
        private readonly IDialogService _dialogService;
        #endregion

        #region Properties
        #endregion

        #region Constructor
        public ExportQuantityUseCase(
            IExcelExporter excelExporter,
            IDialogService dialogService)
        {
            _excelExporter = excelExporter;
            _dialogService = dialogService;
        }
        #endregion

        #region Methods
        public void Execute(
            string filePath,
            IEnumerable<QuantitySummaryItem> summaryItems,
            IEnumerable<QuantityItem> quantityItems)
        {
            try
            {
                WriteSummarySheet(summaryItems);
                WriteDetailSheet(quantityItems);
                _excelExporter.Save(filePath);
                _dialogService.Info("Success", "수량을 성공적으로 내보냈습니다");
            }
            catch(Exception ex)
            {
                _dialogService.Warn("Error", $"수량을 내보내는 중 오류가 발생했습니다.\n\n{ex.Message}");
            }
        }
        private void WriteSummarySheet(IEnumerable<QuantitySummaryItem> summaryItems)
        {
            _excelExporter.CreateSheet("수량집계표");
            _excelExporter.WriteHeader(["공종", "규격", "규격2", "단위", "수량"]);

            foreach (var item in summaryItems)
            {
                var row = new[] { item.WorkType, item.Specification, item.SubSpecification, item.Unit, item.Value.ToString("F3") };
                if (item.IsTotal)
                {
                    _excelExporter.WriteTotalRow(row);
                    //_excelExporter.WriteEmptyRow();
                }
                else
                {
                    _excelExporter.WriteRow(row);
                }
            }
        }
        private void WriteDetailSheet(IEnumerable<QuantityItem> quantityItems)
        {
            _excelExporter.CreateSheet("상세항목");
            _excelExporter.WriteHeader(["카테고리", "Element Id", "Element Code", "공종", "규격", "규격2", "산출식", "수량", "단위", "상태"]);

            foreach (var item in quantityItems)
            {
                _excelExporter.WriteRow(
                [
                    item.Category,
                    item.ElementId.ToString(),
                    item.ElementCode,
                    item.WorkType,
                    item.Specification,
                    item.SubSpecification,
                    item.RenderedFormula,
                    item.Value.ToString("F3"),
                    item.Unit,
                    item.Status.ToString()
                ]);
            }
        }
        #endregion
    }
}
