using System;
using System.Collections.Generic;
using System.Linq;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases.Sheets;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using DHBIMWATER.UI.Views.Documentation.Sheets;


namespace DHBIMWATER.UI.ViewModels.Documentation.Sheets
{
    public class WaterReservoirViewModel : ViewModelBase
    {
        private readonly IWaterReservoirUseCase _useCase;
        private readonly IDialogService _dialogService;
        private readonly Action _refreshSheets;
        private readonly Action _reactivateWindow;

        private bool? _mainDialogResult;
        public bool? MainDialogResult
        {
            get => _mainDialogResult;
            set { _mainDialogResult = value; OnPropertyChanged(); }
        }

        private bool? _sheetsDialogResult;
        public bool? SheetsDialogResult
        {
            get => _sheetsDialogResult;
            set { _sheetsDialogResult = value; OnPropertyChanged(); }
        }

        public RelayCommand CreateSheetsCommand { get; }
        public RelayCommand ConfirmCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand SheetsConfirmCommand { get; }
        public RelayCommand SheetsCancelCommand { get; }
        public RelayCommand PlaceViewsCommand { get; }
        public RelayCommand DeleteSheetsAndViewsCommand { get; }
        public RelayCommand DeleteSheetsCommand { get; }
        public RelayCommand DeleteViewsCommand { get; }
        public RelayCommand PlaceDimensionsCommand { get; }
        public RelayCommand OpenSheetsCommand { get; }
        public RelayCommand CloseSheetsCommand { get; }
        public string RequestedReservoirDimensionTypeName { get; private set; }
        public RelayCommand PlaceAnnotatesCommand { get; }

        public WaterReservoirViewModel(
            IWaterReservoirUseCase useCase,
            IDialogService dialogService,
            Action refreshSheets,
            Action reactivateWindow)
        {
            _useCase = useCase;
            _dialogService = dialogService;
            _refreshSheets = refreshSheets;
            _reactivateWindow = reactivateWindow;

            CreateSheetsCommand = new RelayCommand(_ => OpenCreateSheetsDialog());
            ConfirmCommand = new RelayCommand(_ => MainDialogResult = true);
            CancelCommand = new RelayCommand(_ => MainDialogResult = false);
            SheetsConfirmCommand = new RelayCommand(_ => SheetsDialogResult = true);
            SheetsCancelCommand = new RelayCommand(_ => SheetsDialogResult = false);
            PlaceViewsCommand = new RelayCommand(_ => PlaceViews());
            DeleteSheetsAndViewsCommand = new RelayCommand(_ => DeleteSheetsAndViews());
            DeleteSheetsCommand = new RelayCommand(_ => DeleteSheets());
            DeleteViewsCommand = new RelayCommand(_ => DeleteViews());
            PlaceDimensionsCommand = new RelayCommand(_ => PlaceDimensions());
            OpenSheetsCommand = new RelayCommand(_ => OpenSheets());
            CloseSheetsCommand = new RelayCommand(_ => CloseSheets());
            PlaceAnnotatesCommand = new RelayCommand(_ => PlaceAnnotates());
        }

        private void OpenCreateSheetsDialog()
        {
            var dlg = new WaterReservoirSheetsView
            {
                DataContext = this
            };

            if (dlg.ShowDialog() != true)
            {
                _reactivateWindow?.Invoke();
                return;
            }

            CreateSheets();
        }

        private string _sheetNumber = "C-080";
        public string SheetNumber
        {
            get => _sheetNumber;
            set
            {
                _sheetNumber = value;
                OnPropertyChanged();
            }
        }

        private string _sheetCount = "12";
        public string SheetName
        {
            get => _sheetCount;
            set
            {
                _sheetCount = value;
                OnPropertyChanged();
            }
        }


        private void CreateSheets()
        {
            if (string.IsNullOrWhiteSpace(SheetNumber))
            {
                _dialogService.Warn("배수지 시트 생성", "시작 도면번호를 입력하세요.");
                _reactivateWindow?.Invoke();
                return;
            }

            if (!int.TryParse(SheetName, out var totalSheetCount) || totalSheetCount <= 0)
            {
                _dialogService.Warn("배수지 시트 생성", "Sheets 총 개수는 1 이상의 숫자로 입력하세요.");
                _reactivateWindow?.Invoke();
                return;
            }

            var result = _useCase.CreateReservoirSheets(SheetNumber.Trim(), totalSheetCount);


            if (result.HasDuplicates)
            {
                var messages = new List<string>();

                if (result.DuplicateSheetNumbers.Any())
                    messages.Add("중복 도면번호: " + string.Join(", ", result.DuplicateSheetNumbers));

                if (result.DuplicateSheetNames.Any())
                    messages.Add("중복 시트이름: " + string.Join(", ", result.DuplicateSheetNames));

                if (result.CreatedCount > 0)
                    messages.Add($"새로 생성된 시트: {result.CreatedCount}개");

                _refreshSheets?.Invoke();
                _dialogService.Warn("배수지 시트 생성", string.Join("\n", messages));
                _reactivateWindow?.Invoke();
                return;
            }
            _refreshSheets?.Invoke();
            _dialogService.Info("배수지 시트 생성", $"{result.CreatedCount}개의 시트가 생성되었습니다.");
            _reactivateWindow?.Invoke();
        }

