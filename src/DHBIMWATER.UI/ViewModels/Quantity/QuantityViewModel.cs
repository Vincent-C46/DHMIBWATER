using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases.QuantityCalculator;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Quantity
{
    public class QuantityViewModel : ViewModelBase
    {
        #region Fields
        private IDialogService _dialogService;
        private readonly CalculateQuantityUseCase _calculateQuantityUseCase;
        public ObservableCollection<QuantitySummaryItem> SummaryItems { get; set; }
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
                    // Command CanExecute 재평가
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

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                    _selectedTabIndex = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// 수동 입력 다이얼로그 열기 요청
        /// </summary>
        public event EventHandler<QuantityItem?> ManualInputRequested = delegate { };
        
        /// <summary>
        /// 항목 수정 요청 (기존 항목, 원본 인덱스 전달)
        /// </summary>
        public event EventHandler<(QuantityItem item, int index)> EditItemRequested = delegate { };
        #endregion

        #region Commands
        public ICommand ExtractCommand       { get; }
        public ICommand AddManualItemCommand { get; }
        public ICommand CopyItemCommand      { get; }
        public ICommand EditItemCommand      { get; }
        #endregion

        #region Constructor
        public QuantityViewModel(CalculateQuantityUseCase useCase, IDialogService dialogService)
        {
            _calculateQuantityUseCase = useCase;
            _dialogService = dialogService;

            var items = _calculateQuantityUseCase.Execute();
            QuantityItems = new ObservableCollection<QuantityItem>(items);
            UpdateSummary();

            ExtractCommand       = new RelayCommand(GetCalculateQuantity);
            AddManualItemCommand = new RelayCommand(_ => ManualInputRequested.Invoke(this, null));
            CopyItemCommand      = new RelayCommand(_ => OnCopyItem(), _ => SelectedItem != null);
            EditItemCommand      = new RelayCommand(_ => OnEditItem(), _ => SelectedItem != null);
        }
        #endregion

        #region Methods
        /// <summary>
        /// ManualQuantityView 확인 시 호출. 수동 항목 추가 후 집계 갱신.
        /// </summary>
        public void AddItem(QuantityItem item)
        {
            QuantityItems.Add(item);
            UpdateSummary();
        }

        /// <summary>
        /// 항목 수정 후 기존 항목 Replace
        /// </summary>
        public void ReplaceItem(int index, QuantityItem newItem)
        {
            if (index >= 0 && index < QuantityItems.Count)
            {
                QuantityItems[index] = newItem;
                OnPropertyChanged(nameof(QuantityItems));
                UpdateSummary();
            }
        }

        private void OnCopyItem()
        {
            if (SelectedItem == null) return;
            
            // 선택된 아이템을 복사해서 Manual 상태로 추가
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
        
        private void GetCalculateQuantity(object? obj)
        {
            // 수동 입력 항목은 재산출 후에도 유지
            var manualItems = QuantityItems.Where(i => i.Status == QuantityStatus.Manual).ToList();
            var items = _calculateQuantityUseCase.Execute();
            QuantityItems = new ObservableCollection<QuantityItem>(items.Concat(manualItems));
            OnPropertyChanged(nameof(QuantityItems));
            UpdateSummary();
        }

        // 해당 단어 포함된 공종 순으로 Sorting
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
                .OrderBy(g => GetWorkTypeOrder(g.Key));         // IGrouping<string, QuantityItem> 의 집합. 여기서 string은 그룹핑한 WorkType

            foreach (var workTypeGroup in byWorkType)
            {
                // 규격별 소계
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

                // 공종별 합계
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
        #endregion
    }
}