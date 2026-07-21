using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Application.UseCases.QuantityCalculator;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Quantity
{
    public class QuantityViewModel : ViewModelBase
    {
        #region Fields
        private IDialogService _dialogService;
        private IFileDialogService _fileDialogService;
        private readonly CalculateQuantityUseCase _calculateQuantityUseCase;
        private readonly ExportQuantityUseCase _exportQuantityUseCase;
        private Action? _extractAction;
        private Action<IList<long>>? _selectAction;
        private Action<IList<long>>? _visualizeAction;
        private bool _isSelectedInRevit;

        private List<QuantityItem> _currentSelectedItems = new();
        public ObservableCollection<QuantitySummaryItem> SummaryItems { get; set; }
        private ObservableCollection<GroupSummaryItem> _groupSummaries = new();
        public ObservableCollection<GroupSummaryItem> GroupSummaries
        {
            get => _groupSummaries;
            private set { _groupSummaries = value; OnPropertyChanged(); }
        }
        private QuantityItem? _selectedItem;
        private QuantitySummaryItem? _selectedSummaryItem;
        private int _selectedTabIndex;
        #endregion

        #region Properties
        public ObservableCollection<QuantityItem> QuantityItems { get; private set; } = new();
        public QuantityItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }
        public QuantitySummaryItem? SelectedSummaryItem
        {
            get => _selectedSummaryItem;
            set
            {
                if (_selectedSummaryItem != value)
                    _selectedSummaryItem = value;
                OnPropertyChanged();
            }
        }
        public int AutoCount => QuantityItems.Count(i => i.Status == QuantityStatus.Auto);
        public int ModifiedCount => QuantityItems.Count(i => i.Status == QuantityStatus.Modified);
        public int ManualCount => QuantityItems.Count(i => i.Status == QuantityStatus.Manual);
        public int TotalCount => QuantityItems.Count;
        public IMeasurePickService? MeasureService { get; private set; }

        public bool IsSelectedInRevit
        {
            get => _isSelectedInRevit;
            private set { _isSelectedInRevit = value; OnPropertyChanged(); }
        }

        private int _revitSelectedCount;
        public int RevitSelectedCount
        {
            get => _revitSelectedCount;
            private set { _revitSelectedCount = value; OnPropertyChanged(); }
        }
        private static readonly double[] TabWidths = { 980, 980, 1280 };
        public double WindowWidth => TabWidths[Math.Clamp(_selectedTabIndex, 0, TabWidths.Length - 1)];

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex == value) return;
                _selectedTabIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WindowWidth));
                SelectedItem = null;
                GroupSummaries = new ObservableCollection<GroupSummaryItem>();
                _currentSelectedItems.Clear();
                CommandManager.InvalidateRequerySuggested();
            }
        }
        #endregion

        #region Events
        public event EventHandler<QuantityItem?> ManualInputRequested = delegate { };
        public event EventHandler<(QuantityItem item, int index)> EditItemRequested = delegate { };
        #endregion

        #region Commands
        public ICommand ExtractCommand { get; }
        public ICommand AddManualItemCommand { get; }
        public ICommand CopyItemCommand { get; }
        public ICommand EditItemCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand ExportToExcelCommand { get; }
        public ICommand SelectInRevitCommand { get; }
        public ICommand VisualizeNetFaceCommand { get; }
        #endregion

        #region Constructor
        public QuantityViewModel(
            CalculateQuantityUseCase calculateQuantityUseCase,
            ExportQuantityUseCase exportQuantityUseCase,
            IDialogService dialogService,
            IFileDialogService fileDialogService)
        {
            _calculateQuantityUseCase = calculateQuantityUseCase;
            _dialogService = dialogService;
            _fileDialogService = fileDialogService;
            _exportQuantityUseCase = exportQuantityUseCase;

            var items = _calculateQuantityUseCase.Execute();
            QuantityItems = new ObservableCollection<QuantityItem>(items);
            UpdateSummary();

            ExtractCommand = new RelayCommand(GetCalculateQuantity);
            ExportToExcelCommand = new RelayCommand(_ => OnExportToExcel());
            AddManualItemCommand = new RelayCommand(_ => ManualInputRequested.Invoke(this, null));
            CopyItemCommand = new RelayCommand(_ => OnCopyItem(), _ => SelectedItem != null);
            EditItemCommand = new RelayCommand(_ => OnEditItem(), _ => SelectedItem != null);
            DeleteItemCommand = new RelayCommand(_ => OnDeleteItem(), _ => _currentSelectedItems.Count > 0);
            SelectInRevitCommand = new RelayCommand(_ => OnSelectInRevit(), _ => _currentSelectedItems.Count > 0);
            VisualizeNetFaceCommand = new RelayCommand(_ => OnVisualizeNetFace(), _ => _currentSelectedItems.Count > 0);
        }
        #endregion

        #region Methods
        public void AddItem(QuantityItem item)
        {
            QuantityItems.Add(item);
            UpdateSummary();
        }

        public void UpdateSelectedItems(IList<QuantityItem> items)
        {
            _currentSelectedItems = items.ToList();
            IsSelectedInRevit = false;

            if (!items.Any())
            {
                GroupSummaries = new ObservableCollection<GroupSummaryItem>();
                CommandManager.InvalidateRequerySuggested();
                return;
            }
            var rows = new List<GroupSummaryItem>();
            var categories = items.Select(i => i.Category).Distinct().ToList();
            rows.Add(new GroupSummaryItem
            {
                Name = "카테고리",
                ValueDisplay = categories.Count == 1 ? categories[0] : "다양함",
                Unit = string.Empty
            });

            var workTypeRows = items
                .GroupBy(i => new { i.WorkType, i.Specification, i.SubSpecification, i.Unit })
                .Select(g => new GroupSummaryItem
                {
                    Name = g.Key.WorkType,
                    Spec = FormatSpec(g.Key.Specification, g.Key.SubSpecification),
                    ValueDisplay = g.Sum(i => i.Value).ToString("F1"),
                    Unit = g.Key.Unit
                });

            rows.AddRange(workTypeRows);
            GroupSummaries = new ObservableCollection<GroupSummaryItem>(rows);
            CommandManager.InvalidateRequerySuggested();
        }
        private static string FormatSpec(string spec, string subSpec)
        {
            var hasSpec = !string.IsNullOrWhiteSpace(spec);
            var hasSubSpec = !string.IsNullOrWhiteSpace(subSpec);
            if (!hasSpec && !hasSubSpec) return string.Empty;
            if (!hasSubSpec) return spec;
            if (!hasSpec) return subSpec;
            return $"{spec} / {subSpec}";
        }

        public void ReplaceItem(int index, QuantityItem newItem)
        {
            if (index >= 0 && index < QuantityItems.Count)
            {
                var original = QuantityItems[index];
                var status = original.Status == QuantityStatus.Auto ? QuantityStatus.Modified : original.Status;
                QuantityItems[index] = newItem with { Status = status };
                OnPropertyChanged(nameof(QuantityItems));
                UpdateSummary();
            }
        }
        private void OnCopyItem()
        {
            if (SelectedItem == null) return;

            var copiedItem = SelectedItem with
            {
                Status = QuantityStatus.Manual
            };

            QuantityItems.Add(copiedItem);
            UpdateSummary();
        }
        private void OnEditItem()
        {
            if (SelectedItem == null) return;

            var index = QuantityItems.IndexOf(SelectedItem);
            if (index >= 0)
            {
                EditItemRequested.Invoke(this, (SelectedItem, index));
            }
        }
        private void OnDeleteItem()
        {
            if (_currentSelectedItems.Count == 0) return;
            foreach (var item in _currentSelectedItems)
                QuantityItems.Remove(item);
            _currentSelectedItems.Clear();
            UpdateSummary();
        }
        private void GetCalculateQuantity(object? obj)
        {
            _extractAction?.Invoke();
        }
        public void SetExtractAction(Action action) => _extractAction = action;
        public void SetSelectAction(Action<IList<long>> action) => _selectAction = action;
        public void SetVisualizeAction(Action<IList<long>> action) => _visualizeAction = action;
        public void SetMeasureService(IMeasurePickService service) => MeasureService = service;

        private void OnSelectInRevit()
        {
            var ids = _currentSelectedItems
                .Select(i => i.ElementId)
                .Distinct()
                .ToList();
            _selectAction?.Invoke(ids);
            RevitSelectedCount = ids.Count;
            IsSelectedInRevit = true;
        }

        private void OnVisualizeNetFace()
        {
            var ids = _currentSelectedItems
                .Select(i => i.ElementId)
                .Distinct()
                .ToList();
            _visualizeAction?.Invoke(ids);
        }

        public void ApplyCalculatedItems(List<QuantityItem> items)
        {
            var manualItems = QuantityItems.Where(i => i.Status == QuantityStatus.Manual).ToList();
            QuantityItems = new ObservableCollection<QuantityItem>(items.Concat(manualItems));
            OnPropertyChanged(nameof(QuantityItems));
            UpdateSummary();
        }

        private static readonly List<string> WorkTypeOrder = new()
        {
            "콘크리트",
            "거푸집",
            "비계",
            "동바리",
            "방수",
            "면",
            "스페이서",
            "그레이팅",
            "난간",
            "철근",
        };
        private void UpdateSummary()
        {
            var result = new List<QuantitySummaryItem>();

            var byWorkType = QuantityItems.GroupBy(i => i.WorkType)
                .OrderBy(g => GetWorkTypeOrder(g.Key));

            foreach (var workTypeGroup in byWorkType)
            {
                var details = workTypeGroup
                    .GroupBy(i => new { i.Specification, i.SubSpecification, i.Unit })
                    .Select(g => new QuantitySummaryItem
                    {
                        WorkType = workTypeGroup.Key,
                        Specification = g.Key.Specification,
                        SubSpecification = g.Key.SubSpecification,
                        Unit = g.Key.Unit,
                        Value = g.Sum(i => i.Value),
                    });
                result.AddRange(details);

                result.Add(new QuantitySummaryItem
                {
                    WorkType = workTypeGroup.Key,
                    Specification = "계",
                    Unit = workTypeGroup.First().Unit,
                    Value = workTypeGroup.Sum(i => i.Value),
                    IsTotal = true,
                });
            }
            SummaryItems = new ObservableCollection<QuantitySummaryItem>(result);
            OnPropertyChanged(nameof(SummaryItems));
            OnPropertyChanged(nameof(AutoCount));
            OnPropertyChanged(nameof(ModifiedCount));
            OnPropertyChanged(nameof(ManualCount));
            OnPropertyChanged(nameof(TotalCount));
        }
        private int GetWorkTypeOrder(string workType)
        {
            for (int i = 0; i < WorkTypeOrder.Count; i++)
            {
                if (workType.Contains(WorkTypeOrder[i]))
                    return i;
            }
            return int.MaxValue;
        }
        private void OnExportToExcel()
        {
            var filePath = _fileDialogService.SaveFile("Export to Excel", "Excel Files|*.xlsx", $"QuantityItems");
            if (string.IsNullOrEmpty(filePath)) return;
            _exportQuantityUseCase.Execute(filePath, SummaryItems, QuantityItems);
        }
        #endregion
    }
}
