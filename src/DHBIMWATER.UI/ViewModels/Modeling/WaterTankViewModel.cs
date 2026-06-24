using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases.AutoGenerator;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling
{
    public class WaterTankViewModel : ViewModelBase
    {
        #region Fields
        private readonly IDialogService _dialogService;
        private readonly CreateReservoirUseCase _createReservoirUseCase;

        // 설계조건
        private double _q   = 5000;
        private double _rt  = 12;
        private int    _n   = 2;
        private double _lwl = 0;
        private double _hwl = 4.5;     // HWL (m)

        // 수조부 — 단위: mm (He 제외)
        private double _hf  = 300;     // 여유고 (mm)
        private double _hm  = 150;     // 바닥~LWL (mm)
        private double _w   = 25000;   // 지 폭 (mm)
        private double _l   = 30000;   // 지 길이 (mm)
        private double _m1  = 4000;
        private double _m2  = 4000;
        private double _m3  = 4000;
        private double _m4  = 4000;
        private double _wh  = 2500;    // Hopper 폭 (mm)
        private double _lh  = 2500;    // Hopper 길이 (mm)
        private double _hh  = 2000;    // Hopper 깊이 (mm)
        private double _ltt = 0;       // 기초 Toe (mm)
        private double _slv = 1.0;     // 사면경사 (무차원)
        private double _crt;

        // 밸브실 — 단위: mm
        private double _h1f   = 4000;
        private double _lv    = 30000;
        private double _wv    = 5000;
        private double _lvt   = 0;
        private double _we    = 2500;
        private double _trOff = 100;
        private double _wp    = 1000;
        private double _hp    = 1000;
        private double _wpThk = 300;
        private double _spThk = 300;
        private double _slp   = 1000;

        // 단면 두께 (mm)
        private double _stuThk = 300;
        private double _stbThk = 500;
        private double _svuThk = 300;
        private double _svmThk = 300;
        private double _svbThk = 500;
        private double _wteThk = 350;
        private double _wtiThk = 350;
        private double _whThk  = 300;
        private double _wveThk = 300;
        private double _wviThk = 300;
        private double _lcThk  = 100;
        private double _cw     = 500;
        private double _cd     = 500;
        private double _gw     = 500;
        private double _gh     = 600;
        #endregion

        #region Properties
        public ICommand CreateWTankCommand { get; }

        // 설계조건
        public double Q
        {
            get => _q;
            set { if (_q != value) { _q = value; OnPropertyChanged(nameof(Q)); UpdateCRT(); } }
        }
        public double RT
        {
            get => _rt;
            set { if (_rt != value) { _rt = value; OnPropertyChanged(nameof(RT)); OnPropertyChanged(nameof(RTCheck)); } }
        }
        public int N
        {
            get => _n;
            set { if (_n != value) { _n = value; OnPropertyChanged(nameof(N)); UpdateCRT(); } }
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
                    OnPropertyChanged(nameof(He));
                    OnPropertyChanged(nameof(H2F));
                    UpdateCRT();
                }
            }
        }
        public double HWL
        {
            get => _hwl;
            set
            {
                if (_hwl != value)
                {
                    _hwl = value;
                    OnPropertyChanged(nameof(HWL));
                    OnPropertyChanged(nameof(He));
                    OnPropertyChanged(nameof(H2F));
                    UpdateCRT();
                }
            }
        }

        // 수조부
        /// <summary>유효수심 (m) = HWL - LWL. 계산값, 입력 불가.</summary>
        public double He => HWL - LWL;

        public double Hf
        {
            get => _hf;
            set { if (_hf != value) { _hf = value; OnPropertyChanged(nameof(Hf)); OnPropertyChanged(nameof(H2F)); } }
        }
        public double Hm
        {
            get => _hm;
            set { if (_hm != value) { _hm = value; OnPropertyChanged(nameof(Hm)); OnPropertyChanged(nameof(H2F)); } }
        }
        public double W
        {
            get => _w;
            set { if (_w != value) { _w = value; OnPropertyChanged(nameof(W)); UpdateCRT(); } }
        }
        public double L
        {
            get => _l;
            set { if (_l != value) { _l = value; OnPropertyChanged(nameof(L)); UpdateCRT(); } }
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
            set { if (_hh != value) { _hh = value; OnPropertyChanged(nameof(Hh)); OnPropertyChanged(nameof(H2F)); } }
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
            private set { if (_crt != value) { _crt = value; OnPropertyChanged(nameof(CRT)); OnPropertyChanged(nameof(RTCheck)); } }
        }
        public string RTCheck => CRT >= RT ? "O.K" : "N.G";

        // 밸브실
        public double H1F
        {
            get => _h1f;
            set { if (_h1f != value) { _h1f = value; OnPropertyChanged(nameof(H1F)); OnPropertyChanged(nameof(H2F)); } }
        }
        /// <summary>밸브실 2F 순높이 (mm) = Hh + He*1000 + Hf + Hm - H1F</summary>
        public double H2F => Hh + He * 1000 + Hf + Hm - H1F;

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

        // 단면 두께 (mm)
        public double StuThk { get => _stuThk; set { if (_stuThk != value) { _stuThk = value; OnPropertyChanged(nameof(StuThk)); } } }
        public double StbThk { get => _stbThk; set { if (_stbThk != value) { _stbThk = value; OnPropertyChanged(nameof(StbThk)); } } }
        public double SvuThk { get => _svuThk; set { if (_svuThk != value) { _svuThk = value; OnPropertyChanged(nameof(SvuThk)); } } }
        public double SvmThk { get => _svmThk; set { if (_svmThk != value) { _svmThk = value; OnPropertyChanged(nameof(SvmThk)); } } }
        public double SvbThk { get => _svbThk; set { if (_svbThk != value) { _svbThk = value; OnPropertyChanged(nameof(SvbThk)); } } }
        public double WteThk { get => _wteThk; set { if (_wteThk != value) { _wteThk = value; OnPropertyChanged(nameof(WteThk)); } } }
        public double WtiThk { get => _wtiThk; set { if (_wtiThk != value) { _wtiThk = value; OnPropertyChanged(nameof(WtiThk)); } } }
        public double WhThk  { get => _whThk;  set { if (_whThk  != value) { _whThk  = value; OnPropertyChanged(nameof(WhThk));  } } }
        public double WveThk { get => _wveThk; set { if (_wveThk != value) { _wveThk = value; OnPropertyChanged(nameof(WveThk)); } } }
        public double WviThk { get => _wviThk; set { if (_wviThk != value) { _wviThk = value; OnPropertyChanged(nameof(WviThk)); } } }
        public double LcThk  { get => _lcThk;  set { if (_lcThk  != value) { _lcThk  = value; OnPropertyChanged(nameof(LcThk));  } } }
        public double Cw     { get => _cw;     set { if (_cw     != value) { _cw     = value; OnPropertyChanged(nameof(Cw));     } } }
        public double Cd     { get => _cd;     set { if (_cd     != value) { _cd     = value; OnPropertyChanged(nameof(Cd));     } } }
        public double Gw     { get => _gw;     set { if (_gw     != value) { _gw     = value; OnPropertyChanged(nameof(Gw));     } } }
        public double Gh     { get => _gh;     set { if (_gh     != value) { _gh     = value; OnPropertyChanged(nameof(Gh));     } } }
        #endregion

        #region Constructor
        public WaterTankViewModel(CreateReservoirUseCase useCase, IDialogService dialogService)
        {
            _createReservoirUseCase = useCase;
            _dialogService = dialogService;

            UpdateCRT();
            CreateWTankCommand = new RelayCommand(CreateWaterTank);
        }
        #endregion

        #region Methods
        private void UpdateCRT()
        {
            if (_q > 0)
                CRT = (W / 1000.0 * L / 1000.0 * He * N) / Q * 24;
        }

        private void CreateWaterTank(object? obj)
        {
            // He(m)는 HWL-LWL 계산값; 나머지 치수는 mm 단위로 DTO에 직접 전달
            var designConditionDto = new ReservoirDesignConditionDto(Q, RT, N, LWL);
            var tankDto    = new ReservoirTankDto(He, Hf, Hm, W, L, M1, M2, M3, M4, Wh, Lh, Hh, Ltt);
            var valveDto   = new ReservoirValveDto(H1F, Lv, Wv, Lvt, We, TrOff, Wp, Hp, WpThk, SpThk, SLp);
            var thicknessDto = new ReservoirTypeThicknessDto(
                StuThk, StbThk, SvuThk, SvmThk, SvbThk,
                WteThk, WtiThk, WhThk, WveThk, WviThk, LcThk,
                Cw, Cd, Gw, Gh);

            var requestDto = new ReservoirCreationRequestDto(designConditionDto, tankDto, valveDto, thicknessDto);
            _createReservoirUseCase.Execute(requestDto);
        }
        #endregion
    }
}
