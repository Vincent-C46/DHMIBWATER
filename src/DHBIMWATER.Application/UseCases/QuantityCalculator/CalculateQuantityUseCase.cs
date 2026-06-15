using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Application.Interfaces.Storage;
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
        private readonly IElementQuantityRepo _elementQuantityRepo;
        private readonly IEnumerable<IQuantityExtractor> _extractors;
        #endregion

        #region Properties
        #endregion

        #region Constructor
        public CalculateQuantityUseCase(ITransactionContext tx,
                                        IDialogService dialogService,
                                        IElementQuantityRepo elementQuantityRepo,
                                        IEnumerable<IQuantityExtractor> extractors)
        {
            _tx = tx;
            _dialogService = dialogService;
            _elementQuantityRepo = elementQuantityRepo;
            _extractors = extractors;
        }
        #endregion

        #region Methods
        public IEnumerable<QuantityItem> Execute()
        {
            var quantityItems = new List<QuantityItem>();

            foreach (var extractor in _extractors)
            {
                var ids = extractor.CollectElementIds();
                if (!ids.Any()) continue;   // 없으면 다음 Extractor 순환

                foreach (var id in ids)
                    quantityItems.AddRange(extractor.Extract(id));
            }

            using (_tx)
            {
                try
                {
                    _tx.Begin("Save Quantity");
                    foreach (var group in quantityItems.GroupBy(q => q.ElementId))
                        _elementQuantityRepo.Save(group.Key, group.ToList());
                    _tx.Commit();

                    return quantityItems;
                }
                catch (Exception ex)
                {
                    _tx.Rollback();
                    _dialogService.Warn("Error", $"Error Message: {ex.Message}");
                    return quantityItems;
                }
            }
            #endregion
        }
    }
}
