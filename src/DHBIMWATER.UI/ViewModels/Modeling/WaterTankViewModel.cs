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
        private readonly CreateReservoirUseCase _createReservoirUseCase;

        private double _q = 5000;
        private double _rt = 12;
        private int _n = 2; 
        private double _lwl = 0;

        #endregion

        #region Properties
        public ICommand CreateWTankCommand { get; }
        public double Q
        {
            get => _q;
            set
            {
                if (_q != value)
                {
                    _q= value;
                    OnPropertyChanged(nameof(Q));
                }
            }
        }
        public double RT
        {
            get => _rt;
            set
            {
                if (_rt != value)
                {
                    _rt= value;
                    OnPropertyChanged(nameof(RT));
                }
            }
        }
        public int N
        {
            get => _n;
            set
            {
                if (_n != value)
                {
                    _n= value;
                    OnPropertyChanged(nameof(N));
                }
            }
        }
        public double LWL
        {
            get => _lwl;
            set
            {
                if (_lwl != value)
                {
                    _lwl= value;
                    OnPropertyChanged(nameof(LWL));
                }
            }
        }

        // DTO
        public ReservoirDesignConditionDto designConditionDto { get; set; }
        public ReservoirTankDto tankDto { get; set; }
        public ReservoirValveDto valveDto { get; set; }
        public ReservoirSelectedTypeIdDto typeSelectionDto { get; set; }
        public ReservoirCreationRequestDto reservoirCreationRequestDto { get; set; }
        #endregion

        #region Constructor
        public WaterTankViewModel(CreateReservoirUseCase useCase, IDialogService dialogService)
        {
            _createReservoirUseCase = useCase;
            _dialogService = dialogService;

            CreateWTankCommand = new RelayCommand(CreateWaterTank);
        }
        #endregion

        #region Methods
        private void CreateWaterTank(object? obj)
        {
            // DTO 작성
            designConditionDto = new ReservoirDesignConditionDto(Q, RT, N, LWL);
            tankDto = new ReservoirTankDto(0,0,0,0,0,0,0,0,0,0,0,0,0);
            valveDto = new ReservoirValveDto(0,0,0,0);
            typeSelectionDto = new ReservoirSelectedTypeIdDto("a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a");

            reservoirCreationRequestDto = new ReservoirCreationRequestDto(designConditionDto, tankDto, valveDto, typeSelectionDto);

            _createReservoirUseCase.Execute(reservoirCreationRequestDto);

            //var reservoirDto = new ReservoirDto
            //{
            //    StartPt = new Application.DTOs.Common.Point3DDto() { X = 0, Y = 0, Z = 0 },
            //    EndPt = new Application.DTOs.Common.Point3DDto() { X = WallLength, Y = 0, Z = 0 },
            //    Length = WallLength
            //};

            //_createWallUseCase.Execute(reservoirDto);
        }
        #endregion
    }
}
