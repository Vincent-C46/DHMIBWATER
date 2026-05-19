using System.Collections.ObjectModel;
using DHBIMWATER.Application.DTOs.Revit.Sheet;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases;
using DHBIMWATER.Application.UseCases.Sheets;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using DHBIMWATER.UI.ViewModels.Documentation.Sheets;
using DHBIMWATER.UI.Views.Documentation.Sheets;
using static DHBIMWATER.UI.ViewModels.Documentation.SheetManagerViewModel.SheetRow;


namespace DHBIMWATER.UI.ViewModels.Documentation
{
    public class SheetManagerViewModel : ViewModelBase
    {
        private readonly ISheetUseCase _useCase;

        private readonly IWaterReservoirUseCase _waterReservoirUseCase;

        private readonly List<SheetPendingAction> _pending = new();

        private readonly IDialogService _dialogService;

        public ObservableCollection<SheetRow> Sheets { get; } = new();        
        public string RequestedCurrentViewDimensionTypeName { get; private set; }
        public DimensionSide RequestedCurrentViewDimensionSides { get; private set; }
        public bool RequestedCurrentViewIncludeOverall { get; private set; }
        public bool RequestedCurrentViewSelectedObjects { get; private set; }
        public bool RequestedCurrentViewSelectedAnnotates { get; private set; }
        public bool RequestedCurrentViewAllAnnotates { get; private set; }



        private SheetRow _selectedSheet;

