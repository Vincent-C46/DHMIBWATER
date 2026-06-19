using System;
using System.Linq;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases.Sheets;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using DHBIMWATER.UI.Views.Documentation.Sheets;

namespace DHBIMWATER.UI.ViewModels.Documentation.Sheets
{
    public class PumpingStationSheetsViewModel : ViewModelBase
    {
        private readonly IPumpingStationUseCase _useCase;
        private readonly IDialogService _dialogService;
        private readonly Action _refreshSheets;

        private bool? _mainDialogResult;
        public bool? MainDialogResult
        {
            get => _mainDialogResult;
            set { _mainDialogResult = value; OnPropertyChanged(); }
        }

        public RelayCommand CreateSheetsCommand { get; }
        public RelayCommand PlaceViewsCommand { get; }
        public RelayCommand PlaceDimensionsCommand { get; }
        public RelayCommand PlaceAnnotatesCommand { get; }
        public RelayCommand DeleteSheetsAndViewsCommand { get; }
        public RelayCommand DeleteSheetsCommand { get; }
        public RelayCommand DeleteViewsCommand { get; }
        public RelayCommand ConfirmCommand { get; }
        public RelayCommand CancelCommand { get; }

        public PumpingStationSheetsViewModel(
            IPumpingStationUseCase useCase,
            IDialogService dialogService,
            Action refreshSheets)
        {
            _useCase = useCase;
            _dialogService = dialogService;
            _refreshSheets = refreshSheets;

            CreateSheetsCommand = new RelayCommand(_ => CreateSheets());
            PlaceViewsCommand = new RelayCommand(_ => PlaceViews());
            PlaceDimensionsCommand = new RelayCommand(_ => PlaceDimensions());
            PlaceAnnotatesCommand = new RelayCommand(_ => PlaceAnnotates());
            DeleteSheetsAndViewsCommand = new RelayCommand(_ => DeleteSheetsAndViews());
            DeleteSheetsCommand = new RelayCommand(_ => _dialogService.Info("펌프장", "개발 예정입니다."));
            DeleteViewsCommand = new RelayCommand(_ => _dialogService.Info("펌프장", "개발 예정입니다."));
            ConfirmCommand = new RelayCommand(_ => MainDialogResult = true);
            CancelCommand = new RelayCommand(_ => MainDialogResult = false);
        }

        private void DeleteSheetsAndViews()
        {
            int deleted = _useCase.DeletePumpingStationSheets();

            if (deleted == 0)
                _dialogService.Warn("삭제 완료", "삭제할 펌프장 시트가 없습니다.");
            else
                _dialogService.Info("삭제 완료", $"시트 {deleted}개 및 출력 뷰가 삭제되었습니다.");

            _refreshSheets?.Invoke();
        }

        private void PlaceViews()
        {
            var result = _useCase.PlacePumpingStationViews();

            if (result.PlacedCount == 0 && result.NotFoundSheets.Count > 0)
            {
                _dialogService.Warn("Views 배치 실패",
                    $"매칭되는 뷰를 찾을 수 없습니다.\n{string.Join(", ", result.NotFoundSheets)}");
                return;
            }

            var msg = $"뷰 {result.PlacedCount}개가 배치되었습니다.";
            if (result.NotFoundSheets.Count > 0)
                msg += $"\n매칭 실패: {string.Join(", ", result.NotFoundSheets)}";

            _refreshSheets?.Invoke();
            _dialogService.Info("Views 배치 완료", msg);
        }

        private void PlaceDimensions()
        {
            var dimTypes = _useCase.GetDimensionTypes();
            var vm = new DImensionTypeViewModel(dimTypes);
            var dialog = new DimensionTypeView(vm);
            if (dialog.ShowDialog() != true) return;
            _useCase.PlacePumpingStationDimensions(vm.SelectedDimensionType?.Name);
            _dialogService.Info("치수선 배치 완료", "단면 시트에 치수선을 배치했습니다.");
        }

        private void PlaceAnnotates()
        {
            var tagFamilies = _useCase.GetAvailableTagFamilies();
            var vm = new AnnotateSelectViewModel(tagFamilies);
            var dialog = new AnnotateSelectView(vm);
            if (dialog.ShowDialog() != true) return;

            _useCase.ApplyPumpingStationAnnotations();
            _useCase.ApplyDHTags(vm.SelectedTagFamilyIds);
            _refreshSheets?.Invoke();
            _dialogService.Info("펌프장 주석 배치", "시트에 주석을 배치하였습니다.");
        }

        private void CreateSheets()
        {
            var titleBlocks = _useCase.GetTitleBlocks();
            var selectVm = new SheetsSelectViewModel(titleBlocks);
            var selectDialog = new SheetsSelectView(selectVm);
            if (selectDialog.ShowDialog() != true) return;

            var result = _useCase.CreatePumpingStationSheets(selectVm.SelectedTitleBlock?.Id);

            if (result.CreatedCount == 0 && result.HasDuplicates)
            {
                _dialogService.Warn("시트 생성 실패",
                    $"중복된 시트 번호가 있어 생성할 수 없습니다.\n{string.Join(", ", result.DuplicateSheetNumbers)}");
                return;
            }

            if (result.CreatedCount == 0)
            {
                _dialogService.Warn("시트 생성 실패", "도곽을 찾을 수 없거나 생성할 단면 뷰가 없습니다.");
                return;
            }

            _refreshSheets?.Invoke();
            _dialogService.Info("Sheets 배치 완료", $"시트 {result.CreatedCount}개가 생성되었습니다.");
        }
    }
}
