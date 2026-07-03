using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Collections.ObjectModel;
using System.Globalization;
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
        private readonly IMeasurePickService? _measure;
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
        /// 산출식 - 변경 시 변수목록 자동 동기화 + 미리보기 갱신
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
        public ObservableCollection<VariableInput> VariableInputs { get; } = [];
        public bool HasVariables => VariableInputs.Any();

        public string Preview
        {
            get => _preview;
            private set => SetProperty(ref _preview, value);
        }

        public double BaseValue { get; set; }
        public double FinalValue => BaseValue - Deductions.Sum(d => d.Area);
        public ObservableCollection<DeductionItem> Deductions { get; } = [];

        public QuantityItem? ResultItem { get; private set; }
        public event Action<bool>? CloseRequested;
        #endregion

        #region Commands
        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand MeasureLengthCommand { get; }
        public ICommand MeasureAreaCommand { get; }
        #endregion

        private static readonly Regex _varRegex =
            new(@"\b([A-Za-z][A-Za-z0-9_]*)\b", RegexOptions.Compiled);
        private static readonly HashSet<string> _keywords =
            new(StringComparer.OrdinalIgnoreCase) { "x", "PI" };

        #region Constructor
        public ManualQuantityViewModel(
            QuantityInputMode mode = QuantityInputMode.New,
            QuantityItem? sourceItem = null,
            IMeasurePickService? measureService = null)
        {
            Mode = mode;
            _measure = measureService;
            ConfirmCommand = new RelayCommand(_ => OnConfirm(), _ => CanConfirm());
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(false));
            MeasureLengthCommand = new RelayCommand(p => _ = MeasureAsync(p, MeasureKind.Length), _ => _measure != null);
            MeasureAreaCommand = new RelayCommand(p => _ = MeasureAsync(p, MeasureKind.Area), _ => _measure != null);

            if (mode == QuantityInputMode.Edit && sourceItem != null)
            {
                InitializeFromItem(sourceItem);
            }
        }
        #endregion

        private void SyncVariables()
        {
            var names = _varRegex.Matches(RawFormula)
                .Select(m => m.Groups[1].Value)
                .Where(v => !_keywords.Contains(v))
                .Distinct()
                .ToList();

            foreach (var stale in VariableInputs.Where(v => !names.Contains(v.Name)).ToList())
            {
                stale.PropertyChanged -= OnVariableChanged;
                VariableInputs.Remove(stale);
            }

            foreach (var name in names.Where(n => VariableInputs.All(v => v.Name != n)))
            {
                var item = new VariableInput { Name = name };
                item.PropertyChanged += OnVariableChanged;
                VariableInputs.Add(item);
            }

            for (int i = 0; i < names.Count; i++)
            {
                var item = VariableInputs.First(v => v.Name == names[i]);
                int cur = VariableInputs.IndexOf(item);
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

        private async Task MeasureAsync(object? param, MeasureKind kind)
        {
            if (_measure == null || param is not VariableInput target) return;
            if (target.IsMeasuring) return;

            try
            {
                target.IsMeasuring = true;
                var result = await _measure.PickAsync(kind);
                if (result == null) return;

                target.Value = result.Value.ToString("F3", CultureInfo.InvariantCulture);
                target.Unit = result.Unit;
                UpdatePreview();
            }
            finally
            {
                target.IsMeasuring = false;
            }
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
                var dict = VariableInputs
                    .GroupBy(v => v.Name)
                    .ToDictionary(g => g.Key, g => double.TryParse(g.First().Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0.0);
                var rendered = FormulaCalculator.Render(RawFormula, dict);
                var value = FormulaCalculator.Calculate(RawFormula, dict);
                Preview = $"{rendered}  =  {value:F3} {Unit}";
            }
            catch
            {
                Preview = "계산 오류 - 산출식을 확인하세요";
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
                var dict = VariableInputs
                    .GroupBy(v => v.Name)
                    .ToDictionary(g => g.Key, g => double.TryParse(g.First().Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0.0);
                ResultItem = new QuantityItem
                {
                    ElementId = _originalElementId,
                    WorkType = WorkType.Trim(),
                    Unit = Unit,
                    Category = Category.Trim(),
                    ElementCode = ElementCode.Trim(),
                    Specification = Specification.Trim(),
                    SubSpecification = SubSpecification.Trim(),
                    RawFormula = RawFormula.Trim(),
                    RenderedFormula = FormulaCalculator.Render(RawFormula, dict),
                    Value = FormulaCalculator.Calculate(RawFormula, dict),
                    Status = QuantityStatus.Manual,
                };
                CloseRequested?.Invoke(true);
            }
            catch
            {
                // 계산 오류 시 확인 불가 - Preview에 오류 메시지가 표시되므로 사용자가 인지함
            }
        }

        private void InitializeFromItem(QuantityItem item)
        {
            _originalElementId = item.ElementId;

            WorkType = item.WorkType;
            Unit = item.Unit;
            Category = item.Category;
            ElementCode = item.ElementCode;
            Specification = item.Specification;
            SubSpecification = item.SubSpecification;
            RawFormula = item.RawFormula;

            var variableValues = ParseRenderedFormula(item.RenderedFormula);

            foreach (var varInput in VariableInputs)
            {
                if (variableValues.TryGetValue(varInput.Name, out var value))
                {
                    varInput.Value = value.ToString(CultureInfo.InvariantCulture);
                }
            }

            UpdatePreview();
        }

        private Dictionary<string, double> ParseRenderedFormula(string rendered)
        {
            var result = new Dictionary<string, double>();
            var regex = new Regex(@"(-?[\d.]+)\s*\(([A-Za-z][A-Za-z0-9_]*)(?:[^)]*)\)", RegexOptions.Compiled);
            var matches = regex.Matches(rendered);

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    var valueStr = match.Groups[1].Value;
                    var varName = match.Groups[2].Value;

                    if (double.TryParse(valueStr, out var value))
                    {
                        result[varName] = value;
                    }
                }
            }

            return result;
        }
    }

    public class VariableInput : ViewModelBase
    {
        private string _name = string.Empty;
        private string _value = "0";
        private string _unit = string.Empty;
        private bool _isMeasuring;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }
        public bool IsMeasuring
        {
            get => _isMeasuring;
            set => SetProperty(ref _isMeasuring, value);
        }
    }

    public record DeductionItem
    {
        public string Description { get; init; } = string.Empty;
        public double Area { get; init; }
    }
}
