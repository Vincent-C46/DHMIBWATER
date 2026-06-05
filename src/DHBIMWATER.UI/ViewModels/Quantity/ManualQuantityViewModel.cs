using DHBIMWATER.Core.Quantity;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Quantity
{
    public enum QuantityInputMode { New, Edit }

    public class ManualQuantityViewModel : ViewModelBase
    {
        public QuantityInputMode Mode { get; }
        public bool IsEditMode => Mode == QuantityInputMode.Edit;
        public string Title => IsEditMode ? "수량 항목 수정" : "수량 항목 추가";
        public string ConfirmButtonText => IsEditMode ? "저장" : "추가";

        #region Fields
        private string _workType = string.Empty;
        private string _unit = "m³";
        private string _rawFormula = string.Empty;
        private string _category = string.Empty;
        private string _elementCode = string.Empty;
        private string _specification = string.Empty;
        private string _subSpecification = string.Empty;
        private string _preview = string.Empty;
        private long _originalElementId = -1; // Edit 모드에서는 원본 ElementId 유지
        #endregion

        #region Properties
        public List<string> UnitOptions { get; } = ["EA", "m", "m²", "m³", "공m³", "ton"];
        public string WorkType
        {
            get => _workType;
            set => SetProperty(ref _workType, value);
        }
        public string Unit
        {
            get => _unit;
            set { SetProperty(ref _unit, value); UpdatePreview(); }
        }
        /// <summary>
        /// 산출식 — 변경 시 변수목록 자동 동기화 + 미리보기 갱신
        /// </summary>
        public string RawFormula
        {
            get => _rawFormula;
            set { SetProperty(ref _rawFormula, value); SyncVariables(); UpdatePreview(); }
        }
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }
        public string ElementCode
        {
            get => _elementCode;
            set => SetProperty(ref _elementCode, value);
        }
        public string Specification
        {
            get => _specification;
            set => SetProperty(ref _specification, value);
        }
        public string SubSpecification
        {
            get => _subSpecification;
            set => SetProperty(ref _subSpecification, value);
        }
        // ── Variables ─────────────────────────────────────────────────────────
        public ObservableCollection<VariableInput> VariableInputs { get; } = [];
        public bool HasVariables => VariableInputs.Any();

        // ── Preview ───────────────────────────────────────────────────────────
        public string Preview
        {
            get => _preview;
            private set => SetProperty(ref _preview, value);
        }

        // ── Edit mode ─────────────────────────────────────────────────────────
        public double BaseValue { get; set; }
        public double FinalValue => BaseValue - Deductions.Sum(d => d.Area);
        public ObservableCollection<DeductionItem> Deductions { get; } = [];

        // ── Result / Close ────────────────────────────────────────────────────
        public QuantityItem? ResultItem { get; private set; }
        public event Action<bool>? CloseRequested;
        #endregion

        #region Commands
        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        // ── Regex ─────────────────────────────────────────────────────────────
        // 변수명: 영문자로 시작, 영문자/숫자/언더바 조합 (예: A, B_1, Width_Max)
        private static readonly Regex _varRegex =
            new(@"\b([A-Za-z][A-Za-z0-9_]*)\b", RegexOptions.Compiled);
        private static readonly HashSet<string> _keywords =
            new(StringComparer.OrdinalIgnoreCase) { "x", "PI" };

        #region Constructor
        public ManualQuantityViewModel(QuantityInputMode mode = QuantityInputMode.New, QuantityItem? sourceItem = null)
        {
            Mode = mode;
            ConfirmCommand = new RelayCommand(_ => OnConfirm(), _ => CanConfirm());
            CancelCommand  = new RelayCommand(_ => CloseRequested?.Invoke(false));

            // Edit 모드: 기존 항목 데이터로 초기화
            if (mode == QuantityInputMode.Edit && sourceItem != null)
            {
                InitializeFromItem(sourceItem);
            }
        }
        #endregion

        // ── Variable sync ─────────────────────────────────────────────────────
        private void SyncVariables()
        {
            // 식에서 변수명 추출 (등장 순서 유지, 키워드 제외)
            var names = _varRegex.Matches(RawFormula)
                .Select(m => m.Groups[1].Value)
                .Where(v => !_keywords.Contains(v))
                .Distinct()
                .ToList();

            // 식에 더 이상 없는 변수 제거
            foreach (var stale in VariableInputs.Where(v => !names.Contains(v.Name)).ToList())
            {
                stale.PropertyChanged -= OnVariableChanged;
                VariableInputs.Remove(stale);
            }

            // 새로 생긴 변수 추가
            foreach (var name in names.Where(n => VariableInputs.All(v => v.Name != n)))
            {
                var item = new VariableInput { Name = name };
                item.PropertyChanged += OnVariableChanged;
                VariableInputs.Add(item);
            }

            // 식 등장 순서로 정렬
            for (int i = 0; i < names.Count; i++)
            {
                var item = VariableInputs.First(v => v.Name == names[i]);
                int cur  = VariableInputs.IndexOf(item);
                if (cur != i) VariableInputs.Move(cur, i);
            }

            OnPropertyChanged(nameof(HasVariables));
        }

        private void OnVariableChanged(object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VariableInput.Value))
                UpdatePreview();
        }
        private void UpdatePreview()
        {
            if (string.IsNullOrWhiteSpace(RawFormula) || !VariableInputs.Any())
            {
                Preview = string.Empty;
                return;
            }
            try
            {
                var dict     = VariableInputs.ToDictionary(v => v.Name, v => v.Value);
                var rendered = FormulaCalculator.Render(RawFormula, dict);
                var value    = FormulaCalculator.Calculate(RawFormula, dict);
                Preview = $"{rendered}  =  {value:F3} {Unit}";
            }
            catch
            {
                Preview = "계산 오류 — 산출식을 확인하세요";
            }
        }

        private bool CanConfirm()
            => !string.IsNullOrWhiteSpace(WorkType)
            && !string.IsNullOrWhiteSpace(RawFormula)
            && !string.IsNullOrWhiteSpace(Unit);

        private void OnConfirm()
        {
            try
            {
                var dict = VariableInputs.ToDictionary(v => v.Name, v => v.Value);
                ResultItem = new QuantityItem
                {
                    ElementId        = _originalElementId, // Edit 모드면 원본 ID 유지
                    WorkType         = WorkType.Trim(),
                    Unit             = Unit,
                    Category         = Category.Trim(),
                    ElementCode      = ElementCode.Trim(),
                    Specification    = Specification.Trim(),
                    SubSpecification = SubSpecification.Trim(),
                    RawFormula       = RawFormula.Trim(),
                    RenderedFormula  = FormulaCalculator.Render(RawFormula, dict),
                    Value            = FormulaCalculator.Calculate(RawFormula, dict),
                    Status           = QuantityStatus.Manual,
                };
                CloseRequested?.Invoke(true);
            }
            catch
            {
                // 계산 오류 시 확인 불가 — Preview에 오류 메시지가 표시되므로 사용자가 인지함
            }
        }

        /// <summary>
        /// Edit 모드: QuantityItem으로부터 ViewModel 초기화
        /// RenderedFormula 파싱: "0.5(A) x 10.0(L)" → RawFormula="A x L", A=0.5, L=10.0
        /// </summary>
        private void InitializeFromItem(QuantityItem item)
        {
            _originalElementId = item.ElementId; // ElementId 저장
            
            WorkType         = item.WorkType;
            Unit             = item.Unit;
            Category         = item.Category;
            ElementCode      = item.ElementCode;
            Specification    = item.Specification;
            SubSpecification = item.SubSpecification;
            
            // RawFormula 설정
            RawFormula = item.RawFormula;
            
            // RenderedFormula 파싱해서 변수 값 추출
            // 예: "0.6(B) × 0.7(D) × 10.0(L)" → B=0.6, D=0.7, L=10.0
            var variableValues = ParseRenderedFormula(item.RenderedFormula);
            
            // 변수 값 설정 (SyncVariables는 이미 RawFormula setter에서 호출됨)
            foreach (var varInput in VariableInputs)
            {
                if (variableValues.TryGetValue(varInput.Name, out var value))
                {
                    varInput.Value = value;
                }
            }
            
            // 변수 값 설정 후 미리보기 갱신
            UpdatePreview();
        }

        /// <summary>
        /// RenderedFormula를 파싱하여 변수명과 값 추출
        /// "0.5(A) x 10.0(L)" → { "A": 0.5, "L": 10.0 }
        /// "1.5(A_1) x 2.0(B_2)" → { "A_1": 1.5, "B_2": 2.0 }
        /// </summary>
        private Dictionary<string, double> ParseRenderedFormula(string rendered)
        {
            var result = new Dictionary<string, double>();
            
            // 패턴: 숫자(변수명) 형태 추출
            // 변수명: 영문자로 시작, 영문자/숫자/언더바 조합 가능
            // 예: 0.5(A), 10.0(L_1), 123.45(Width_Max)
            var regex = new Regex(@"([\d.]+)\s*\(([A-Za-z][A-Za-z0-9_]*)\)", RegexOptions.Compiled);
            var matches = regex.Matches(rendered);
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    var valueStr = match.Groups[1].Value;
                    var varName  = match.Groups[2].Value;
                    
                    if (double.TryParse(valueStr, out var value))
                    {
                        result[varName] = value;
                    }
                }
            }
            
            return result;
        }
    }

    // ── VariableInput ─────────────────────────────────────────────────────────
    // record → ViewModelBase 클래스로 변경:
    // Value 변경 시 PropertyChanged를 발생시켜야 ManualQuantityViewModel이
    // 미리보기를 즉시 갱신할 수 있다.
    public class VariableInput : ViewModelBase
    {
        private string _name  = string.Empty;
        private double _value;
        private string _unit  = string.Empty;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public double Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }
    }

    // ── DeductionItem (Edit 모드 전용, 추후 구현) ─────────────────────────────
    public record DeductionItem
    {
        public string Description { get; init; } = string.Empty;
        public double Area        { get; init; }
    }
}