        public SheetRow SelectedSheet
        {
            get => _selectedSheet;
            set
            {
                _selectedSheet = value;
                OnPropertyChanged();
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        public RelayCommand AddCommand { get; }
        public RelayCommand RemoveCommand { get; }
        public RelayCommand CopyCommand { get; }
        public RelayCommand AddViewCommand { get; }
        public RelayCommand ChangeViewCommand { get; }
        public RelayCommand RemoveViewCommand { get; }
        public RelayCommand ConfirmCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand DimensionCommand { get; }
        public RelayCommand WaterReservoirCommand { get; }
        public enum SheetActionType { Create, Delete, Copy, Rename, AddView, ReplaceView, RemoveView, ArrangeViews }
        public RelayCommand AnnotateCommand { get; }


        public class SheetPendingAction
        {
            public SheetActionType Type { get; set; }
            public string SheetId { get; set; }
            public string SheetNumber { get; set; }
            public string SheetName { get; set; }
            public string TitleBlockId { get; set; }
            public string ViewId { get; set; }
            public string OldViewId { get; set; }
            public int Scale { get; set; }
            public string VisualStyle { get; set; }
            public string NewTempSheetId { get; set; } // Copy로 만들어진 UI 임시 row id
            public string ViewTitleOnSheet { get; set; }
            public string SheetForm { get; set; }
            public string DrawingTitle { get; set; }
            public string DrawingMember { get; set; }
            public string DrawingScale { get; set; }
            public string DrawingNumber { get; set; }
            public string ViewDirectionType { get; set; }
            public bool DuplicateView { get; set; } = true;

        }

        public SheetManagerViewModel(
            ISheetUseCase useCase,
            IWaterReservoirUseCase waterReservoirUseCase,
            IDialogService dialogService)
        {
            _useCase = useCase;
            _waterReservoirUseCase = waterReservoirUseCase;
            _dialogService = dialogService;

            AddCommand = new RelayCommand(_ => Add());
            RemoveCommand = new RelayCommand(_ => Remove(), _ => SelectedSheet != null);
            CopyCommand = new RelayCommand(_ => Copy(), _ => SelectedSheet != null);
            ConfirmCommand = new RelayCommand(_ => Confirm());
            CancelCommand = new RelayCommand(_ => Cancel());
            AddViewCommand = new RelayCommand(p => AddView(p as SheetRow), p => p is SheetRow);
            ChangeViewCommand = new RelayCommand(p => ChangeView(p as SheetViewRow), _ => SelectedSheet != null);
            RemoveViewCommand = new RelayCommand(p => RemoveLastView(p as SheetRow), p => p is SheetRow row && row.Views.Count > 0);
            DimensionCommand = new RelayCommand(_ => ApplyDimensions());
            WaterReservoirCommand = new RelayCommand(_ => OpenWaterReservoir());
            AnnotateCommand = new RelayCommand(_ => ApplyAnnotates());

            LoadSheets();
        }

        private void LoadSheets()
        {
            Sheets.Clear();
            foreach (var s in _useCase.GetSheets())
            {
                var row = new SheetRow
                {
                    Id = s.Id,
                    SheetNumber = s.SheetNumber,
                    SheetName = s.SheetName,
                    Views = new ObservableCollection<SheetViewRow>()
                };

                if (s.Views != null)
                {
                    foreach (var v in s.Views)
                    {
                        row.Views.Add(new SheetViewRow
                        {
                            ViewId = v.ViewId,
                            ViewName = v.ViewName,
                            ViewType = v.ViewType
                        });
                    }
                }

                // 이벤트 구독 전에 저장된 방향 복원 (구독 전이라 QueueArrange 미발생)
                if (!string.IsNullOrWhiteSpace(s.ViewDirName))
                {
                    var savedDir = row.ViewDirections.FirstOrDefault(x => x.Type == s.ViewDirName);
                    if (savedDir != null)
                        row.SelectedViewDirection = savedDir;
                }

                row.NameChanged += OnSheetNameChanged;
                row.DirectionChanged += OnSheetDirectionChanged;
                Sheets.Add(row);
            }
        }


        private void Add()
        {
            var titleBlocks = _useCase.GetTitleBlocks();

            var addVm = new AddSheetsViewModel(titleBlocks);
            var dlg = new AddSheetsView(addVm);

            if (dlg.ShowDialog() != true) return;

            var vm = dlg.DataContext as AddSheetsViewModel;
            if (vm == null || vm.SelectedTitleBlock == null) return;

            var row = new SheetRow
            {
                Id = Guid.NewGuid().ToString(), // 임시 ID
                SheetNumber = vm.SheetNumber,
                SheetName = vm.SheetName,
                SheetSubtitle = vm.SheetSubtitle
            };
            row.NameChanged += OnSheetNameChanged;
            row.DirectionChanged += OnSheetDirectionChanged;
            Sheets.Add(row);

            _pending.Add(new SheetPendingAction
            {
                Type = SheetActionType.Create,
                SheetId = row.Id,
                SheetNumber = row.SheetNumber,
                SheetName = row.SheetName,
                TitleBlockId = vm.SelectedTitleBlock.Id,
                DrawingMember = vm.SheetSubtitle
            });
        }

        private void AddView(SheetRow row)
        {
            if (row == null) return;

            var vm = new ViewManageViewModel(_useCase.GetViews());
            var dlg = new ViewManageView(vm);
            if (dlg.ShowDialog() != true) return;

            var selected = vm.SelectedView;
            if (selected == null) return;

            var duplicateView = _dialogService.Confirm(
                "뷰 복제",
                $"선택한 뷰를 복제하여 시트에 추가할까요?\n\n복제하면 '{selected.ViewName}_시트' 형태의 새 뷰가 생성됩니다.\n복제하지 않으면 원본 뷰가 그대로 시트에 배치됩니다.");

            row.Views.Add(new SheetViewRow
            {
                ViewId = selected.ViewId,
                ViewName = selected.ViewName,
                ViewType = selected.ViewType
            });

            _pending.Add(new SheetPendingAction
            {
                Type = SheetActionType.AddView,
                SheetId = row.Id,
                ViewId = selected.ViewId,
                Scale = selected.Scale,
                VisualStyle = selected.VisualStyle,
                ViewTitleOnSheet = selected.SheetName,
                SheetForm = selected.SheetForm,
                DrawingTitle = row.SheetName,
                DrawingMember = string.Empty,
                DrawingScale = selected.Scale > 0 ? $"1:{selected.Scale}" : string.Empty,
                DrawingNumber = row.SheetNumber,
                DuplicateView = duplicateView
            });

            QueueArrange(row);
        }


        private void RemoveLastView(SheetRow row)
        {
            if (row == null || row.Views.Count == 0) return;

            var last = row.Views[row.Views.Count - 1];
            row.Views.RemoveAt(row.Views.Count - 1);

            _pending.Add(new SheetPendingAction
            {
                Type = SheetActionType.RemoveView,
                SheetId = row.Id,
                ViewId = last.ViewId
            });

            QueueArrange(row);
        }


        private void ChangeView(SheetViewRow target)
        {
            if (target == null) return;

            var currentView = _useCase.GetViews()
            .FirstOrDefault(x => x.ViewId == target.ViewId);

            if (currentView == null) return;

            var vm = new ViewManageViewModel(new List<ViewInfoDto> { currentView });

            var dlg = new ViewManageView(vm);
            if (dlg.ShowDialog() != true) return;

            var selected = vm.SelectedView;
            if (selected == null) return;

            var oldViewId = target.ViewId;  // 먼저 저장

            // UI 변경
            target.ViewId = selected.ViewId;
            target.ViewName = selected.ViewName;
            target.ViewType = selected.ViewType;

            // Replace만 기록 (Add 기록 금지)
            _pending.Add(new SheetPendingAction
            {
                Type = SheetActionType.ReplaceView,
                SheetId = SelectedSheet.Id,
                OldViewId = oldViewId,
                ViewId = selected.ViewId,
                Scale = selected.Scale,
                VisualStyle = selected.VisualStyle,
                ViewTitleOnSheet = selected.SheetName,
                SheetForm = selected.SheetForm,
                DrawingTitle = SelectedSheet?.SheetName,
                DrawingMember = string.Empty,
                DrawingScale = selected.Scale > 0 ? $"1:{selected.Scale}" : string.Empty,
                DrawingNumber = SelectedSheet?.SheetNumber
            });

            QueueArrange(SelectedSheet);
        }

        private void Remove()
        {
            if (SelectedSheet == null) return;

            // 1) 신규 생성 대기 시트면 Create 액션 취소 후 UI에서만 제거
            var pendingCreate = _pending.FirstOrDefault(p =>
                p.Type == SheetActionType.Create &&
                p.SheetId == SelectedSheet.Id);

            if (pendingCreate != null)
            {
                _pending.Remove(pendingCreate);
                Sheets.Remove(SelectedSheet);
                return;
            }

            // 2) 복사 대기 시트면 Copy 액션 취소 후 UI에서만 제거
            var pendingCopy = _pending.FirstOrDefault(p =>
                p.Type == SheetActionType.Copy &&
                p.NewTempSheetId == SelectedSheet.Id);

            if (pendingCopy != null)
            {
                _pending.Remove(pendingCopy);
                Sheets.Remove(SelectedSheet);
                return;
            }

            // 3) 기존 Revit 시트면 Delete 액션 추가
            _pending.Add(new SheetPendingAction
            {
                Type = SheetActionType.Delete,
                SheetId = SelectedSheet.Id
            });

            Sheets.Remove(SelectedSheet);
        }


        private void Copy()
        {
            if (SelectedSheet == null) return;

            var row = new SheetRow
            {
                Id = Guid.NewGuid().ToString(), // 임시 ID
                SheetNumber = SelectedSheet.SheetNumber + "_COPY",
                SheetName = SelectedSheet.SheetName + "_Copy"
            };
            row.SelectedViewDirection = row.ViewDirections.FirstOrDefault(x =>
                x.Type == SelectedSheet.SelectedViewDirection?.Type) ?? row.ViewDirections.FirstOrDefault();
            row.NameChanged += OnSheetNameChanged;
            row.DirectionChanged += OnSheetDirectionChanged;
            Sheets.Add(row);

            _pending.Add(new SheetPendingAction
            {
                Type = SheetActionType.Copy,
                SheetId = SelectedSheet.Id,
                NewTempSheetId = row.Id          // 복사본 임시 row
            });
        }

        private void OnSheetNameChanged(SheetRow row)
        {
            if (row == null) return;

            var existingRename = _pending.LastOrDefault(p =>
                p.Type == SheetActionType.Rename &&
                p.SheetId == row.Id);

            if (existingRename != null)
            {
                existingRename.SheetName = row.SheetName;
                return;
            }

            _pending.Add(new SheetPendingAction
            {
                Type = SheetActionType.Rename,
                SheetId = row.Id,
                SheetName = row.SheetName
            });
        }

        private void OnSheetDirectionChanged(SheetRow row)
        {
            QueueArrange(row);
        }

        private void QueueArrange(SheetRow row)
        {
            if (row == null) return;

            _pending.RemoveAll(p =>
                p.Type == SheetActionType.ArrangeViews &&
                p.SheetId == row.Id);

            _pending.Add(new SheetPendingAction
            {
                Type = SheetActionType.ArrangeViews,
                SheetId = row.Id,
                ViewDirectionType = row.SelectedViewDirection?.Type ?? "Center"
            });
        }


        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; OnPropertyChanged(); }
        }

