using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases;
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

        private double _q = 5000;
        private double _rt = 12;
        private int _n = 2; 
        private double _lwl = 0;

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
                    OnPropertyChanged(nameof(_selectedTankFoundSlabType));
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

            CreateWTankCommand = new RelayCommand(CreateWaterTank);
        }

        // 목록 초기화
        private void LoadElementTypes()
        {
            // 목록 초기화 
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
