using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases.AutoGenerator;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling
{
    public class WaterTankViewModel : ViewModelBase
    {
        #region Fields
        private readonly IDialogService _dialogService;
        private readonly CreateReservoirUseCase _createReservoirUseCase;
        private readonly IElementTypeQueryRepo _typeQueryRepo;
        private readonly IFileDialogService _fileDialogService;

        // 설계조건
        private double _q   = 5000;
        private double _rt  = 12;
        private int    _n   = 2;
        private double _lwl = 0;
        private double _hwl = 4.5;     // HWL (m)

        // 수조부 — 입력 단위: m (Hf/Hm 제외), DTO 전달 시 mm 환산
        private double _hf  = 300;     // 여유고 (mm)
        private double _hm  = 150;     // 바닥~LWL (mm)
        private double _w   = 25;      // 지 폭 (m)
        private double _l   = 30;      // 지 길이 (m)
        private double _m1  = 4;
        private double _m2  = 4;
        private double _m3  = 4;
        private double _m4  = 4;
        private double _wh  = 2.5;    // Hopper 폭 (m)
        private double _lh  = 2.5;    // Hopper 길이 (m)
        private double _hh  = 2;      // Hopper 깊이 (m)
        private double _ltt = 0;       // 기초 Toe (m)
        private double _slv = 1.0;     // 사면경사 (무차원)
        private double _crt;

        // 밸브실 — 입력 단위: m (TrOff/WpThk/SpThk/SLp 제외), DTO 전달 시 mm 환산
        private double _h1f   = 4;
        private double _lv    = 30;
        private double _wv    = 5;
        private double _lvt   = 0;
        private double _we    = 2.5;
        private double _trOff = 100;
        private double _wp    = 1;
        private double _hp    = 1;
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
        private string _columnTypeName = string.Empty;
        private string _beamTypeName   = string.Empty;
        #endregion

        #region Properties
        public ICommand CreateWTankCommand { get; }
        public ICommand ExportSettingCommand { get; }
        public ICommand ImportSettingCommand { get; }
        public Action? CloseAction { get; set; }

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
        /// <summary>밸브실 2F 순높이 (m) = Hh + He + Hf/1000 + Hm/1000 - H1F</summary>
        public double H2F => Hh + He + Hf / 1000.0 + Hm / 1000.0 - H1F;

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

        public IReadOnlyList<string> ColumnTypeNames { get; private set; } = new List<string>();
        public IReadOnlyList<string> BeamTypeNames   { get; private set; } = new List<string>();

        public string ColumnTypeName
        {
            get => _columnTypeName;
            set { if (_columnTypeName != value) { _columnTypeName = value; OnPropertyChanged(nameof(ColumnTypeName)); } }
        }
        public string BeamTypeName
        {
            get => _beamTypeName;
            set { if (_beamTypeName != value) { _beamTypeName = value; OnPropertyChanged(nameof(BeamTypeName)); } }
        }
        #endregion

        #region Constructor
        public WaterTankViewModel(CreateReservoirUseCase useCase, IDialogService dialogService, IElementTypeQueryRepo typeQueryRepo, IFileDialogService fileDialogService)
        {
            _createReservoirUseCase = useCase;
            _dialogService          = dialogService;
            _typeQueryRepo          = typeQueryRepo;
            _fileDialogService      = fileDialogService;

            LoadTypeNames();
            UpdateCRT();
            CreateWTankCommand = new RelayCommand(CreateWaterTank);
            ExportSettingCommand = new RelayCommand(ExportSetting);
            ImportSettingCommand = new RelayCommand(ImportSetting);
        }

        private void LoadTypeNames()
        {
            ColumnTypeNames  = _typeQueryRepo.GetColumnTypeNames().ToList();
            BeamTypeNames    = _typeQueryRepo.GetBeamTypeNames().ToList();
            _columnTypeName  = ColumnTypeNames.FirstOrDefault() ?? string.Empty;
            _beamTypeName    = BeamTypeNames.FirstOrDefault()   ?? string.Empty;
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
            var requestDto = BuildCreationRequestDto();
            _createReservoirUseCase.Execute(requestDto);
            CloseAction?.Invoke();
        }
        private ReservoirCreationRequestDto BuildCreationRequestDto()
        {
            static double MToMm(double value) => value * 1000.0;

            var designConditionDto = new ReservoirDesignConditionDto(Q, RT, N, LWL);
            var tankDto = new ReservoirTankDto(
                He, Hf, Hm,
                MToMm(W), MToMm(L), MToMm(M1), MToMm(M2), MToMm(M3), MToMm(M4),
                MToMm(Wh), MToMm(Lh), MToMm(Hh), MToMm(Ltt));
            var valveDto = new ReservoirValveDto(
                MToMm(H1F), MToMm(Lv), MToMm(Wv), MToMm(Lvt), MToMm(We),
                TrOff, MToMm(Wp), MToMm(Hp), WpThk, SpThk, SLp, SLv);
            var thicknessDto = new ReservoirTypeThicknessDto(
                StuThk, StbThk, SvuThk, SvmThk, SvbThk,
                WteThk, WtiThk, WhThk, WveThk, WviThk, LcThk,
                ColumnTypeName, BeamTypeName);

            return new ReservoirCreationRequestDto(designConditionDto, tankDto, valveDto, thicknessDto);
        }
        private void ExportSetting(object? obj)
        {
            var path = _fileDialogService.SaveFile("배수지 설정 내보내기", "CSV 파일 (*.csv)|*.csv", "ReservoirSettings.csv");
            if (path == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("매개변수명,설명,값,단위,비고");
            foreach (var row in BuildSettingRows())
                sb.AppendLine(string.Join(",", EscapeCsv(row.Code), EscapeCsv(row.Description), EscapeCsv(row.Value), EscapeCsv(row.Unit), EscapeCsv(row.Note)));

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            _dialogService.Info("내보내기", "CSV 파일이 저장되었습니다.");
        }

        private void ImportSetting(object? obj)
        {
            var path = _fileDialogService.OpenFile("배수지 설정 가져오기", "CSV 파일 (*.csv)|*.csv");
            if (path == null) return;

            try
            {
                var lines = File.ReadAllLines(path, Encoding.UTF8);
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var cells = line.Contains('\t') ? line.Split('\t').ToList() : ParseCsvLine(line);
                    if (cells.Count >= 3 && !string.IsNullOrWhiteSpace(cells[0]))
                        values[cells[0].Trim()] = cells[2].Trim();
                }

                var errors = new List<string>();
                SetDouble(values, "Q", v => Q = v, errors);
                SetDouble(values, "RT", v => RT = v, errors);
                SetInt(values, "N", v => N = v, errors);
                SetDouble(values, "LWL", v => LWL = v, errors);
                SetDouble(values, "HWL", v => HWL = v, errors);
                SetDouble(values, "Hf", v => Hf = v, errors);
                SetDouble(values, "Hm", v => Hm = v, errors);
                SetDouble(values, "W", v => W = v, errors);
                SetDouble(values, "L", v => L = v, errors);
                SetDouble(values, "M1", v => M1 = v, errors);
                SetDouble(values, "M2", v => M2 = v, errors);
                SetDouble(values, "M3", v => M3 = v, errors);
                SetDouble(values, "M4", v => M4 = v, errors);
                SetDouble(values, "Wh", v => Wh = v, errors);
                SetDouble(values, "Lh", v => Lh = v, errors);
                SetDouble(values, "Hh", v => Hh = v, errors);
                SetDouble(values, "Ltt", v => Ltt = v, errors);
                SetDouble(values, "SLv", v => SLv = v, errors);
                SetDouble(values, "H1F", v => H1F = v, errors);
                SetDouble(values, "Lv", v => Lv = v, errors);
                SetDouble(values, "Wv", v => Wv = v, errors);
                SetDouble(values, "Lvt", v => Lvt = v, errors);
                SetDouble(values, "We", v => We = v, errors);
                SetDouble(values, "TrOff", v => TrOff = v, errors);
                SetDouble(values, "Wp", v => Wp = v, errors);
                SetDouble(values, "Hp", v => Hp = v, errors);
                SetDouble(values, "WpThk", v => WpThk = v, errors);
                SetDouble(values, "SpThk", v => SpThk = v, errors);
                SetDouble(values, "SLp", v => SLp = v, errors);
                SetDouble(values, "StuThk", v => StuThk = v, errors);
                SetDouble(values, "StbThk", v => StbThk = v, errors);
                SetDouble(values, "SvuThk", v => SvuThk = v, errors);
                SetDouble(values, "SvmThk", v => SvmThk = v, errors);
                SetDouble(values, "SvbThk", v => SvbThk = v, errors);
                SetDouble(values, "WteThk", v => WteThk = v, errors);
                SetDouble(values, "WtiThk", v => WtiThk = v, errors);
                SetDouble(values, "WhThk", v => WhThk = v, errors);
                SetDouble(values, "WveThk", v => WveThk = v, errors);
                SetDouble(values, "WviThk", v => WviThk = v, errors);
                SetDouble(values, "LcThk", v => LcThk = v, errors);

                if (values.TryGetValue("ColumnType", out var columnType)) ColumnTypeName = columnType;
                else errors.Add("ColumnType 누락");
                if (values.TryGetValue("BeamType", out var beamType)) BeamTypeName = beamType;
                else errors.Add("BeamType 누락");

                if (errors.Count > 0)
                    _dialogService.Warn("가져오기", string.Join(Environment.NewLine, errors));
                else
                    _dialogService.Info("가져오기", "CSV 파일을 가져왔습니다.");
            }
            catch (Exception ex)
            {
                _dialogService.Warn("가져오기", $"CSV 파일을 읽을 수 없습니다: {ex.Message}");
            }
        }

        private IEnumerable<(string Code, string Description, string Value, string Unit, string Note)> BuildSettingRows()
        {
            static string D(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
            return new List<(string, string, string, string, string)>
            {
                ("Q", "계획 1일 최대급수량", D(Q), "㎥", string.Empty),
                ("RT", "최소 체류시간", D(RT), "hr", string.Empty),
                ("N", "지 개수", N.ToString(CultureInfo.InvariantCulture), "EA", string.Empty),
                ("LWL", "저수위(EL)", D(LWL), "m", string.Empty),
                ("HWL", "고수위(EL)", D(HWL), "m", string.Empty),
                ("Hf", "여유고", D(Hf), "mm", string.Empty),
                ("Hm", "바닥~LWL(사수심)", D(Hm), "mm", string.Empty),
                ("W", "지 폭", D(W), "m", string.Empty),
                ("L", "지 길이", D(L), "m", string.Empty),
                ("M1", "기둥 Margin(상)", D(M1), "m", string.Empty),
                ("M2", "기둥 Margin(하)", D(M2), "m", string.Empty),
                ("M3", "기둥 Margin(좌)", D(M3), "m", string.Empty),
                ("M4", "기둥 Margin(우)", D(M4), "m", string.Empty),
                ("Wh", "Hopper 폭", D(Wh), "m", string.Empty),
                ("Lh", "Hopper 길이", D(Lh), "m", string.Empty),
                ("Hh", "Hopper 깊이", D(Hh), "m", string.Empty),
                ("Ltt", "수조부 기초 Toe", D(Ltt), "m", string.Empty),
                ("SLv", "단차부 사면경사", D(SLv), "1:x", string.Empty),
                ("H1F", "배관실 1F 내부높이", D(H1F), "m", string.Empty),
                ("Lv", "배관실 길이", D(Lv), "m", string.Empty),
                ("Wv", "배관실 폭", D(Wv), "m", string.Empty),
                ("Lvt", "배관실 Toe", D(Lvt), "m", string.Empty),
                ("We", "전기실 폭", D(We), "m", string.Empty),
                ("TrOff", "트렌치 이격", D(TrOff), "mm", string.Empty),
                ("Wp", "PIT 폭/길이", D(Wp), "m", string.Empty),
                ("Hp", "PIT 깊이", D(Hp), "m", string.Empty),
                ("WpThk", "PIT 벽체 두께", D(WpThk), "mm", string.Empty),
                ("SpThk", "PIT 하부기초 두께", D(SpThk), "mm", string.Empty),
                ("SLp", "PIT 사면경사", D(SLp), "1:x", string.Empty),
                ("StuThk", "수조부 상부슬래브 두께", D(StuThk), "mm", string.Empty),
                ("StbThk", "수조부 하부슬래브 두께", D(StbThk), "mm", string.Empty),
                ("SvuThk", "밸브실 상부슬래브 두께", D(SvuThk), "mm", string.Empty),
                ("SvmThk", "밸브실 중간슬래브 두께", D(SvmThk), "mm", string.Empty),
                ("SvbThk", "밸브실 하부슬래브 두께", D(SvbThk), "mm", string.Empty),
                ("WteThk", "수조부 외벽 두께", D(WteThk), "mm", string.Empty),
                ("WtiThk", "수조부 내벽 두께", D(WtiThk), "mm", string.Empty),
                ("WhThk", "Hopper 벽체 두께", D(WhThk), "mm", string.Empty),
                ("WveThk", "밸브실 외벽 두께", D(WveThk), "mm", string.Empty),
                ("WviThk", "밸브실 내벽 두께", D(WviThk), "mm", string.Empty),
                ("LcThk", "버림콘크리트 두께", D(LcThk), "mm", string.Empty),
                ("ColumnType", "기둥 유형명", ColumnTypeName, "", "문자열"),
                ("BeamType", "보 유형명", BeamTypeName, "", "문자열"),
            };
        }
        private static void SetDouble(Dictionary<string, string> values, string code, Action<double> setValue, List<string> errors)
        {
            if (!values.TryGetValue(code, out var text))
            {
                errors.Add($"{code} 누락");
                return;
            }
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                setValue(value);
                return;
            }
            errors.Add($"{code} 값 파싱 실패: {text}");
        }

        private static void SetInt(Dictionary<string, string> values, string code, Action<int> setValue, List<string> errors)
        {
            if (!values.TryGetValue(code, out var text))
            {
                errors.Add($"{code} 누락");
                return;
            }

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ||
                int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
            {
                setValue(value);
                return;
            }

            errors.Add($"{code} 값 파싱 실패: {text}");
        }

        private static string EscapeCsv(string value)
        {
            if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
                return value;
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private static List<string> ParseCsvLine(string line)
        {
            var cells = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    cells.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            cells.Add(sb.ToString());
            return cells;
        }
        #endregion
    }
}