        private void PlaceViews()
        {
            _useCase.PlaceReservoirViews();
            _refreshSheets?.Invoke();
            _dialogService.Info("배수지 뷰 배치", "뷰 배치가 완료되었습니다.");
            _reactivateWindow?.Invoke();
        }

        private void DeleteSheetsAndViews()
        {
            if (!_dialogService.Confirm("배수지 삭제", "배수지용 시트와 SHT 뷰를 모두 삭제하시겠습니까?"))
                return;

            _useCase.DeleteReservoirSheetsAndViews();
            _refreshSheets?.Invoke();
            _dialogService.Info("배수지 삭제", "배수지용 시트와 SHT 뷰가 삭제되었습니다.");
            _reactivateWindow?.Invoke();
        }

        private void DeleteSheets()
        {
            if (!_dialogService.Confirm("배수지 시트 삭제", "배수지용 시트를 삭제하시겠습니까?"))
                return;

            _useCase.DeleteReservoirSheets();
            _refreshSheets?.Invoke();
            _dialogService.Info("배수지 시트 삭제", "배수지용 시트가 삭제되었습니다.");
            _reactivateWindow?.Invoke();
        }

        private void DeleteViews()
        {
            if (!_dialogService.Confirm("배수지 뷰 삭제", "배수지용 SHT 뷰를 삭제하시겠습니까?"))
                return;

            _useCase.DeleteReservoirViews();
            _refreshSheets?.Invoke();
            _dialogService.Info("배수지 뷰 삭제", "배수지용 SHT 뷰가 삭제되었습니다.");
            _reactivateWindow?.Invoke();
        }

        private void PlaceDimensions()
        {
            var dimensionTypes = _useCase.GetDimensionTypes();
            var vm = new DImensionTypeViewModel(dimensionTypes);
            var dlg = new DimensionTypeView(vm);

            if (dlg.ShowDialog() != true || vm.SelectedDimensionType == null)
                return;           

            _useCase.OpenReservoirSheets();
            _useCase.ApplyReservoirDimensions(vm.SelectedDimensionType.Name);

            _refreshSheets?.Invoke();
            _dialogService.Info("배수지 치수 배치", "치수선 배치가 완료되었습니다.");
            _reactivateWindow?.Invoke();
        }
        private void OpenSheets()
        {            
            _useCase.OpenReservoirSheets();
            _dialogService.Info("배수지 시트 열기", "배수지용 시트를 순서대로 활성화했습니다.");
            _reactivateWindow?.Invoke();
        }
        private void CloseSheets()
        {
            _useCase.CloseReservoirSheets();
            _dialogService.Info("배수지 시트 닫기", "원래 보던 뷰로 복귀했습니다.");
            _reactivateWindow?.Invoke();
        }
        private void PlaceAnnotates()
        {
            _useCase.ApplyReservoirTags();
            _refreshSheets?.Invoke();
            _dialogService.Info("배수지 주석 배치", "배수지 도면 전체 주석 배치가 완료되었습니다.");
            _reactivateWindow?.Invoke();
        }
        private bool TryUpdateReservoirSheetRange()
        {
            if (string.IsNullOrWhiteSpace(SheetNumber))
            {
                _dialogService.Warn("배수지 도면", "시작 도면번호를 입력하세요.");
                _reactivateWindow?.Invoke();
                return false;
            }

            if (!int.TryParse(SheetName, out var totalSheetCount) || totalSheetCount <= 0)
            {
                _dialogService.Warn("배수지 도면", "Sheets 총 개수는 1 이상의 숫자로 입력하세요.");
                _reactivateWindow?.Invoke();
                return false;
            }

            _useCase.SetReservoirSheetRange(SheetNumber.Trim(), totalSheetCount);
            return true;
        }

    }
}