        private void Confirm()
        {
            var tempToRealSheetId = new Dictionary<string, string>();

            string ResolveSheetId(string sheetId)
            {
                if (string.IsNullOrWhiteSpace(sheetId)) return sheetId;
                return tempToRealSheetId.TryGetValue(sheetId, out var realId) ? realId : sheetId;
            }

            foreach (var p in _pending)
            {
                switch (p.Type)
                {
                    case SheetActionType.Create:
                        var created = _useCase.CreateSheet(p.TitleBlockId, p.SheetNumber, p.SheetName);
                        if (created == null)
                        {
                            _dialogService.Warn("시트 생성 실패", $"도면 번호 '{p.SheetNumber}'이(가) 이미 존재하거나 시트를 생성할 수 없습니다.");
                            var failedId = p.SheetId;
                            var failedRow = Sheets.FirstOrDefault(s => s.Id == failedId);
                            if (failedRow != null) Sheets.Remove(failedRow);
                            _pending.RemoveAll(x => x.SheetId == failedId);
                            return;
                        }
                        if (!string.IsNullOrWhiteSpace(created.Id) &&
                            !string.IsNullOrWhiteSpace(p.SheetId))
                        {
                            tempToRealSheetId[p.SheetId] = created.Id;
                        }
                        break;

                    case SheetActionType.Delete:
                        _useCase.DeleteSheet(ResolveSheetId(p.SheetId));
                        break;

                    case SheetActionType.Copy:
                        var copied = _useCase.CopySheet(ResolveSheetId(p.SheetId));
                        if (copied != null &&
                            !string.IsNullOrWhiteSpace(copied.Id) &&
                            !string.IsNullOrWhiteSpace(p.NewTempSheetId))
                        {
                            tempToRealSheetId[p.NewTempSheetId] = copied.Id;
                        }
                        break;

                    case SheetActionType.Rename:
                        _useCase.RenameSheet(ResolveSheetId(p.SheetId), p.SheetName);
                        break;

                    case SheetActionType.AddView:
                        var placedViewId = _useCase.AddViewToSheet(ResolveSheetId(p.SheetId), p.ViewId, duplicate: p.DuplicateView);

                        if (!string.IsNullOrWhiteSpace(placedViewId))
                        {
                            if (p.Scale > 0)
                                _useCase.UpdateViewScale(placedViewId, p.Scale);

                            if (!string.IsNullOrWhiteSpace(p.VisualStyle))
                                _useCase.UpdateViewVisualStyle(placedViewId, p.VisualStyle);

                            if (!string.IsNullOrWhiteSpace(p.ViewTitleOnSheet))
                                _useCase.UpdateViewTitleOnSheet(placedViewId, p.ViewTitleOnSheet);

                            if (!string.IsNullOrWhiteSpace(p.SheetForm))
                                _useCase.ApplyViewFormProfile(placedViewId, p.SheetForm);

                            _useCase.UpdateSheetParameters(ResolveSheetId(p.SheetId),
                                p.DrawingTitle, p.DrawingMember, p.DrawingScale, p.DrawingNumber);
                            _useCase.RecenterViewportToSheetCenter(ResolveSheetId(p.SheetId), placedViewId);
                            _useCase.UpdateReservoirViewportTitleLayout(ResolveSheetId(p.SheetId), placedViewId, false);
                        }
                        break;


                    case SheetActionType.ReplaceView:
                        _useCase.ReplaceViewOnSheet(ResolveSheetId(p.SheetId), p.OldViewId, p.ViewId);
                        if (p.Scale > 0) _useCase.UpdateViewScale(p.ViewId, p.Scale);
                        if (!string.IsNullOrWhiteSpace(p.VisualStyle))
                            _useCase.UpdateViewVisualStyle(p.ViewId, p.VisualStyle);
                        if (!string.IsNullOrWhiteSpace(p.ViewTitleOnSheet))
                            _useCase.UpdateViewTitleOnSheet(p.ViewId, p.ViewTitleOnSheet);
                        if (!string.IsNullOrWhiteSpace(p.SheetForm))
                            _useCase.ApplyViewFormProfile(p.ViewId, p.SheetForm);

                        _useCase.UpdateSheetParameters(ResolveSheetId(p.SheetId),
                            p.DrawingTitle, p.DrawingMember, p.DrawingScale, p.DrawingNumber);
                        _useCase.RecenterViewportToSheetCenter(ResolveSheetId(p.SheetId), p.ViewId);
                        _useCase.UpdateReservoirViewportTitleLayout(ResolveSheetId(p.SheetId), p.ViewId, false);
                        break;

                    case SheetActionType.RemoveView:
                        _useCase.RemoveView(ResolveSheetId(p.SheetId), p.ViewId);
                        break;

                    case SheetActionType.ArrangeViews:
                        _useCase.ArrangeViewportsByDirection(ResolveSheetId(p.SheetId), p.ViewDirectionType);
                        _useCase.SaveSheetDirection(ResolveSheetId(p.SheetId), p.ViewDirectionType);
                        break;
                }
            }
            _pending.Clear();
            RequestedCurrentViewSelectedObjects = false;
            RequestedCurrentViewDimensionTypeName = null;
            DialogResult = true;

            var viewsById = _useCase.GetViews().ToDictionary(x => x.ViewId);

            foreach (var row in Sheets)
            {
                var realSheetId = ResolveSheetId(row.Id);
                if (string.IsNullOrWhiteSpace(realSheetId)) continue;

                var firstView = row.Views.FirstOrDefault();
                var drawingScale = string.Empty;

                if (firstView != null && viewsById.TryGetValue(firstView.ViewId, out var viewInfo))
                {
                    if (viewInfo.Scale > 0)
                        drawingScale = $"1:{viewInfo.Scale}";
                }

                _useCase.UpdateSheetParameters(
                    realSheetId,
                    row.SheetName,
                    row.SheetSubtitle ?? string.Empty,
                    drawingScale,
                    row.SheetNumber);
            }

        }

