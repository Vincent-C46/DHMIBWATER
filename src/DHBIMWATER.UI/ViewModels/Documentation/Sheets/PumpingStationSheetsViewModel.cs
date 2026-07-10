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

        public RelayCommand PlaceAllCommand { get; }
        public RelayCommand DeleteSheetsAndViewsCommand { get; }
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

            PlaceAllCommand = new RelayCommand(_ => PlaceAll());
            DeleteSheetsAndViewsCommand = new RelayCommand(_ => DeleteSheetsAndViews());
            ConfirmCommand = new RelayCommand(_ => MainDialogResult = true);
            CancelCommand = new RelayCommand(_ => MainDialogResult = false);
        }

        // Sheets 배치 → Views 배치 → 치수선 배치 → 주석 배치를 한 번에 실행, 완료 메시지는 마지막에 한 번만 표시
        private void PlaceAll()
        {
            var messages = new List<string>();

            messages.Add(CreateSheets());
            messages.Add(PlaceViews());
            messages.Add(PlaceDimensions());
            messages.Add(PlaceAnnotates());

            _refreshSheets?.Invoke();
            _dialogService.Info("펌프장 배치 완료", string.Join("\n", messages.Where(m => !string.IsNullOrEmpty(m))));
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

        private string PlaceViews()
        {
            // 선택창 없이 고정 템플릿 사용, 축척은 변경 안함(null)
            var templates = _useCase.GetViewTemplates();
            var planTemplateId = templates.FirstOrDefault(t => t.Name == "DH_평면도")?.Id ?? "";
            var sectionTemplateId = templates.FirstOrDefault(t => t.Name == "DH_단면도")?.Id ?? "";

            var result = _useCase.PlacePumpingStationViews(
                planTemplateId, sectionTemplateId,
                null, null);

            if (result.PlacedCount == 0 && result.NotFoundSheets.Count > 0)
                return $"Views 배치 실패: 매칭되는 뷰를 찾을 수 없습니다.\n{string.Join(", ", result.NotFoundSheets)}";

            var msg = $"뷰 {result.PlacedCount}개가 배치되었습니다.";
            if (result.NotFoundSheets.Count > 0)
                msg += $"\n매칭 실패: {string.Join(", ", result.NotFoundSheets)}";

            return msg;
        }

        private string PlaceDimensions()
        {
            // 선택창 없이 "DH_치수선" 고정 사용
            _useCase.PlacePumpingStationDimensions("DH_치수선");
            return "단면 시트에 치수선을 배치했습니다.";
        }

        // 뷰별 DH_ElementCode / DH_Part 화이트리스트 (필요 시 여기서 직접 수정)
        // 키: 뷰 이름("상부슬래브", "기초(유입부)") 또는 단면 뷰 공통 키("*")
        // 값이 null이거나 빈 배열이면 해당 속성은 필터링 없이 전체 허용
        private static readonly Dictionary<string, (IList<string> Codes, IList<string> Parts)> ViewTagFilters = new()
        {
            //Array.Empty<string>())
            //(new[] { "G1" }
            //null

            ["상부슬래브"]      = (new[] { "SO1", "SO2", "WO1", "WO2", "G1" , "PED1"         },    null),
            ["기초(유입부)"]    = (new[] { "SO1", "SO2", "WO1", "WO2", "AVW"                 },    null),
            ["A"]               = (new[] { "SO1", "SO2", "WO1", "WO2", "G1" , "AVW" , "PED1" },    null),
            ["B"]               = (new[] { "SO1", "SO2", "WO1", "WO2", "G1", "W3-1"          },    null),
            ["C"]               = (new[] { "SO1", "SO2", "WO1", "WO2"                        },    null),
            ["D"]               = (new[] { "SO1", "SO2", "WO1", "WO2"                        },    null),
            ["E"]               = (new[] { "SO1", "SO2", "WO1", "WO2"                        },    null),
            ["F"]               = (new[] { "SO1", "SO2", "WO1", "WO2"                        },    null),
            ["G"]               = (new[] { "G1"                                              },    null),
            ["H"]               = (new[] { "SO1", "SO2", "WO1", "WO2"                        },    null),
            ["I"]               = (new[] { "PED1"                                            },    Array.Empty<string>()),
            ["J"]               = (null,                                                           new[] { "와류방지벽" }),
            ["K"]               = (null,                                                           new[] { "와류방지벽" })
        };

        private string PlaceAnnotates()
        {
            // 선택창 없이 전체 태그 패밀리 사용, DH_ElementCode/DH_Part 필터는 위 ViewTagFilters 값 사용
            var tagFamilies = _useCase.GetAvailableTagFamilies();
            var allTagIds = tagFamilies.Select(t => t.Id).ToList();

            _useCase.ApplyPumpingStationAnnotations();
            _useCase.ApplyDHTags(allTagIds, ViewTagFilters);
            return "시트에 주석을 배치하였습니다.";
        }

        private string CreateSheets()
        {
            // 선택창 없이 "A1" 도곽 고정 사용
            var titleBlocks = _useCase.GetTitleBlocks();
            var titleBlock = titleBlocks.FirstOrDefault(t => t.DisplayName == "A1");

            var result = _useCase.CreatePumpingStationSheets(titleBlock?.Id);

            if (result.CreatedCount == 0 && result.HasDuplicates)
                return $"시트 생성 실패: 중복된 시트 번호가 있어 생성할 수 없습니다.\n{string.Join(", ", result.DuplicateSheetNumbers)}";

            if (result.CreatedCount == 0)
                return "시트 생성 실패: 도곽을 찾을 수 없거나 생성할 단면 뷰가 없습니다.";

            return $"시트 {result.CreatedCount}개가 생성되었습니다.";
        }
    }
}
