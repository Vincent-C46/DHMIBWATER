using DHBIMWATER.Application.DTOs.GuildeLine;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.GuideLine
{
    public class GuideLineViewModel : ViewModelBase
    {
        // 필드 및 속성 설정 - OnPropertyChanged 구현 포함
        private IDialogService _dialogService;
        private IGuideLineService _guideService;


        private string _textBoxContent = string.Empty;
        private bool _isCheckBoxChecked;
        private RevitFamilyDto _selectedFamily;

        public string TextBoxContent
        {
            get => _textBoxContent;
            set
            {
                if (_textBoxContent != value)
                {
                    _textBoxContent = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCheckBoxChecked
        {
            get => _isCheckBoxChecked;
            set
            {
                if (_isCheckBoxChecked != value)
                {
                    _isCheckBoxChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public RevitFamilyDto SelectedFamily
        {
            get => _selectedFamily;
            set
            {
                if (_selectedFamily != value)
                {
                    _selectedFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        // ObservableCollection 예제
        public ObservableCollection<RevitFamilyDto> Families { get; set; }
        public ObservableCollection<RevitColumnDto> Columns { get; set; }

        // Button에 바인딩할 ICommand 예제
        public ICommand ButtonCommand { get; }

        public GuideLineViewModel(IDialogService dialogService, IGuideLineService guideService)
        {
            _dialogService = dialogService;
            _guideService = guideService;

            Families = new ObservableCollection<RevitFamilyDto>();
            Columns = new ObservableCollection<RevitColumnDto>();

            ButtonCommand = new RelayCommand(OnButtonClicked);

            Initialize();
        }

        
        private void OnButtonClicked(object? obj)
        {
            _dialogService.Info("Button clicked!", "Info");
            TextBoxContent = "Button was clicked!";
            IsCheckBoxChecked = !IsCheckBoxChecked;
        }

        private void Initialize()
        {
            // Familys 컬렉션 초기화
            Families.Clear();
            foreach(RevitFamilyDto family in _guideService.GetFamilies())
                Families.Add(family);

            SelectedFamily = Families[2];

            // Columns 컬렉션 초기화
            Columns.Clear();
            foreach(RevitColumnDto column in _guideService.GetColumn())
                Columns.Add(column);
        }

    }
}