        private void Cancel()
        {
            _pending.Clear();
            DialogResult = false;
            RequestedCurrentViewSelectedObjects = false;
            RequestedCurrentViewDimensionTypeName = null;
            RequestedCurrentViewSelectedAnnotates = false;


        }

        private void ApplyDimensions()
        {
            var dimensionTypes = _useCase.GetDimensionTypes();
            var vm = new DimensionViewModel(dimensionTypes);
            var dlg = new DimensionView { DataContext = vm };

            if (dlg.ShowDialog() != true || vm.SelectedDimensionType == null)
                return;

            if (vm.SelectedDimensionMode == DimensionMode.SelectedObjects)
            {
                var dirVm = new DimensionDirectionViewModel();
                var dirDlg = new DimensionDirectionView(dirVm);
                if (dirDlg.ShowDialog() != true)
                    return;

                RequestedCurrentViewSelectedObjects = true;
                RequestedCurrentViewDimensionTypeName = vm.SelectedDimensionType.Name;
                RequestedCurrentViewDimensionSides = dirVm.SelectedSides;
                RequestedCurrentViewIncludeOverall = dirVm.IsIncludeOverall;
                DialogResult = false;
                return;
            }

            _useCase.ApplyDimensionsOnCurrentView(
                vm.SelectedDimensionMode,
                vm.SelectedDimensionType.Name,
                DimensionSide.All);
        }


