using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.UseCases.QuantityCalculator
{
    public class ExportQuantityUseCase
    {
        #region Fields
        IExcelExporter _excelExporter;

        #endregion

        #region Properties

        #endregion

        #region Constructor
        public void Execute(
            string filePath,
            IEnumerable<QuantitySummaryItem> summaryItems,
            IEnumerable<QuantityItem> quantityItems)
        {
            
        }
        #endregion

        #region Methods

        #endregion
    }
}
