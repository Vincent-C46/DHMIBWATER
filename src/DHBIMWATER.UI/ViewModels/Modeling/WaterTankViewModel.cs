using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling
{
    public class WaterTankViewModel : ViewModelBase
    {
        #region Fields
        private IDialogService _dialogService;
        private readonly CreateWallUseCase _createWallUseCase;

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
        public WaterTankViewModel(CreateWallUseCase useCase, IDialogService dialogService)
        {
            _createWallUseCase = useCase;
            _dialogService = dialogService;

            CreateWTankCommand = new RelayCommand(CreateWaterTank);
        }
        #endregion

        #region Methods
        private void CreateWaterTank(object? obj)
        {
            var wallDto = new CreateReservoirWallDto
            {
                StartPt = new Application.DTOs.Common.Point3DDto() { X = 0, Y = 0, Z = 0 },
                EndPt = new Application.DTOs.Common.Point3DDto() { X = WallLength, Y = 0, Z = 0 },
                Length = WallLength
            };

            _createWallUseCase.Execute(wallDto);
        }
        #endregion
    }
}
