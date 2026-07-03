using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases.AutoGenerator;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling
{
    public class PumpingStationViewModel : ViewModelBase
    {
        #region Fields
        private IDialogService _dialogService;
        private readonly CreatePumpingStationUseCase _createPumpingStationUseCase;
        private readonly IUsageLogger _usageLogger;
        private readonly IExcelReader _excelReader;
        private readonly IFileDialogService _fileDialogService;
        private string _excelFilePath = string.Empty;
        private string _selectedPumpManufacturer = string.Empty;
        private IReadOnlyDictionary<string, List<string[]>>? _allSheets;
        private Dictionary<string, Dictionary<(double D, double HD), PumpManufacturerSpecDto>>? _manufacturerSpecs;
        private Dictionary<double, PumpValveExtensionDto>? _valveExtensions;
        // HasCheckValve 여부에 따라 선택된 밸브받침 제원 (Excel 미로드/관경 미매칭 시 0 기본값)
        private PumpValveDimensionDto _selectedValveBase = new(100, 100, 100, 0);
        private double _supportBlockWidth = 500;
        private double _supportBlockHeight = 100;

        private string _profileType1ImagePath = "pack://application:,,,/DHBIMWATER.UI;component/Resources/PumpStationImages/TYPE-1_종단제원.png";
        private string _profileType2ImagePath = "pack://application:,,,/DHBIMWATER.UI;component/Resources/PumpStationImages/TYPE-2_종단제원.png";
        private string _profileType3ImagePath = "pack://application:,,,/DHBIMWATER.UI;component/Resources/PumpStationImages/TYPE-3_종단제원.png";
        private string _planLeftImagePath = "pack://application:,,,/DHBIMWATER.UI;component/Resources/PumpStationImages/TYPE-1_평면제원-좌안진입.png";
        private string _planRightImagePath = "pack://application:,,,/DHBIMWATER.UI;component/Resources/PumpStationImages/TYPE-1_평면제원-우안진입.png";
        private string _planDefaultImagePath = "pack://application:,,,/DHBIMWATER.UI;component/Resources/PumpStationImages/TYPE-1_평면제원-측면진입.png";

        // 설계조건
        private string _selectedPumpingStationType = "Type1";
        private string _selectedEntranceType = "좌안부";
        private double _d = 800.0;
        private double _hd = 5.0;
        private int _n = 3;
        private double _lwl = 0.0;
        private double _hwl = 2.5;
        private bool _hasCheckValve = false;

        // 종단제원
        private double _b1 = 1200.0;
        private double _b3 = 7000;
        private double _b4 = 3000.0;
        private double _b6 = 700.0;
        private double _b7 = 3000.0;
        private double _b7Base = 3000.0;
        private double _h1 = 500.0;
        private double _h6 = 600.0;
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
        private int _ns1;
        private double _hs1 = 200;

        private double _h5;

        // 평면제원
        private double _b2 = 3500.0;
        private double _b8;
        private bool _isRectangularOpening = true;
        private double _b5 = 1600.0;
        private double _b9 = 4500.0;
        private double _l5;
        private double _b10 = 0.0;

        // 부재 유형
        private double _t1 = 400.0;
        private double _t2;
        private double _t3 = 400.0;
        private double _t4;
        private double _t5;
        private double _t5Prime;
        private double _t6 = 300.0;
        private double _gb1 = 500;
        private double _gh1 = 700;
        private double _hb1 = 500;
        private double _hh1 = 500;

        // 힌트
        private string _hintTitle = string.Empty;
        private string _hintDescription = string.Empty;
        private string _currentHintKey = string.Empty;
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
                    OnPropertyChanged(nameof(ProfileImagePath));
                    OnPropertyChanged(nameof(T5Visibility));
                    OnPropertyChanged(nameof(T6Visibility));
                    OnPropertyChanged(nameof(B4Visibility));
                    OnPropertyChanged(nameof(T5PrimeVisibility));
                    OnPropertyChanged(nameof(HB1Visibility));
                    OnPropertyChanged(nameof(HH1Visibility));
                    OnPropertyChanged(nameof(EntranceTypes));
                    UpdateTypeDependents();
                    OnPropertyChanged(nameof(PlanImagePath));
                    if (value != "Type1" && _selectedEntranceType != "측면부")
                    {
                        SelectedEntranceType = "측면부";
                        OnPropertyChanged(nameof(SelectedEntranceType));
                    }
                    RefreshHint();
                }
            }
        }
        // Type1일 때만 좌안부·우안부·측면부 전체 노출, Type2·3은 측면부만
        public IReadOnlyList<string> EntranceTypes => _selectedPumpingStationType == "Type1"
            ? new List<string> { "좌안부", "우안부", "측면부" }
            : new List<string> { "측면부" };
        public string SelectedEntranceType
        {
            get => _selectedEntranceType;
            set
            {
                if (_selectedEntranceType != value)
                {
                    _selectedEntranceType = value;
                    OnPropertyChanged(nameof(SelectedEntranceType));
                    OnPropertyChanged(nameof(PlanImagePath));
                    OnPropertyChanged(nameof(T5Visibility));
                    OnPropertyChanged(nameof(B9Visibility));
                    UpdateTypeDependents();
                    RefreshHint();
                }
            }
        }
        public string ExcelFilePath
        {
            get => _excelFilePath;
            private set
            {
                if (_excelFilePath != value)
                {
                    _excelFilePath = value;
                    OnPropertyChanged(nameof(ExcelFilePath));
                }
            }
        }
        public ObservableCollection<string> PumpManufacturers { get; } = new();
        public string SelectedPumpManufacturer
        {
            get => _selectedPumpManufacturer;
            set
            {
                if (_selectedPumpManufacturer != value)
                {
                    _selectedPumpManufacturer = value;
                    OnPropertyChanged(nameof(SelectedPumpManufacturer));
                    ApplyManufacturerSpec();
                }
            }
        }
        public double SupportBlockWidth
        {
            get => _supportBlockWidth;
            set
            {
                if (_supportBlockWidth != value)
                {
                    _supportBlockWidth = value;
                    OnPropertyChanged(nameof(SupportBlockWidth));
                }
            }
        }
        public double SupportBlockHeight
        {
            get => _supportBlockHeight;
            set
            {
                if (_supportBlockHeight != value)
                {
                    _supportBlockHeight = value;
                    OnPropertyChanged(nameof(SupportBlockHeight));
                }
            }
        }
        public bool HasCheckValve
        {
            get => _hasCheckValve;
            set
            {
                _hasCheckValve = value;
                ApplyValveExtension();
                OnPropertyChanged(nameof(HasCheckValve));
            }
        }

        // 가시성
        public string B4Visibility => SelectedPumpingStationType == "Type1" ? "Visible" : "Collapsed";
        public string T6Visibility => SelectedPumpingStationType == "Type1" ? "Visible" : "Collapsed";
        public string B9Visibility => SelectedEntranceType == "측면부" ? "Collapsed" : "Visible";
        public string T5Visibility => SelectedEntranceType == "측면부" ? "Collapsed" : "Visible";

        // 이미지 경로
        public string PlanDefaultImagePath
        {
            get => _planDefaultImagePath;
            set
            {
                if (_planDefaultImagePath != value)
                {
                    _planDefaultImagePath = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PlanImagePath));
                }
            }
        }
        public string PlanLeftImagePath
        {
            get => _planLeftImagePath;
            set
            {
                if (_planLeftImagePath != value)
                {
                    _planLeftImagePath = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PlanImagePath));   // ← 계산 프로퍼티 갱신
                }
            }
        }
        public string PlanRightImagePath
        {
            get => _planRightImagePath;
            set
            {
                if (_planRightImagePath != value)
                {
                    _planRightImagePath = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PlanImagePath));   // ← 계산 프로퍼티 갱신
                }
            }
        }
        public string ProfileType1ImagePath
        {
            get => _profileType1ImagePath;
            set
            {
                if (_profileType1ImagePath != value)
                {
                    _profileType1ImagePath = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProfileImagePath)); // ← 계산 프로퍼티 갱신
                }
            }
        }
        public string ProfileType2ImagePath
        {
            get => _profileType2ImagePath;
            set
            {
                if (_profileType2ImagePath != value)
                {
                    _profileType2ImagePath = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProfileImagePath));
                }
            }
        }
        public string ProfileType3ImagePath
        {
            get => _profileType3ImagePath;
            set
            {
                if (_profileType3ImagePath != value)
                {
                    _profileType3ImagePath = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProfileImagePath));
                }
            }
        }

        public bool IsRectangularOpening
        {
            get { return _isRectangularOpening; }
            set
            {
                if (_isRectangularOpening != value)
                {
                    _isRectangularOpening = value;
                    OnPropertyChanged(nameof(IsRectangularOpening));
                    OnPropertyChanged(nameof(IsCircularOpening));
                    //OnPropertyChanged(nameof(PlaneImagePath));
                }
            }
        }
        public bool IsCircularOpening
        {
            get => !_isRectangularOpening;
            set => IsRectangularOpening = !value;
        }

        public double D
        {
            get { return _d; }
            set
            {
                if (_d != value)
                {
                    _d = value;
                    UpdateDDependents();
                    ApplyManufacturerSpec();
                    ApplyValveExtension();
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
                    ApplyManufacturerSpec();
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
                    UpdateWLDependents();
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
                    UpdateWLDependents();
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
                    OnPropertyChanged(nameof(B6));
                }
            }
        }
        public double B7
        {
            get { return _b7; }
            set
            {
                _b7Base = value;
                ApplyB7Final();
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
                    UpdateH1Dependents();
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
                    //RecalculateDerivedValues();
                    UpdateH6Dependents();
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
                    //RecalculateDerivedValues();
                    UpdateThetaDependents();
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
                    //RecalculateDerivedValues();
                    UpdateL4Dependents();
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
                    UpdateH3Dependents();
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
                    UpdateH4Dependents();
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
                    //RecalculateDerivedValues();
                    UpdateHSDependents();
                    OnPropertyChanged(nameof(HS));
                }
            }
        }
        // Type1 전용 — 밸브실 계단
        public int NS1
        {
            get => _ns1;
            private set
            {
                if (_ns1 != value)
                {
                    _ns1 = value;
                    OnPropertyChanged(nameof(NS1));
                }
            }
        }
        public double HS1
        {
            get => _hs1;
            set
            {
                if (_hs1 != value)
                {
                    _hs1 = value;
                    OnPropertyChanged(nameof(HS1));
                    UpdateNS1();
                }
            }
        }

        // 읽기 전용
        public double H2 => (HWL - LWL) * 1000;
        public double H5
        {
            get => _h5;
            set
            {
                if (_h5 != value)
                {
                    _h5 = value;
                    UpdateH5Dependents();
                    OnPropertyChanged(nameof(H5));
                }
            }
        }

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
                    //RecalculateDerivedValues();
                    UpdateB8Dependents();
                    OnPropertyChanged(nameof(B8));
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
                    UpdateB5Dependents();
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
                    //RecalculateDerivedValues();
                    UpdateT1Dependents();
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
                    UpdateT3Dependents();
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
                    UpdateT4Dependents();
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
        public double T5Prime
        {
            get { return _t5Prime; }
            set
            {
                if (_t5Prime != value)
                {
                    _t5Prime = value;
                    OnPropertyChanged(nameof(T5Prime));
                }
            }
        }
        public string T5PrimeVisibility => SelectedPumpingStationType == "Type2" ? "Visible" : "Collapsed";
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
        public double HB1
        {
            get { return _hb1; }
            set
            {
                if (_hb1 != value)
                {
                    _hb1 = value;
                    OnPropertyChanged(nameof(HB1));
                }
            }
        }
        public double HH1
        {
            get { return _hh1; }
            set
            {
                if (_hh1 != value)
                {
                    _hh1 = value;
                    OnPropertyChanged(nameof(HH1));
                }
            }
        }
        public string HB1Visibility => SelectedPumpingStationType == "Type2" ? "Visible" : "Collapsed";
        public string HH1Visibility => SelectedPumpingStationType == "Type2" ? "Visible" : "Collapsed";
        //public string PlaneImagePath => _isRectangularOpening ? RectangularImagePath : CircularImagePath;
        public string ProfileImagePath => SelectedPumpingStationType switch
        {
            "Type1" => ProfileType1ImagePath,
            "Type2" => ProfileType2ImagePath,
            "Type3" => ProfileType3ImagePath,
            _ => ProfileType1ImagePath
        };
        public string PlanImagePath => SelectedEntranceType switch
        {
            "좌안부" => PlanLeftImagePath,
            "우안부" => PlanRightImagePath,
            "측면부" => PlanDefaultImagePath,
            _ => PlanDefaultImagePath
        };

        // 힌트
        public string HintTitle
        {
            get => _hintTitle;
            private set { if (_hintTitle != value) { _hintTitle = value; OnPropertyChanged(nameof(HintTitle)); } }
        }
        public string HintDescription
        {
            get => _hintDescription;
            private set { if (_hintDescription != value) { _hintDescription = value; OnPropertyChanged(nameof(HintDescription)); } }
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
        public ICommand ImportExcelCommand { get; }
        public Action? CloseAction { get; set; }
        #endregion

        #region Constructor
        public PumpingStationViewModel(CreatePumpingStationUseCase useCase, IDialogService dialogService, IElementTypeQueryRepo elementTypeQueryRepo, IUsageLogger usageLogger, IExcelReader excelReader, IFileDialogService fileDialogService)
        {
            _createPumpingStationUseCase = useCase;
            _dialogService = dialogService;
            _usageLogger = usageLogger;
            _excelReader = excelReader;
            _fileDialogService = fileDialogService;

            CreatePumpingStationCommand = new RelayCommand(CreatePumpingStation);
            ImportExcelCommand = new RelayCommand(ImportFromExcel);

            InitializeDerivedValues();
        }
        #endregion

        #region Methods
        private void ImportFromExcel(object? obj)
        {
            var filePath = _fileDialogService.OpenFile("Excel 파일 선택", "Excel Files|*.xlsx;*.xls");
            if (filePath == null) return;

            ExcelFilePath = filePath;
            _allSheets = _excelReader.Read(filePath);

            LoadPumpManufacturers();
        }

        private void LoadPumpManufacturers()
        {
            PumpManufacturers.Clear();
            SelectedPumpManufacturer = string.Empty;

            if (_allSheets == null) return;
            var manufacturerSheetName = "펌프제작사 목록";

            if (!_allSheets.TryGetValue(manufacturerSheetName, out var mfRows)) return;

            // B6 = row index 5, column index 1
            foreach (var row in mfRows.Skip(5))
            {
                if (row.Length < 2) break;
                var value = row[1]?.Trim();
                if (string.IsNullOrWhiteSpace(value)) break;
                PumpManufacturers.Add(value);
            }

            // 스펙 딕셔너리 선파싱 — 이후 선택/D/HD 변경 시 딕셔너리 조회만
            _manufacturerSpecs = new ParsePumpManufacturerSpecsUseCase().Execute(_allSheets, PumpManufacturers);
            _valveExtensions = new ParseValveExtensionUseCase().Execute(_allSheets);

            if (PumpManufacturers.Count > 0)
                SelectedPumpManufacturer = PumpManufacturers[0];

            ApplyValveExtension();
        }

        private void ApplyManufacturerSpec()
        {
            if (string.IsNullOrEmpty(_selectedPumpManufacturer) || _manufacturerSpecs == null) return;
            if (!_manufacturerSpecs.TryGetValue(_selectedPumpManufacturer, out var specs)) return;

            var candidates = specs.Keys.Where(k => k.D == D).ToList();
            if (candidates.Count == 0) return;

            // HD가 정확히 없으면 가장 가까운 값 사용
            var bestKey = candidates.MinBy(k => Math.Abs(k.HD - HD));
            if (!specs.TryGetValue(bestKey, out var dto)) return;

            IsRectangularOpening = dto.OpeningShape == "사각형";
            B5 = dto.B5;
            SupportBlockWidth = dto.SupportBlockWidth;
            SupportBlockHeight = dto.SupportBlockHeight;
        }
        private void ApplyValveExtension()
        {
            if (_valveExtensions == null) return;
            if (!_valveExtensions.TryGetValue(D, out var ext)) return;

            _selectedValveBase = HasCheckValve ? ext.WithCheckValve : ext.WithoutCheckValve;

            _b7Base = HasCheckValve
                ? ext.TotalExtension + ext.ValveExtension + 2200
                : ext.TotalExtension + 1200;
            ApplyB7Final();
        }
        private void CreatePumpingStation(object? obj)
        {
            designConditionDto = new PumpDesignConditionDto(SelectedPumpingStationType, SelectedEntranceType, D, HD, H2, N, LWL, HWL, SupportBlockWidth, SupportBlockHeight);
            profileSpecDto = new PumpProfileSpecDto(B1, B3, B4, B6, B7, H1, H5, H6, SelectedTheta, L1, L2, L3, L4, H3, H4, H7, OB1, OH1, NS, HB1, HH1, HS, T1, T2, T3, T4, T5Prime, GB1, GH1, B2, IsRectangularOpening, B5);
            planSpecDto = new PumpPlanSpecDto(B8, B9, L5, B10, T5, T6);
            //typeSelectionDto = new PumpTypeSelectionDto(T1, T2, T3, T4, T5, T6, GB1, GH1);
            creationRequestDto = new PumpCreationRequestDto(designConditionDto, planSpecDto, profileSpecDto, _selectedValveBase);

            _ = _usageLogger.LogAsync();
            _createPumpingStationUseCase.Execute(creationRequestDto);
            CloseAction?.Invoke();
        }
        // 프로퍼티 업데이트
        // 생성시 초기화 메서드
        private void InitializeDerivedValues()
        {
            // 종단제원
            _l2 = _h1;
            _h4 = Math.Ceiling(2.9 * _d / 100) * 100;

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
            _h3 = 1200 + Math.Ceiling((H2 + _h4) / 100) * 100 - (H2 + _h4);
            _h7 = 1000 + (Math.Ceiling((_h6 + _d) / 100.0) * 100 - (_h6 + _d));
            UpdateNS1(); // NS1 → B7(ApplyB7Final) 연쇄 계산
            _ns = (int)Math.Floor((_h4 - _h1) / _hs);
            _h5 = H2 + _h3 + _h4 - T1;

            _b8 = Math.Ceiling(3 * _d / 100) * 100;

            // 부재유형
            _t4 = Math.Ceiling((H5 + _t1) * 0.1 / 100) * 100;
            _l5 = _b7 + _t3 + _b6 + _b5 / 2 + _l4 - _b10 - _t4; // 평면제원

            _t2 = _t4 + 100;

            if (_b8 < 3000)
                _t5 = 400.0;
            else if (3000 <= _b8 && _b8 <= 4000)
                _t5 = 500.0;
            else
                _t5 = 600.0;

            _gb1 = 500;
            _gh1 = _t1 + 300;
            _t5Prime = Math.Max(400, Math.Ceiling((2 * _t3 + _b7 - _t4) / 500.0) * 50);

            OnPropertyChanged(nameof(L2));
            OnPropertyChanged(nameof(L3));
            OnPropertyChanged(nameof(L4));
            OnPropertyChanged(nameof(H3));
            OnPropertyChanged(nameof(H4));
            OnPropertyChanged(nameof(H5));
            OnPropertyChanged(nameof(H7));
            OnPropertyChanged(nameof(T2));
            OnPropertyChanged(nameof(T4));
            OnPropertyChanged(nameof(NS));
            OnPropertyChanged(nameof(B8));
            OnPropertyChanged(nameof(T5));
            OnPropertyChanged(nameof(T5Prime));
            OnPropertyChanged(nameof(L5));
            OnPropertyChanged(nameof(GH1));
        }
        private void UpdateDDependents()
        {
            H4 = Math.Ceiling(2.9 * _d / 100) * 100;
            B8 = Math.Ceiling(3 * _d / 100) * 100;
            UpdateThetaDependents();
            UpdateB6Calculation();
            UpdateH7Calculation();
        }
        private void UpdateThetaDependents()
        {
            if (SelectedTheta == "30˚")
            {
                L3 = Math.Ceiling((_h4 - _h1) / Math.Tan(30 * Math.PI / 180) / 100) * 100;
                L4 = Math.Ceiling(3 * _d / 100) * 100;
            }
            else if (SelectedTheta == "45˚")
            {
                L3 = Math.Ceiling((_h4 - _h1) / Math.Tan(45 * Math.PI / 180) / 100) * 100;
                L4 = Math.Ceiling(4.5 * _d / 100) * 100;
            }
        }
        private void UpdateH1Dependents()
        {
            L2 = _h1;
            NS = (int)Math.Floor((_h4 - _h1) / _hs);
            UpdateThetaDependents();   // L3 재계산
            UpdateH3Calculation();     // H3 재계산
        }
        private void UpdateH6Dependents()
        {
            UpdateH7Calculation();
        }
        private void UpdateT1Dependents()
        {
            H5 = H2 + H3 + H4 - T1;
            GH1 = _t1 + 300;
        }
        private void UpdateHSDependents()
        {
            NS = (int)Math.Floor((_h4 - _h1) / _hs);
        }
        private void UpdateH3Calculation()
        {
            H3 = 1200 + Math.Ceiling((H2 + _h4) / 100) * 100 - (H2 + _h4);
        }
        private void UpdateH3Dependents()
        {
            H5 = H2 + H3 + H4 - T1;
        }
        private void UpdateH4Dependents()
        {
            UpdateH3Calculation();
            NS = (int)Math.Floor((_h4 - _h1) / _hs);
            H5 = H2 + H3 + H4 - T1; // H3가 변하지 않아도 H4 변경분 반영
        }
        private void UpdateWLDependents()
        {
            OnPropertyChanged(nameof(H2));
            UpdateH3Calculation();
            H5 = H2 + H3 + H4 - T1;
        }
        //private void UpdateB2Dependents()
        //{
        //    // B2 계산
        //    double sum = _h1 + H2 + _h3 + _t1;
        //    B2 = sum <= 5000 ? 3000.0
        //       : sum <= 7000 ? 3500.0
        //       : 4000.0;
        //}
        private void UpdateH5Dependents()
        {
            T4 = Math.Ceiling((H5 + _t1) * 0.1 / 100) * 100;
        }
        private void UpdateT4Dependents()
        {
            T2 = _t4 + 100;
            L5 = _b7 + _t3 + _b6 + _b5 / 2 + _l4 - _b10 - _t4;
            UpdateT5PrimeCalculation();
        }
        private void UpdateB8Dependents()
        {
            T5 = _b8 < 3000 ? 400.0
               : _b8 <= 4000 ? 500.0
               : 600.0;
        }
        private void UpdateL4Dependents()
        {
            L5 = _b7 + _t3 + _b6 + _b5 / 2 + _l4 - _b10 - _t4;
        }
        private void UpdateTypeDependents()
        {
            B4 = SelectedPumpingStationType == "Type1" && SelectedEntranceType == "측면부" ? 4500
               : SelectedPumpingStationType == "Type1" ? 3000
               : 0;

            PlanDefaultImagePath = SelectedPumpingStationType == "Type1"
                ? "pack://application:,,,/DHBIMWATER.UI;component/Resources/PumpStationImages/TYPE-1_평면제원-측면진입.png"
                : "pack://application:,,,/DHBIMWATER.UI;component/Resources/PumpStationImages/TYPE-2&3_평면제원.png";

            UpdateB6Calculation();
            ApplyB7Final();
        }
        private void UpdateB5Dependents()
        {
            UpdateB6Calculation();
        }
        private void UpdateB6Calculation()
        {
            B6 = _selectedPumpingStationType == "Type1" ? 700 : Math.Max(D * 1.5 - B5 / 2, 700);
        }
        private void UpdateH7Calculation()
        {
            H7 = 1000 + (Math.Ceiling((_h6 + _d) / 100.0) * 100 - (_h6 + _d));
            UpdateNS1();
        }
        private void UpdateNS1()
        {
            if (_selectedPumpingStationType != "Type1") return;
            var total = H7 + D + H6;
            var mod = total % _hs1;
            NS1 = (int)Math.Floor(total / _hs1) - (mod < 0.001 ? 1 : 0);
            ApplyB7Final();
        }

        private void ApplyB7Final()
        {
            var baseValue = _selectedPumpingStationType == "Type1"
                ? Math.Max(_b7Base, _ns1 * 300 + 1000)  // 계단 폭 300mm 고정
                : _b7Base;

            // B7 최종값은 100mm 단위로 올림
            var effective = Math.Ceiling(baseValue / 100.0) * 100;

            if (_b7 != effective)
            {
                _b7 = effective;
                UpdateB7Dependents();
                OnPropertyChanged(nameof(B7));
            }
        }

        private void UpdateB7Dependents()
        {
            L5 = _b7 + _t3 + _b6 + _b5 / 2 + _l4 - _b10 - _t4;
            UpdateT5PrimeCalculation();
        }
        private void UpdateT3Dependents()
        {
            L5 = _b7 + _t3 + _b6 + _b5 / 2 + _l4 - _b10 - _t4;
            UpdateT5PrimeCalculation();
        }
        private void UpdateT5PrimeCalculation()
        {
            T5Prime = Math.Max(400, Math.Ceiling((2 * _t3 + _b7 - _t4) / 500.0) * 50);
        }

        public void SetHint(string key)
        {
            _currentHintKey = key;
            RefreshHint();
        }

        private void RefreshHint()
        {
            if (string.IsNullOrEmpty(_currentHintKey)) return;
            var hint = BuildHint(_currentHintKey);
            HintTitle = hint.title;
            HintDescription = hint.desc;
        }

        private (string title, string desc) BuildHint(string key)
        {
            var type = _selectedPumpingStationType;
            var ent = _selectedEntranceType;

            return key switch
            {
                "B4" when type == "Type1" && ent == "측면부"
                    => ("B4", "펌프 유지관리 공간. 차량 진입 폭 및 펌프받침폭 고려하여 최소 4.5m 적용"),
                "B4" when type == "Type1" && (ent == "좌안부" || ent == "우안부")
                    => ("B4", "펌프 유지관리 공간. 펌프받침폭 고려하여 최소 3.0m 적용"),    
                "B6" when type == "Type2" || type == "Type3"
                    => ("B6", "KDS 67 30 25 양배수장 구조, P41, 4.3.1.3 흡입관의 설계\"에 따라 설계펌프 중심에서 벽체 끝까지 1.5D 확보. "),

                "B7" when type == "Type2" || type == "Type3"
                                   => ("B7", "1. 밸브 1만 적용 시\r\n밸브 + 관로 연장\r\n밸브 설치 및 유지관리를 위해 벽체에서 플랜지까지 600mm 공간확보 \n\n1. 밸브 1 + 2 적용 시\r\n밸브 + 관로 연장\r\n밸브 설치 및 유지관리를 위해 벽체에서 플랜지까지 600mm 공간확보\r\n밸브 1과 밸브 2사이 길이 1m의 관 설치 "),

                _ when _paramHints.TryGetValue(key, out var h) => h,
                _ => (string.Empty, string.Empty)
            };
        }

        private static readonly Dictionary<string, (string title, string desc)> _paramHints = new()
        {
            //// 설계조건
            //["D"] = ("D — 펌프 구경", "펌프 흡입관 구경(mm). H4(잠김깊이), L3·L4(경사부 길이), B8(오프닝 폭) 등 종·평면 제원 산정의 기준값."),
            //["HD"] = ("HD — 양정고", "펌프 총 양정(m). 제작사 스펙 매칭 기준으로 사용."),
            //["N"] = ("N — 펌프 대수", "펌프실에 설치되는 펌프의 총 대수."),
            //["LWL"] = ("LWL — 저수위 (EL, m)", "저수위(Low Water Level). H2(유효수심) 산정 기준. H2 = HWL − LWL."),
            //["HWL"] = ("HWL — 고수위 (EL, m)", "고수위(High Water Level). H2(유효수심) 및 H3(여유고) 산정 기준."),

            // 종단제원 — B
            ["B1"] = ("B1", "제진기 유지관리 공간. 난간 설치로 인한 순폭 1m 확보를 위해 1,200mm 적용."),
            ["B2"] = ("B2", "제진기 설치 공간. 시공성 및 경제성 고려 3.5m 적용."),
            ["B3"] = ("B3", "제진기, 컨베이어벨트 설치 및 유지관리 공간. 유지관리 편의성 및 경제성 & 전기실 평균 사이즈 고려 7.0m 적용."),
            ["B4"] = ("B4", "펌프 유지관리 공간. 차량 진입 공간을 고려하여 최소 4.5m. 펌프받침폭 고려."),
            ["B5"] = ("B5", "펌프 INPUT DATA에서 추출"),
            ["B6"] = ("B6", "토출관 플랜지 접합 공간. 경제성 고려 700 적용."),
            ["B7"] = ("B7", "1. 밸브 1만 적용 시\r\nMAX(밸브 + 관로 연장, 계단 설치 연장+1000) 적용\r\n밸브 설치 및 유지관리를 위해 벽체에서 플랜지까지 600mm 공간확보\r\n계단폭은 300mm로 고정, 계단 끝단 동선확보를 위한 1m 여유공간 적용.\n\n2. 밸브 1 + 2 적용 시\r\nMAX(밸브 + 관로 연장, 계단 설치 연장+1000) 적용\r\n밸브 설치 및 유지관리를 위해 벽체에서 플랜지까지 600mm 공간확보\r\n밸브 1과 밸브 2사이 길이 1m의 관 설치\r\n계단폭은 300mm로 고정, 계단 끝단 동선확보를 위한 1m 여유공간 적용"),

            // 종단제원 — H
            ["H1"] = ("H1", "제진기 작동능력 취약 범위"),
            ["H2"] = ("H2", "유효저수높이(H.W.L - L.W.L)"),
            ["H3"] = ("H3", "여유고. 「빗물펌프장 수문 유지관리 및 설계요령(2023, 서울시), P111, 라. 펌프실」, \"펌프실은 옥내에 설치하여 침수 위험에 대비하여야 하며 계획 내 수위에 여유고(1m 이상)를 더한 표고보다 높은 위치에 설치해야 한다.\" 따라서, H.W.L + 1m = 펌프장 상부슬래브 상면 EL.이어야 하지만, 부지 계획고도 여유고가 적용되어야 하고 「KDS 61 45 00 펌프장시설 설계기준, P14, 9.펌프장」, \"펌프장 바닥은 구내의 지반면보다 적어도 15cm 높게 한다\"에 따라 20cm 단차 적용 ⇒ 1m + 0.2m - 상부슬래브 두께 + 전체 높이를 정치수화 하기 위한 치수 추가"),
            ["H4"] = ("H4", "2024년 행안부 지침, 「240701 3.펌프 흡입관의 잠김 깊이와 펌프의 정지수위.pptx」, \"농어촌공사 기준을 준용하여 2.9D 이상\".정치수(roundup) 적용."),
            ["H5"] = ("H5", "H2 + H3 + H4 − T1 로 자동 산정."),
            ["H6"] = ("H6", "밸브와 토출관 플랜지 접합 공간. 경제성 고려 600 적용."),
            ["H7"] = ("H7", "관보호공 미적용을 위한 최소 토피. 「도로설계요령(2020), 제2권 토공 및 배수, P726, 6.2.3 관형 암거의 설계」, \"토피가 1.0m 이하의 경우는 RC 2종 360° 콘크리트 기초도 비교 검토한다.\", 또한 밸브실 높이를 정치수화 하기 위한 치수 추가"),
            ["HS"] = ("HS", "200mm 고정. 나머지 발생시 최하단에서 나머지 반영한 높이 적용"),

            // 종단제원 — L
            ["L1"] = ("L1", "300mm 고정. 구조적 최적설계."),
            ["L2"] = ("L2", "H1과 1:1 경사"),
            ["L3"] = ("L3", "하부슬래브 단차와 경사(θ)에 대한 길이. 정치수(roundup) 적용"),
            ["L4"] = ("L4", "θ = 30° 인 경우 3D, 45°의 경우 4.5D. 정치수(roundup) 적용"),

            // 기초 경사부 기울기
            ["θ"] = ("θ", "「농업생산기반정비사업계획 설계기준-배수편(2012), P215, 다.흡입수조」 및 「빗물펌프장 수문 유지관리 및 설계요령(2023), P112, 9)흡입부의 크기 검토」 등에 30° 또는 45°를 적용하도록 규정하고 있으나, 45° 적용시 급한 경사로 인한 시공성 문제가 발생할 수 있으므로 30°를 권고안으로 적용"),

            // 종단제원 — T
            ["T1"] = ("T1", "400mm 고정. 구조적 최적설계."),
            ["T2"] = ("T2", "벽체 두께 + 100mm. 구조적 최적설계."),
            ["T3"] = ("T3", "400mm 고정. 구조적 최적설계."),
            ["T4"] = ("T4", "토압 높이의 10%의 정치수 반영(ROUNDUP). 구조적 최적설계."),
            ["T5Prime"] = ("T6", "캔틸레버 길이 4m 이하는 400mm, 이후 500mm 증가시마다 50mm 증가"),

            // 종단제원 — 기타
            ["OB1"] = ("OB", "2000mm 고정. 수리적 최적설계."),
            ["OH1"] = ("OH1", "3000mm 고정. 수리적 최적설계."),
            ["GB1"] = ("GB1", "500mm 고정. 구조적 최적설계."),
            ["GH1"] = ("GH1", "돌출높이 300mm 고정. 구조적 최적설계. 지시선 표기시에는 상부슬래브 두께를 포함하여 높이 표기"),
            ["HB1"] = ("HB1", "500mm 고정. 구조적 최적설계."),
            ["HH1"] = ("HH1", "500mm 고정. 구조적 최적설계."),
            ["NS1"] = ("NS1", "밸브실 내 계단 단수.\r\n- 상단에서 1단 아래에서 계단시작\r\n- 밸브실 높이 / 단높이 시 나머지 발생시 잔여 높이를 최상단에서 적용"),
            ["HS1"] = ("HS1", "밸브실 내 계단 높이 200mm 고정. 나머지 발생시 최하단에서 나머지 반영한 높이 적용"),

            // 평면제원
            ["B8"] = ("B8", "「KDS 67 30 25 양배수장 구조 설계, P41, 4.3.1.3 흡입관의 설계」. 정치수(roundup) 적용"),
            ["B9"] = ("B9", "계단 및 지배수펌프 개구부(1.0m)와 유지관리차량 진입 및 여유동선(3.5m) 고려."),
            ["B10"] = ("B10", "직접기초시 부력키 불필요. 말뚝기초시 하부슬래브 두께와 동일폭 적용권장."),
            ["L5"] = ("L5", "유입부측 끝이 하부슬래브 경사부를 침범하지 않는 위치까지의 연장."),
            ["T5"] = ("T5", "B8의 치수에 의해 개략산정. \r\nB8 < 3,000 ⇒ T5 = 400\r\n3,000 ≤ B8 ≤ 4,000 ⇒ T5 = 500\r\n4,000 < B8 ⇒ T5 = 600\r\n구조계산 결과에 따라 보정 필요."),
            ["T6"] = ("T6", "흡수정 내 와류 방지벽 두께. 통상 300mm 적용."),
        };
        #endregion
    }
}