        private void OpenWaterReservoir()
        {
            WaterReservoirView dlg = null;

            Action reactivateWindow = () =>
            {
                if (dlg != null)
                {
                    dlg.Activate();
                    dlg.Focus();
                }
            };

            var vm = new WaterReservoirViewModel(
                    _waterReservoirUseCase,
                    _dialogService,
                    LoadSheets,
                    reactivateWindow);

            dlg = new WaterReservoirView(vm);

            var owner = System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>()
                .FirstOrDefault(x => x is SheetManagerView);
            
            if (owner != null)
                dlg.Owner = owner;

            dlg.ShowDialog();

            if (owner != null)
                owner.Activate();
        }

        public class SheetRow : ViewModelBase
        {
            public string Id { get; set; }


            public event Action<SheetRow> NameChanged;
            public event Action<SheetRow> DirectionChanged;

            private string _sheetNumber;
            public ObservableCollection<SheetViewRow> Views { get; set; } = new();

            public string SheetNumber
            {
                get => _sheetNumber;
                set { _sheetNumber = value; OnPropertyChanged(); }
            }

            private string _sheetName;
            public string SheetName
            {
                get => _sheetName;
                set
                {
                    if (_sheetName == value) return;
                    _sheetName = value;
                    OnPropertyChanged();
                    NameChanged?.Invoke(this);
                }
            }

