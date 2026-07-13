using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Core.Settings;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Quantity
{
    /// <summary>
    /// 수량산출 설정창 ViewModel — 거푸집 / 면적 공제 / 철근비 / 할증률 (탭 4개).
    /// 실제 저장(DataStorage)·.dhcfg 입출력은 호스트가 이벤트로 처리한다 (UI 셸 + 콜백).
    /// 목업: docs/07_수량설정창목업.html
    /// </summary>
    public class QuantitySettingsViewModel : ViewModelBase
    {
        private readonly ProjectSettings _settings;

        public QuantitySettingsViewModel(ProjectSettings settings)
        {
            _settings = settings ?? new ProjectSettings();

            BuildFormworkRows();
            BuildDeductionRows();
            BuildRebarRows();
            BuildLossRows();

            SaveCommand = new RelayCommand(_ => OnSave());
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(false));
            ImportCommand = new RelayCommand(_ => ImportRequested?.Invoke());
            ExportCommand = new RelayCommand(_ => { ApplyToSettings(); ExportRequested?.Invoke(_settings); });
        }

        #region 거푸집
        public ObservableCollection<FormworkRowVm> FormworkRows { get; } = [];

        private void BuildFormworkRows()
        {
            var w = _settings.Formwork.Walls;
            var c = _settings.Formwork.Columns;
            var b = _settings.Formwork.Beams;
            var f = _settings.Formwork.Floors;
            var fo = _settings.Formwork.Foundation;

            // Member 라벨은 각 부재 첫 행에만 표시 (목업의 rowspan 대응)
            FormworkRows.Add(new FormworkRowVm("벽", "외벽면", () => w.Exterior, v => w.Exterior = v));
            FormworkRows.Add(new FormworkRowVm("", "내벽면", () => w.Interior, v => w.Interior = v));
            FormworkRows.Add(new FormworkRowVm("", "마구리", () => w.End, v => w.End = v));

            FormworkRows.Add(new FormworkRowVm("기둥", "옆면", () => c.Side, v => c.Side = v));

            FormworkRows.Add(new FormworkRowVm("보", "하부면", () => b.Bottom, v => b.Bottom = v));
            FormworkRows.Add(new FormworkRowVm("", "옆면", () => b.Side, v => b.Side = v));
            FormworkRows.Add(new FormworkRowVm("", "마구리", () => b.End, v => b.End = v));

            FormworkRows.Add(new FormworkRowVm("슬래브", "하부면", () => f.Bottom, v => f.Bottom = v));
            FormworkRows.Add(new FormworkRowVm("", "옆면 (철근)", () => f.SideRc, v => f.SideRc = v));
            FormworkRows.Add(new FormworkRowVm("", "옆면 (무근)", () => f.SidePlain, v => f.SidePlain = v));

            FormworkRows.Add(new FormworkRowVm("기초", "옆면 (철근)", () => fo.SideRc, v => fo.SideRc = v));
            FormworkRows.Add(new FormworkRowVm("", "옆면 (무근)", () => fo.SidePlain, v => fo.SidePlain = v));
        }
        #endregion

        #region 면적 공제
        // 매트릭스 열 순서: 벽 / 슬래브 / 기둥 / 보 / 기초
        private static readonly RevitCategory[] AdjacentColumns =
        {
            RevitCategory.Walls, RevitCategory.Floors, RevitCategory.StructuralColumns,
            RevitCategory.StructuralFraming, RevitCategory.StructuralFoundation
        };

        public ObservableCollection<DeductionRowVm> DeductionRows { get; } = [];

        private bool _useOpeningMinVolume = true;
        public bool UseOpeningMinVolume
        {
            get => _useOpeningMinVolume;
            set => SetProperty(ref _useOpeningMinVolume, value);
        }

        private double _openingMinVolumeM3 = 1.0;
        public double OpeningMinVolumeM3
        {
            get => _openingMinVolumeM3;
            set => SetProperty(ref _openingMinVolumeM3, value);
        }

        private void BuildDeductionRows()
        {
            var d = _settings.Deduction;
            UseOpeningMinVolume = d.UseOpeningMinVolume;
            OpeningMinVolumeM3 = d.OpeningMinVolumeM3;

            // 행: 벽 / 슬래브 / 기둥 / 보 / 계단
            AddDeductionRow("벽", RevitCategory.Walls);
            AddDeductionRow("슬래브", RevitCategory.Floors);
            AddDeductionRow("기둥", RevitCategory.StructuralColumns);
            AddDeductionRow("보", RevitCategory.StructuralFraming);
            AddDeductionRow("계단", RevitCategory.Stairs);
        }

        private void AddDeductionRow(string label, RevitCategory host)
        {
            _settings.Deduction.CategoryMatrix.TryGetValue(host, out var adjacent);
            adjacent ??= new List<RevitCategory>();
            DeductionRows.Add(new DeductionRowVm(label, host, AdjacentColumns, adjacent));
        }
        #endregion

        #region 철근비
        private bool _rebarEnabled = true;
        public bool RebarEnabled
        {
            get => _rebarEnabled;
            set => SetProperty(ref _rebarEnabled, value);
        }

        public ObservableCollection<RebarRatioRowVm> RebarRows { get; } = [];

        private void BuildRebarRows()
        {
            var r = _settings.RebarRatio;
            RebarEnabled = r.Enabled;

            AddRebarRow("기초", RevitCategory.StructuralFoundation);
            AddRebarRow("슬래브", RevitCategory.Floors);
            AddRebarRow("벽", RevitCategory.Walls);
            AddRebarRow("보", RevitCategory.StructuralFraming);
            AddRebarRow("기둥", RevitCategory.StructuralColumns);
        }

        private void AddRebarRow(string label, RevitCategory category)
        {
            _settings.RebarRatio.KgPerM3.TryGetValue(category, out var kg);
            RebarRows.Add(new RebarRatioRowVm(label, category, kg));
        }
        #endregion

        #region 할증률
        private bool _lossEnabled = true;
        public bool LossEnabled
        {
            get => _lossEnabled;
            set => SetProperty(ref _lossEnabled, value);
        }

        public ObservableCollection<LossRateRowVm> LossRows { get; } = [];

        private void BuildLossRows()
        {
            var l = _settings.LossRate;
            LossEnabled = l.Enabled;

            AddLossRow("철근콘크리트", "레미콘");
            AddLossRow("무근콘크리트", "레미콘");
            AddLossRow("철근", "이형철근, 직경 구분 없이 일괄 적용");
            AddLossRow("거푸집", "합판");
            AddLossRow("강재", "");
        }

        private void AddLossRow(string workType, string note)
        {
            _settings.LossRate.RatePercent.TryGetValue(workType, out var pct);
            LossRows.Add(new LossRateRowVm(workType, note, pct));
        }
        #endregion

        #region Commands / Events
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }

        /// <summary>창 닫기 요청 (true=저장 후 닫기, false=취소).</summary>
        public event Action<bool>? CloseRequested;
        /// <summary>저장 요청 — 호스트가 DataStorage 쓰기(ExternalEvent)를 담당.</summary>
        public event Action<ProjectSettings>? SaveRequested;
        /// <summary>.dhcfg 가져오기 요청 — 호스트가 파일 다이얼로그·재바인딩을 담당.</summary>
        public event Action? ImportRequested;
        /// <summary>.dhcfg 내보내기 요청 — 현재 VM 상태가 반영된 ProjectSettings 전달.</summary>
        public event Action<ProjectSettings>? ExportRequested;
        #endregion

        private void OnSave()
        {
            ApplyToSettings();
            SaveRequested?.Invoke(_settings);
            CloseRequested?.Invoke(true);
        }

        /// <summary>
        /// 각 탭 VM 상태를 주입받은 ProjectSettings에 반영한다.
        /// (거푸집 행은 write-through라 이미 반영됨 — 여기서는 나머지 탭 처리)
        /// </summary>
        private void ApplyToSettings()
        {
            var d = _settings.Deduction;
            d.UseOpeningMinVolume = UseOpeningMinVolume;
            d.OpeningMinVolumeM3 = OpeningMinVolumeM3;
            d.CategoryMatrix = DeductionRows.ToDictionary(r => r.Host, r => r.ToCheckedList());

            var r = _settings.RebarRatio;
            r.Enabled = RebarEnabled;
            r.KgPerM3 = RebarRows.ToDictionary(x => x.Category, x => x.KgPerM3);

            var l = _settings.LossRate;
            l.Enabled = LossEnabled;
            l.RatePercent = LossRows.ToDictionary(x => x.WorkType, x => x.Percent);
        }
    }

    /// <summary>거푸집 종류 ComboBox 선택지 (enum + 한글 표시명).</summary>
    public sealed class FormworkOption
    {
        public FormworkType Value { get; }
        public string Display { get; }

        public FormworkOption(FormworkType value)
        {
            Value = value;
            Display = value.ToSpecification();
        }

        public static IReadOnlyList<FormworkOption> All { get; } =
            Enum.GetValues(typeof(FormworkType)).Cast<FormworkType>()
                .Select(v => new FormworkOption(v)).ToList();
    }

    /// <summary>거푸집 탭 한 행 — 선택값은 주입된 FormworkSettings에 write-through.</summary>
    public sealed class FormworkRowVm : ViewModelBase
    {
        private readonly Func<FormworkType> _get;
        private readonly Action<FormworkType> _set;

        public FormworkRowVm(string member, string face, Func<FormworkType> get, Action<FormworkType> set)
        {
            Member = member;
            Face = face;
            _get = get;
            _set = set;
        }

        public string Member { get; }
        public string Face { get; }
        public IReadOnlyList<FormworkOption> Options => FormworkOption.All;

        public FormworkType Selected
        {
            get => _get();
            set
            {
                if (!EqualityComparer<FormworkType>.Default.Equals(_get(), value))
                {
                    _set(value);
                    OnPropertyChanged();
                }
            }
        }
    }

    /// <summary>면적 공제 매트릭스 한 행 (호스트 → 인접 카테고리 on/off).</summary>
    public sealed class DeductionRowVm : ViewModelBase
    {
        private readonly RevitCategory[] _columns;

        public DeductionRowVm(string label, RevitCategory host, RevitCategory[] columns, List<RevitCategory> checkedAdjacent)
        {
            Label = label;
            Host = host;
            _columns = columns;
            Wall = checkedAdjacent.Contains(RevitCategory.Walls);
            Floor = checkedAdjacent.Contains(RevitCategory.Floors);
            Column = checkedAdjacent.Contains(RevitCategory.StructuralColumns);
            Beam = checkedAdjacent.Contains(RevitCategory.StructuralFraming);
            Foundation = checkedAdjacent.Contains(RevitCategory.StructuralFoundation);
        }

        public string Label { get; }
        public RevitCategory Host { get; }

        private bool _wall, _floor, _column, _beam, _foundation;
        public bool Wall { get => _wall; set => SetProperty(ref _wall, value); }
        public bool Floor { get => _floor; set => SetProperty(ref _floor, value); }
        public bool Column { get => _column; set => SetProperty(ref _column, value); }
        public bool Beam { get => _beam; set => SetProperty(ref _beam, value); }
        public bool Foundation { get => _foundation; set => SetProperty(ref _foundation, value); }

        public List<RevitCategory> ToCheckedList()
        {
            var list = new List<RevitCategory>();
            if (Wall) list.Add(RevitCategory.Walls);
            if (Floor) list.Add(RevitCategory.Floors);
            if (Column) list.Add(RevitCategory.StructuralColumns);
            if (Beam) list.Add(RevitCategory.StructuralFraming);
            if (Foundation) list.Add(RevitCategory.StructuralFoundation);
            return list;
        }
    }

    /// <summary>철근비 탭 한 행 (카테고리별 kg/m³).</summary>
    public sealed class RebarRatioRowVm : ViewModelBase
    {
        public RebarRatioRowVm(string label, RevitCategory category, double kgPerM3)
        {
            Label = label;
            Category = category;
            _kgPerM3 = kgPerM3;
        }

        public string Label { get; }
        public RevitCategory Category { get; }

        private double _kgPerM3;
        public double KgPerM3 { get => _kgPerM3; set => SetProperty(ref _kgPerM3, value); }
    }

    /// <summary>할증률 탭 한 행 (공종별 %).</summary>
    public sealed class LossRateRowVm : ViewModelBase
    {
        public LossRateRowVm(string workType, string note, double percent)
        {
            WorkType = workType;
            Note = note;
            _percent = percent;
        }

        public string WorkType { get; }
        public string Note { get; }

        private double _percent;
        public double Percent { get => _percent; set => SetProperty(ref _percent, value); }
    }
}
