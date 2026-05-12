using DHBIMWATER.Application.DTOs.Revit.Families;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Families;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Collections.ObjectModel;

namespace DHBIMWATER.UI.ViewModels.Documentation.Families
{
    public class WebFamilyLibraryViewModel : ViewModelBase
    {
        private readonly IWebFamilyLibraryService _service;
        private readonly IDialogService _dialogService;

        private string _apiUrl = "https://localhost:5001/api/families";
        public string ApiUrl
        {
            get => _apiUrl;
            set => SetProperty(ref _apiUrl, value);
        }

        public ObservableCollection<WebFamilyLibraryItemDto> Families { get; } = new();

        private WebFamilyLibraryItemDto? _selectedFamily;
        public WebFamilyLibraryItemDto? SelectedFamily
        {
            get => _selectedFamily;
            set
            {
                if (SetProperty(ref _selectedFamily, value))
                    LoadFamilyCommand.RaiseCanExecuteChanged();
            }
        }

        private string _statusMessage = "URL을 입력한 뒤 목록을 불러오면 됩니다.";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        public RelayCommand LoadFamiliesCommand { get; }
        public RelayCommand LoadFamilyCommand { get; }
        public RelayCommand CloseCommand { get; }

        public WebFamilyLibraryViewModel(IWebFamilyLibraryService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;

            LoadFamiliesCommand = new RelayCommand(_ => LoadFamilies());
            LoadFamilyCommand = new RelayCommand(_ => LoadSelectedFamily(), _ => SelectedFamily != null);
            CloseCommand = new RelayCommand(_ => DialogResult = false);
        }

        private void LoadFamilies()
        {
            try
            {
                Families.Clear();

                var items = _service.GetFamilies(ApiUrl);
                foreach (var item in items)
                    Families.Add(item);

                StatusMessage = $"목록 {Families.Count}건을 불러왔습니다.";
            }
            catch (Exception ex)
            {
                StatusMessage = "목록 조회 실패";
                _dialogService.Warn("웹 패밀리 라이브러리", ex.Message);
            }
        }

        private void LoadSelectedFamily()
        {
            if (SelectedFamily == null)
                return;

            var result = _service.LoadFamily(SelectedFamily.DownloadUrl);
            StatusMessage = result.Message;

            if (result.Success)
            {
                var familyName = string.IsNullOrWhiteSpace(result.FamilyName) ? SelectedFamily.Name : result.FamilyName;
                _dialogService.Info("웹 패밀리 라이브러리", $"로드 완료: {familyName}");
                return;
            }

            _dialogService.Warn("웹 패밀리 라이브러리", result.Message);
        }
    }
}
