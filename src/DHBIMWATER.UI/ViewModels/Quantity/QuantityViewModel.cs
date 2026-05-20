using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases.QuantityCalculator;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.UI.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Quantity
{
    public class QuantityViewModel : ViewModelBase
    {
        #region Fields
        private IDialogService _dialogService;
        private readonly CalculateQuantityUseCase _calculateQuantityUseCase;

        private QuantityItem? _selectedItem;
        #endregion

        #region Properties
        public ObservableCollection<QuantityItem> QuantityItems { get; private set; } = new();
        public QuantityItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                    _selectedItem = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands
        public ICommand ExtractCommand { get; }
        #endregion

        #region Constructor
        public QuantityViewModel(CalculateQuantityUseCase useCase, IDialogService dialogService)
        {
            _calculateQuantityUseCase = useCase;
            _dialogService = dialogService;

            var items = _calculateQuantityUseCase.Execute();
            QuantityItems = new ObservableCollection<QuantityItem>(items);

            //ExtractCommand = new RelayCommand(GetCalculateQuantity);
        }
        #endregion

        #region Methods
        private void GetCalculateQuantity(object? obj)
        {
            var items = _calculateQuantityUseCase.Execute();
            QuantityItems = new ObservableCollection<QuantityItem>(items);
            OnPropertyChanged(nameof(QuantityItems));
        }
        #endregion
    }
}