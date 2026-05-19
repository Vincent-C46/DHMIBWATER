using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases.AutoGenerator;
using DHBIMWATER.Application.UseCases.QuantityCalculator;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Quantity
{
    public class QuantityViewModel : ViewModelBase
    {
        #region Fields
        private IDialogService _dialogService;
        private readonly CalculateQuantityUseCase _calculateQuantityUseCase;

        #endregion

        #region Properties
        #endregion

        #region Commands
        public ICommand ExtractCommand { get; }
        #endregion

        #region Constructor
        public QuantityViewModel(CalculateQuantityUseCase useCase, IDialogService dialogService)
        {
            _calculateQuantityUseCase = useCase;
            _dialogService = dialogService;
            ExtractCommand = new RelayCommand(GetCalculateQuantity);
        }
        #endregion

        #region Methods
        private void  GetCalculateQuantity(object? obj)
        {
            _calculateQuantityUseCase.Execute();
            return;
        }
        #endregion
    }
}