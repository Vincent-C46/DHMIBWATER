using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.UseCases.QuantityCalculator
{
    public class CalculateQuantityUseCase
    {
        #region Fields
        private readonly ITransactionContext _tx;
        private readonly IDialogService _dialogService;

        private readonly IEnumerable<IQuantityExtractor> _extractors;
        #endregion

        #region Properties
        #endregion

        #region Constructor
        public CalculateQuantityUseCase(ITransactionContext tx,
                                        IDialogService dialogService,
                                        IEnumerable<IQuantityExtractor> extractors)
        {
            _tx = tx;
            _dialogService = dialogService;

            _extractors = extractors;
        }
        #endregion

        #region Methods
        public void Execute()
        {
            using (_tx)
            {
                try
                {
                    _tx.Begin("Calculate Quantity");

                    //bool result = _extractors.FirstOrDefault(e => e.CanExtract(000000)).CanExtract;
                    IEnumerable<QuantityItem> qItems = _extractors.FirstOrDefault(e => e.CanExtract(454153)).Extract(454153);
                    _dialogService.Info("Info", $"산출식: {qItems.FirstOrDefault().Formula}\n값: {qItems.FirstOrDefault().Value}");

                    _tx.Commit();
                }
                catch (Exception ex)
                {
                    _tx.Rollback();
                    _dialogService.Warn("Error", $"Error Message: {ex.Message}");
                    return;
                }
            }
            #endregion
        }
    }
}
