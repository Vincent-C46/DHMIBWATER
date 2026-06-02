using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public IEnumerable<QuantityItem> Execute()
        {
            var quantityItems = new List<QuantityItem>();

            //using (_tx)
            {
                try
                {
                    //_tx.Begin("Calculate Quantity");
                    foreach (var extractor in _extractors)
                    {
                        var ids = extractor.CollectElementIds();
                        if (!ids.Any()) continue;   // 없으면 다음 Extractor 순환

                        foreach (var id in ids)
                            quantityItems.AddRange(extractor.Extract(id));
                    }

                    if (quantityItems.Any())
                    {
                        var item = quantityItems.FirstOrDefault(r => r.WorkType.Contains("콘크리트"));
                        //_dialogService.Info("Info",$"공종: {item.WorkType}\n규격: {item.Specification}\n산출식: {item.Formula}\n값: {item.Value}");
                    }
                                        
                    //Debug.WriteLine($"{}");

                    return quantityItems;

                    //_tx.Commit();
                }
                catch (Exception ex)
                {
                    //_tx.Rollback();
                    _dialogService.Warn("Error", $"Error Message: {ex.Message}");
                    return quantityItems;
                }
            }
            #endregion
        }
    }
}