            public string SheetSubtitle { get; set; }
            public class SheetViewRow : ViewModelBase
            {
                private string _viewId;
                public string ViewId
                {
                    get => _viewId;
                    set { _viewId = value; OnPropertyChanged(); }
                }

                private string _viewName;
                public string ViewName
                {
                    get => _viewName;
                    set { _viewName = value; OnPropertyChanged(); }
                }

                private string _viewType;
                public string ViewType
                {
                    get => _viewType;
                    set { _viewType = value; OnPropertyChanged(); }
                }
            }
            public class ViewDirectionItem
            {
                public string Name { get; set; }
                public string Type { get; set; }
            }


            public List<ViewDirectionItem> ViewDirections { get; } = new()
            {
                new ViewDirectionItem { Name = "중앙",          Type = "Center" },
                new ViewDirectionItem { Name = "가로",          Type = "Horizontal" },
                new ViewDirectionItem { Name = "세로",          Type = "Vertical" },
                new ViewDirectionItem { Name = "지그재그 가로", Type = "ZHorizontal" },
                new ViewDirectionItem { Name = "지그재그 세로", Type = "ZVertical" }
            };

            public SheetRow()
            {
                _selectedViewDirection = ViewDirections.FirstOrDefault();
                _viewDirName = _selectedViewDirection?.Name;
            }

            private ViewDirectionItem _selectedViewDirection;
            public ViewDirectionItem SelectedViewDirection
            {
                get => _selectedViewDirection;
                set
                {
                    if (_selectedViewDirection == value) return;
                    _selectedViewDirection = value;
                    OnPropertyChanged();
                    ViewDirName = value?.Name;
                    DirectionChanged?.Invoke(this);
                }
            }
            private string _viewDirName;
            public string ViewDirName
            {
                get => _viewDirName;
                set
                {
                    _viewDirName = value;
                    OnPropertyChanged();
                }
            }
        }
        private void ApplyAnnotates()
        {
            var vm = new AnnotateViewModel();
            var dlg = new AnnotateView(vm);

            if (dlg.ShowDialog() != true)
                return;

            if (vm.IsSelectedObjectsMode)
            {
                RequestedCurrentViewSelectedAnnotates = true;
                DialogResult = false;
                return;
            }

            RequestedCurrentViewAllAnnotates = true;
            DialogResult = false;
        }

    }
}

