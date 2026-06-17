using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases.AutoGenerator;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling
{
    public class WaterTankViewModel : ViewModelBase
    {
        #region Fields
        private IDialogService _dialogService;
        private readonly CreateReservoirUseCase _createReservoirUseCase;
        private readonly IElementTypeQueryRepo _elementTypeQueryRepo;

        // 설계조건
        private double _q = 5000;
        private double _rt = 12;
        private int _n = 2;
        private double _lwl = 0;

        // 수조부
        private double _he = 4.5;
        private double _hf = 0.3;
        private double _hm = 0.15;
        private double _w = 25;
        private double _l = 30;
        private double _m1 = 4;
        private double _m2 = 4;
        private double _m3 = 4;
        private double _m4 = 4;
        private double _wh = 2.5;
        private double _lh = 2.5;
        private double _hh = 2.0;
        private double _ltt = 0;
        private double _slv = 1.0;
        private double _crt;

        // 밸브실
        private double _h1f = 4.0;
        private double _lv = 30.0;
        private double _wv = 5.0;
        private double _lvt = 0;
        private double _we = 2.5;
        private double _trOff = 0.1;
        private double _wp = 1.0;
        private double _hp = 1.0;
        private double _wpThk = 0.3;
        private double _spThk = 0.3;
        private double _slp = 1.0;

        // Con'c 단면
        private string _selectedTankUpperSlabType;
        private string _selectedTankFoundSlabType;
        private string _selectedTankOuterWallType;
        private string _selectedTankInnerWallType;
        private string _selectedHopperWallType;
        private string _selectedTankColumnType;
        private string _selectedTankBeamType;

        private string _selectedValveUpperSlabType;
        private string _selectedValveMidSlabType;
        private string _selectedValveFoundSlabType;
        private string _selectedValveOuterWallType;
        private string _selectedValveInnerWallType;

        private string _selectedSubSlabType;
        private string _selectedHaunchType;
        #endregion

        #region Properties
        public ICommand CreateWTankCommand { get; }

        // 설계조건
        public double Q
        {
            get => _q;
            set
            {
                if (_q != value)
                {
                    _q = value;
                    OnPropertyChanged(nameof(Q));
                    UpdateCRT();
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
                    _rt = value;
                    OnPropertyChanged(nameof(RT));
                    OnPropertyChanged(nameof(RTCheck));
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
                    _n = value;
                    OnPropertyChanged(nameof(N));
                    UpdateCRT();
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
                    _lwl = value;
                    OnPropertyChanged(nameof(LWL));
                }
            }
        }

        // 수조부
        public double He
        {
            get => _he;
            set
            {
                if (_he != value)
                {
                    _he = value;
                    OnPropertyChanged(nameof(He));
                    OnPropertyChanged(nameof(H2F));
                    UpdateCRT();
                }
            }
        }
        public double Hf
        {
            get => _hf;
            set
            {
                if (_hf != value)
                {
                    _hf = value;
                    OnPropertyChanged(nameof(Hf));
                    OnPropertyChanged(nameof(H2F));
                }
            }
        }
        public double Hm
        {
            get => _hm;
            set
            {
                if (_hm != value)
                {
                    _hm = value;
                    OnPropertyChanged(nameof(Hm));
                    OnPropertyChanged(nameof(H2F));
                }
            }
        }
        public double W
        {
            get => _w;
            set
            {
                if (_w != value)
                {
                    _w = value;
                    OnPropertyChanged(nameof(W));
                    UpdateCRT();
                }
            }
        }
        public double L
        {
            get => _l;
            set
            {
                if (_l != value)
                {
                    _l = value;
                    OnPropertyChanged(nameof(L));
                    UpdateCRT();
                }
            }
        }
        public double M1
        {
            get => _m1;
            set { if (_m1 != value) { _m1 = value; OnPropertyChanged(nameof(M1)); } }
        }
        public double M2
        {
            get => _m2;
            set { if (_m2 != value) { _m2 = value; OnPropertyChanged(nameof(M2)); } }
        }
        public double M3
        {
            get => _m3;
            set { if (_m3 != value) { _m3 = value; OnPropertyChanged(nameof(M3)); } }
        }
        public double M4
        {
            get => _m4;
            set { if (_m4 != value) { _m4 = value; OnPropertyChanged(nameof(M4)); } }
        }
        public double Wh
        {
            get => _wh;
            set { if (_wh != value) { _wh = value; OnPropertyChanged(nameof(Wh)); } }
        }
        public double Lh
        {
            get => _lh;
            set { if (_lh != value) { _lh = value; OnPropertyChanged(nameof(Lh)); } }
        }
        public double Hh
        {
            get => _hh;
            set
            {
                if (_hh != value)
                {
                    _hh = value;
                    OnPropertyChanged(nameof(Hh));
                    OnPropertyChanged(nameof(H2F));
                }
            }
        }
        public double Ltt
        {
            get => _ltt;
            set { if (_ltt != value) { _ltt = value; OnPropertyChanged(nameof(Ltt)); } }
        }
        public double SLv
        {
            get => _slv;
            set { if (_slv != value) { _slv = value; OnPropertyChanged(nameof(SLv)); } }
        }
        public double CRT
        {
            get => _crt;
            private set
            {
                if (_crt != value)
                {
                    _crt = value;
                    OnPropertyChanged(nameof(CRT));
                    OnPropertyChanged(nameof(RTCheck));
                }
            }
        }
        public string RTCheck => CRT >= RT ? "O.K" : "N.G";

        // 밸브실
        public double H1F
        {
            get => _h1f;
            set
            {
                if (_h1f != value)
                {
                    _h1f = value;
                    OnPropertyChanged(nameof(H1F));
                    OnPropertyChanged(nameof(H2F));
                }
            }
        }
        // Hh + He + Hf + Hm = 수조부 전체 내부 높이, 슬래브 두께는 추후 반영
        public double H2F => Hh + He + Hf + Hm - H1F;

        public double Lv
        {
            get => _lv;
            set { if (_lv != value) { _lv = value; OnPropertyChanged(nameof(Lv)); } }
        }
        public double Wv
        {
            get => _wv;
            set { if (_wv != value) { _wv = value; OnPropertyChanged(nameof(Wv)); } }
        }
        public double Lvt
        {
            get => _lvt;
            set { if (_lvt != value) { _lvt = value; OnPropertyChanged(nameof(Lvt)); } }
        }
        public double We
        {
            get => _we;
            set { if (_we != value) { _we = value; OnPropertyChanged(nameof(We)); } }
        }
        public double TrOff
        {
            get => _trOff;
            set { if (_trOff != value) { _trOff = value; OnPropertyChanged(nameof(TrOff)); } }
        }
        public double Wp
        {
            get => _wp;
            set { if (_wp != value) { _wp = value; OnPropertyChanged(nameof(Wp)); } }
        }
        public double Hp
        {
            get => _hp;
            set { if (_hp != value) { _hp = value; OnPropertyChanged(nameof(Hp)); } }
        }
        public double WpThk
        {
            get => _wpThk;
            set { if (_wpThk != value) { _wpThk = value; OnPropertyChanged(nameof(WpThk)); } }
        }
        public double SpThk
        {
            get => _spThk;
            set { if (_spThk != value) { _spThk = value; OnPropertyChanged(nameof(SpThk)); } }
        }
        public double SLp
        {
            get => _slp;
            set { if (_slp != value) { _slp = value; OnPropertyChanged(nameof(SLp)); } }
        }

        // Con'c 단면
        public ObservableCollection<string> SlabTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> WallTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ColumnTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> BeamTypes { get; set; } = new ObservableCollection<string>();

        public string SelectedTankUpperSlabType
        {
            get => _selectedTankUpperSlabType;
            set
            {
                if (_selectedTankUpperSlabType != value)
                {
                    _selectedTankUpperSlabType = value;
                    OnPropertyChanged(nameof(SelectedTankUpperSlabType));
                }
            }
        }
        public string SelectedTankFoundSlabType
        {
            get => _selectedTankFoundSlabType;
            set
            {
                if (_selectedTankFoundSlabType != value)
                {
                    _selectedTankFoundSlabType = value;
                    OnPropertyChanged(nameof(SelectedTankFoundSlabType));
                }
            }
        }
        public string SelectedTankOuterWallType
        {
            get => _selectedTankOuterWallType;
            set
            {
                if (_selectedTankOuterWallType != value)
                {
                    _selectedTankOuterWallType = value;
                    OnPropertyChanged(nameof(SelectedTankOuterWallType));
                }
            }
        }
        public string SelectedTankInnerWallType
        {
            get => _selectedTankInnerWallType;
            set
            {
                if (_selectedTankInnerWallType != value)
                {
                    _selectedTankInnerWallType = value;
                    OnPropertyChanged(nameof(SelectedTankInnerWallType));
                }
            }
        }
        public string SelectedHopperWallType
        {
            get => _selectedHopperWallType;
            set
            {
                if (_selectedHopperWallType != value)
                {
                    _selectedHopperWallType = value;
                    OnPropertyChanged(nameof(SelectedHopperWallType));
                }
            }
        }
        public string SelectedTankColumnType
        {
            get => _selectedTankColumnType;
            set
            {
                if (_selectedTankColumnType != value)
                {
                    _selectedTankColumnType = value;
                    OnPropertyChanged(nameof(SelectedTankColumnType));
                }
            }
        }
        public string SelectedTankBeamType
        {
            get => _selectedTankBeamType;
            set
            {
                if (_selectedTankBeamType != value)
                {
                    _selectedTankBeamType = value;
                    OnPropertyChanged(nameof(SelectedTankBeamType));
                }
            }
        }
        public string SelectedValveUpperSlabType
        {
            get => _selectedValveUpperSlabType;
            set
            {
                if (_selectedValveUpperSlabType != value)
                {
                    _selectedValveUpperSlabType = value;
                    OnPropertyChanged(nameof(SelectedValveUpperSlabType));
                }
            }
        }
        public string SelectedValveMidSlabType
        {
            get => _selectedValveMidSlabType;
            set
            {
                if (_selectedValveMidSlabType != value)
                {
                    _selectedValveMidSlabType = value;
                    OnPropertyChanged(nameof(SelectedValveMidSlabType));
                }
            }
        }
        public string SelectedValveFoundSlabType
        {
            get => _selectedValveFoundSlabType;
            set
            {
                if (_selectedValveFoundSlabType != value)
                {
                    _selectedValveFoundSlabType = value;
                    OnPropertyChanged(nameof(SelectedValveFoundSlabType));
                }
            }
        }
        public string SelectedValveOuterWallType
        {
            get => _selectedValveOuterWallType;
            set
            {
                if (_selectedValveOuterWallType != value)
                {
                    _selectedValveOuterWallType = value;
                    OnPropertyChanged(nameof(SelectedValveOuterWallType));
                }
            }
        }
        public string SelectedValveInnerWallType
        {
            get => _selectedValveInnerWallType;
            set
            {
                if (_selectedValveInnerWallType != value)
                {
                    _selectedValveInnerWallType = value;
                    OnPropertyChanged(nameof(SelectedValveInnerWallType));
                }
            }
        }
        public string SelectedSubSlabType
        {
            get => _selectedSubSlabType;
            set
            {
                if (_selectedSubSlabType != value)
                {
                    _selectedSubSlabType = value;
                    OnPropertyChanged(nameof(SelectedSubSlabType));
                }
            }
        }
        public string SelectedHaunchType
        {
            get => _selectedHaunchType;
            set
            {
                if (_selectedHaunchType != value)
                {
                    _selectedHaunchType = value;
                    OnPropertyChanged(nameof(SelectedHaunchType));
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
        public WaterTankViewModel(CreateReservoirUseCase useCase, IDialogService dialogService, IElementTypeQueryRepo elementTypeQueryRepo)
        {
            _createReservoirUseCase = useCase;
            _dialogService = dialogService;
            _elementTypeQueryRepo = elementTypeQueryRepo;

            LoadElementTypes();
            UpdateCRT();

            CreateWTankCommand = new RelayCommand(CreateWaterTank);
        }

        private void LoadElementTypes()
        {
            SlabTypes.Clear();
            WallTypes.Clear();
            ColumnTypes.Clear();
            BeamTypes.Clear();

            foreach (var slabTypeName in _elementTypeQueryRepo.GetSlabTypeNames())
                SlabTypes.Add(slabTypeName);

            foreach (var wallTypeName in _elementTypeQueryRepo.GetWallTypeNames())
                WallTypes.Add(wallTypeName);

            foreach (var columnTypeName in _elementTypeQueryRepo.GetColumnTypeNames())
                ColumnTypes.Add(columnTypeName);

            foreach (var beamTypeName in _elementTypeQueryRepo.GetBeamTypeNames())
                BeamTypes.Add(beamTypeName);

            SelectedTankUpperSlabType = SlabTypes.FirstOrDefault(st => st.Contains("300mm")) ?? SlabTypes.FirstOrDefault();
            SelectedTankFoundSlabType = SlabTypes.FirstOrDefault(st => st.Contains("500mm")) ??
                                        SlabTypes.FirstOrDefault(st => st.Contains("300mm")) ??
                                        SlabTypes.FirstOrDefault();
            SelectedTankOuterWallType = WallTypes.FirstOrDefault(wt => wt.Contains("300mm")) ?? WallTypes.FirstOrDefault();
            SelectedTankInnerWallType = WallTypes.FirstOrDefault(wt => wt.Contains("300mm")) ?? WallTypes.FirstOrDefault();
            SelectedHopperWallType = WallTypes.FirstOrDefault(wt => wt.Contains("300mm")) ?? WallTypes.FirstOrDefault();
            SelectedTankColumnType = ColumnTypes.FirstOrDefault(ct => ct.Contains("450 x 600")) ?? ColumnTypes.FirstOrDefault();
            SelectedTankBeamType = BeamTypes.FirstOrDefault(bt => bt.Contains("600mm")) ?? BeamTypes.FirstOrDefault();

            SelectedValveUpperSlabType = SlabTypes.FirstOrDefault(st => st.Contains("300mm")) ?? SlabTypes.FirstOrDefault();
            SelectedValveMidSlabType = SlabTypes.FirstOrDefault(st => st.Contains("300mm")) ?? SlabTypes.FirstOrDefault();
            SelectedValveFoundSlabType = SlabTypes.FirstOrDefault(st => st.Contains("500mm")) ?? SlabTypes.FirstOrDefault();
            SelectedValveOuterWallType = WallTypes.FirstOrDefault(wt => wt.Contains("300mm")) ?? WallTypes.FirstOrDefault();
            SelectedValveInnerWallType = WallTypes.FirstOrDefault(wt => wt.Contains("300mm")) ?? WallTypes.FirstOrDefault();

            SelectedSubSlabType = SlabTypes.FirstOrDefault(st => st.Contains("100mm")) ?? SlabTypes.FirstOrDefault();
            SelectedHaunchType = BeamTypes.FirstOrDefault(bt => bt.Contains("헌치")) ??
                                 BeamTypes.FirstOrDefault(bt => bt.Contains("Haunch")) ??
                                 BeamTypes.FirstOrDefault();
        }
        #endregion

        #region Methods
        private void UpdateCRT()
        {
            if (_q > 0)
                CRT = (W * L * He * N) / Q * 24;
        }

        private void CreateWaterTank(object? obj)
        {
            designConditionDto = new ReservoirDesignConditionDto(Q, RT, N, LWL);
            tankDto = new ReservoirTankDto(He, Hf, Hm, W, L, M1, M2, M3, M4, Wh, Lh, Hh, Ltt);
            valveDto = new ReservoirValveDto(H1F, Lv, Wv, Lvt);
            typeSelectionDto = new ReservoirSelectedTypeIdDto("a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a");

            reservoirCreationRequestDto = new ReservoirCreationRequestDto(designConditionDto, tankDto, valveDto, typeSelectionDto);
            _createReservoirUseCase.Execute(reservoirCreationRequestDto);
        }
        #endregion
    }
}
