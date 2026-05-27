using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Application.Interfaces.Quantity
{
    public interface IExcelExporter
    {
        void ExportQuantity(string filePath, 
                            IEnumerable<QuantitySummaryItem> summaryItems, 
                            IEnumerable<QuantityItem> quantityItems);
    }
}
