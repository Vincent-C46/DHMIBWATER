using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace DHBIMWATER.UI.ViewModels.Modeling
{
    public class PumpingStationViewModel : ViewModelBase
    {
        #region Fields
        private IDialogService _dialogService;
        private readonly CreatePumpingStationUseCase _createPumpingStationUseCase;

        // 설계조건
        private string _selectedPumpingStationType = "Type1";
        private string _selectedEntranceType = "좌안부";
        private double _d = 800.0;
        private double _hd = 5.0;
        private int _n = 3;
        private double _lwl = 0.0;
        private double _hwl = 2.5;

        // 종단제원
        private double _b1 = 1200.0;
        private double _b3 = 5000.0;
        private double _b4 = 3000.0;
        private double _b6 = 500.0;
        private double _b7 = 3000.0;
        private double _h1 = 500.0;
        private double _h6 = 500.0;
        private string _selectedTheta = "30˚";

        // 종단제원 - 계산값 or 고정값
        private double _l1 = 300.0;
        private double _l2;
        private double _l3;
        private double _l4;
        private double _h3;
        private double _h4;
        private double _h7;
        private double _ob1 = 2000.0;
        private double _oh1 = 3000.0;
        private int _ns;
        private double _hs = 200;

        // 평면제원
        private double _b2;
        private double _b8;
        private string _selectedOpeningType = "사각형";
        private double _b5 = 1600.0;
        private double _b9 = 4000.0;
        private double _l5;
        private double _b10 = 0.0;

        // 부재 유형
        private double _t1 = 400.0;
        private double _t2;
        private double _t3 = 400.0;
        private double _t4;
        private double _t5;
        private double _t6 = 400.0;
        private double _gb1;
        private double _gh1;
        #endregion

        #region Properties
        // 설계조건
        public ObservableCollection<string> PumpingStaitonTypes { get; } = new() { "Type1", "Type2", "Type3", };
        public string SelectedPumpingStationType
        {
            get => _selectedPumpingStationType;
            set
            {
                if (_selectedPumpingStationType != value)
                {
                    _selectedPumpingStationType = value;
                    OnPropertyChanged(nameof(SelectedPumpingStationType));
                }
            }
        }
        public ObservableCollection<string> EntranceTypes { get; } = new() { "좌안부", "우안부", "측면부", };
        public string SelectedEntranceType
        {
            get => _selectedEntranceType;
            set
            {
                if (_selectedEntranceType != value)
                {
                    _selectedEntranceType = value;
                    OnPropertyChanged(nameof(SelectedEntranceType));
                }
            }
        }

        public double D
        {
            get { return _d; }
            set
            {
                if (_d != value)
                {
                    _d = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(D));
                }
            }
        }
        public double HD
        {
            get { return _hd; }
            set
            {
                if (_hd != value)
                {
                    _hd = value;
                    OnPropertyChanged(nameof(HD));
                }
            }
        }
        public int N
        {
            get { return _n; }
            set
            {
                if (_n != value)
                {
                    _n = value;
                    OnPropertyChanged(nameof(N));
                }
            }
        }
        public double LWL
        {
            get { return _lwl; }
            set
            {
                if (_lwl != value)
                {
                    _lwl = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(LWL));
                }
            }
        }
        public double HWL
        {
            get { return _hwl; }
            set
            {
                if (_hwl != value)
                {
                    _hwl = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(HWL));
                }
            }
        }

        // 종단제원
        public double B1
        {
            get { return _b1; }
            set
            {
                if (_b1 != value)
                {
                    _b1 = value;
                    OnPropertyChanged(nameof(B1));
                }
                if (value < 0)
                {
                    _b1 = 400;
                    OnPropertyChanged(nameof(B1));
                }
            }
        }
        public double B3
        {
            get { return _b3; }
            set
            {
                if (_b3 != value)
                {
                    _b3 = value;
                    OnPropertyChanged(nameof(B3));
                }
            }
        }
        public double B4
        {
            get { return _b4; }
            set
            {
                if (_b4 != value)
                {
                    _b4 = value;
                    OnPropertyChanged(nameof(B4));
                }
            }
        }
        public double B6
        {
            get { return _b6; }
            set
            {
                if (_b6 != value)
                {
                    _b6 = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(B6));
                }
            }
        }
        public double B7
        {
            get { return _b7; }
            set
            {
                if (_b7 != value)
                {
                    _b7 = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(B7));
                }
            }
        }
        public double H1
        {
            get { return _h1; }
            set
            {
                if (_h1 != value)
                {
                    _h1 = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(H1));
                }
            }
        }
        public double H6
        {
            get { return _h6; }
            set
            {
                if (_h6 != value)
                {
                    _h6 = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(H6));
                }
            }
        }
        public ObservableCollection<string> Thetas { get; } = new() { "30˚", "45˚", };
        public string SelectedTheta
        {
            get => _selectedTheta;
            set
            {
                if (_selectedTheta != value)
                {
                    _selectedTheta = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(SelectedTheta));
                }
            }
        }
        public double L1
        {
            get { return _l1; }
            set
            {
                if (_l1 != value)
                {
                    _l1 = value;
                    OnPropertyChanged(nameof(L1));
                }
            }
        }
        public double L2
        {
            get { return _l2; }
            set
            {
                if (_l2 != value)
                {
                    _l2 = value;
                    OnPropertyChanged(nameof(L2));
                }
            }
        }
        public double L3
        {
            get { return _l3; }
            set
            {
                if (_l3 != value)
                {
                    _l3 = value;
                    OnPropertyChanged(nameof(L3));
                }
            }
        }
        public double L4
        {
            get { return _l4; }
            set
            {
                if (_l4 != value)
                {
                    _l4 = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(L4));
                }
            }
        }
        public double H3
        {
            get { return _h3; }
            set
            {
                if (_h3 != value)
                {
                    _h3 = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(H3));
                }
            }
        }
        public double H4
        {
            get { return _h4; }
            set
            {
                if (_h4 != value)
                {
                    _h4 = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(H4));
                }
            }
        }
        public double H7
        {
            get { return _h7; }
            set
            {
                if (_h7 != value)
                {
                    _h7 = value;
                    OnPropertyChanged(nameof(H7));
                }
            }
        }
        public double OB1
        {
            get { return _ob1; }
            set
            {
                if (_ob1 != value)
                {
                    _ob1 = value;
                    OnPropertyChanged(nameof(OB1));
                }
            }
        }
        public double OH1
        {
            get { return _oh1; }
            set
            {
                if (_oh1 != value)
                {
                    _oh1 = value;
                    OnPropertyChanged(nameof(OH1));
                }
            }
        }
        public int NS
        {
            get { return _ns; }
            set
            {
                if (_ns != value)
                {
                    _ns = value;
                    OnPropertyChanged(nameof(NS));
                }
            }
        }
        public double HS
        {
            get { return _hs; }
            set
            {
                if (_hs != value)
                {
                    _hs = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(HS));
                }
            }
        }

        // 읽기 전용
        public double H2 => (HWL - LWL) * 1000;
        public double H5 => H2 + H3 + H4;

        //평면제원
        public double B2
        {
            get { return _b2; }
            set
            {
                if (_b2 != value)
                {
                    _b2 = value;
                    OnPropertyChanged(nameof(B2));
                }
            }
        }
        public double B8
        {
            get { return _b8; }
            set
            {
                if (_b8 != value)
                {
                    _b8 = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(B8));
                }
            }
        }
        public string SelectedOpeningType
        {
            get => _selectedOpeningType;
            set
            {
                if (_selectedOpeningType != value)
                {
                    _selectedOpeningType = value;
                    OnPropertyChanged(nameof(SelectedOpeningType));
                }
            }
        }
        public double B5
        {
            get { return _b5; }
            set
            {
                if (_b5 != value)
                {
                    _b5 = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(B5));
                }
            }
        }
        public double B9
        {
            get { return _b9; }
            set
            {
                if (_b9 != value)
                {
                    _b9 = value;
                    OnPropertyChanged(nameof(B9));
                }
            }
        }
        public double L5
        {
            get { return _l5; }
            set
            {
                if (_l5 != value)
                {
                    _l5 = value;
                    OnPropertyChanged(nameof(L5));
                }
            }
        }
        public double B10
        {
            get { return _b10; }
            set
            {
                if (_b10 != value)
                {
                    _b10 = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(B10));
                }
            }
        }

        // 부재 유형
        public double T1
        {
            get { return _t1; }
            set
            {
                if (value < 0) value = 400; // 음수 입력 방지
                if (_t1 != value)
                {
                    _t1 = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(T1));
                }
            }
        }
        public double T2
        {
            get { return _t2; }
            set
            {
                if (_t2 != value)
                {
                    _t2 = value;
                    OnPropertyChanged(nameof(T2));
                }
            }
        }
        public double T3
        {
            get { return _t3; }
            set
            {
                if (_t3 != value)
                {
                    _t3 = value;
                    RecalculateDerivedValues();
                    OnPropertyChanged(nameof(T3));
                }
            }
        }
        public double T4
        {
            get { return _t4; }
            set
            {
                if (_t4 != value)
                {
                    _t4 = value;
                    OnPropertyChanged(nameof(T4));
                }
            }
        }
        public double T5
        {
            get { return _t5; }
            set
            {
                if (_t5 != value)
                {
                    _t5 = value;
                    OnPropertyChanged(nameof(T5));
                }
            }
        }
        public double T6
        {
            get { return _t6; }
            set
            {
                if (_t6 != value)
                {
                    _t6 = value;
                    OnPropertyChanged(nameof(T6));
                }
            }
        }

        public double GB1
        {
            get { return _gb1; }
            set
            {
                if (_gb1 != value)
                {
                    _gb1 = value;
                    OnPropertyChanged(nameof(GB1));
                }
            }
        }
        public double GH1
        {
            get { return _gh1; }
            set
            {
                if (_gh1 != value)
                {
                    _gh1 = value;
                    OnPropertyChanged(nameof(GH1));
                }
            }
        }

        // DTO
        public PumpDesignConditionDto designConditionDto { get; set; }
        public PumpPlanSpecDto planSpecDto { get; set; }
        public PumpProfileSpecDto profileSpecDto { get; set; }
        public PumpTypeSelectionDto typeSelectionDto { get; set; }
        public PumpCreationRequestDto creationRequestDto { get; set; }

        #endregion

        #region Commands
        public ICommand CreatePumpingStationCommand { get; }
        #endregion

        #region Constructor
        public PumpingStationViewModel(CreatePumpingStationUseCase useCase, IDialogService dialogService, IElementTypeQueryRepo elementTypeQueryRepo)
        {
            _createPumpingStationUseCase = useCase;
            _dialogService = dialogService;

            CreatePumpingStationCommand = new RelayCommand(CreatePumpingStation);
            RecalculateDerivedValues();
        }
        #endregion

        #region Methods
        private void CreatePumpingStation(object? obj)
        {
            designConditionDto = new PumpDesignConditionDto(SelectedPumpingStationType, SelectedEntranceType, D, HD, H2, N, LWL, HWL);
            profileSpecDto = new PumpProfileSpecDto(B1, B3, B4, B6, B7, H1, H5, H6, SelectedTheta, L1, L2, L3, L4, H3, H4, H7, OB1, OH1, NS, HS);
            planSpecDto = new PumpPlanSpecDto(B2, B8, SelectedOpeningType, B5, B9, L5, B10);
            typeSelectionDto = new PumpTypeSelectionDto(T1, T2, T3, T4, T5, T6, GB1, GH1);
            creationRequestDto = new PumpCreationRequestDto(designConditionDto, planSpecDto, profileSpecDto, typeSelectionDto);

            _createPumpingStationUseCase.Execute(creationRequestDto);
        }

        // 프로퍼티 업데이트
        private void RecalculateDerivedValues()
        {
            // 종단제원
            _l2 = _h1;
            _h4 = 2.9 * _d;

            if (SelectedTheta == "30˚")
            {
                _l3 = Math.Ceiling((_h4 - _h1) / Math.Tan(30 * Math.PI / 180) / 100) * 100;
                _l4 = Math.Ceiling(3 * _d / 100) * 100;
            }
            else if (SelectedTheta == "45˚")
            {
                _l3 = Math.Ceiling((_h4 - _h1) / Math.Tan(45 * Math.PI / 180) / 100) * 100;
                _l4 = Math.Ceiling(4.5 * _d / 100) * 100;
            }
            _h3 = 1000 - _t1 + 100 - (H2 + _h4) % 100;
            _h7 = 1000 + 100 - _h6 % 100;
            _ns = (int)Math.Floor((_h4 - _h1) / _hs);

            // 평면제원
            if (_h1 + H2 + _h3 + _t1 <= 5000)
                _b2 = 3000.0;
            else if ((5000 < _h1 + H2 + _h3 + _t1) && (_h1 + H2 + _h3 + _t1 <= 7000))
                _b2 = 3500.0;
            else
                _b2 = 4000.0;
            _b8 = Math.Ceiling(3 * _d / 100) * 100;
            _l5 = _b7 + _t3 + _b6 + _b5 / 2 + _l4 - _b10;

            // 부재유형
            _t4 = Math.Ceiling((H5 + _t1) * 0.1 / 100) * 100;
            _t2 = _t4 + 100;

            if (_b8 < 3000)
                _t5 = 400.0;
            else if (3000 <= _b8 && _b8 <= 4000)
                _t5 = 500.0;
            else
                _t5 = 600.0;

            _gb1 = 500;
            _gh1 = _t1 + 300;

            OnPropertyChanged(nameof(B2));
            OnPropertyChanged(nameof(L2));
            OnPropertyChanged(nameof(L3));
            OnPropertyChanged(nameof(L4));
            OnPropertyChanged(nameof(H3));
            OnPropertyChanged(nameof(H4));
            OnPropertyChanged(nameof(H7));
            OnPropertyChanged(nameof(T2));
            OnPropertyChanged(nameof(T4));
            OnPropertyChanged(nameof(NS));
            OnPropertyChanged(nameof(B8));
            OnPropertyChanged(nameof(T5));
            OnPropertyChanged(nameof(L5));
            OnPropertyChanged(nameof(GH1));
        }
        #endregion
    }
}
