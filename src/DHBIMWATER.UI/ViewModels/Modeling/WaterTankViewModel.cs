using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling
{
    public class WaterTankViewModel : ViewModelBase
    {
        private IDialogService _dialogService;

        public ICommand CreateWTankCommand { get; }

        public WaterTankViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            CreateWTankCommand = new RelayCommand(CreateWaterTank);
        }

        private void CreateWaterTank(object? obj)
        {
            _dialogService.Info("Water Tank", "Create Water Tank command executed.");
        }
    }
}
