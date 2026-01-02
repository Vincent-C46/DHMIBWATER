using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling
{
    public class WaterTankViewModel : ViewModelBase
    {
        #region Fields
        private IDialogService _dialogService;
        private double _wallLength = 2.5; // 테스트용
        #endregion

        #region Properties
        public ICommand CreateWTankCommand { get; }
        public double WallLength
        {
            get => _wallLength;
            set
            {
                _wallLength = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Constructor
        public WaterTankViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            CreateWTankCommand = new RelayCommand(CreateWaterTank);
        }
        #endregion

        #region Methods
        private void CreateWaterTank(object? obj)
        {
            _dialogService.Info("Water Tank", "Create Water Tank command executed.");
        }
        #endregion
    }
}
