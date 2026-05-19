using DHBIMWATER.Application.UseCases.Sheets;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Documentation.Sheets
{
    public class DimensionDirectionViewModel : ViewModelBase
    {
        private bool _isTopSelected = true;
        private bool _isBottomSelected = true;
        private bool _isLeftSelected = true;
        private bool _isRightSelected = true;
        private bool _isIncludeOverall = false;

        public bool IsTopSelected
        {
            get => _isTopSelected;
            set { _isTopSelected = value; OnPropertyChanged(); }
        }
        public bool IsBottomSelected
        {
            get => _isBottomSelected;
            set { _isBottomSelected = value; OnPropertyChanged(); }
        }
        public bool IsLeftSelected
        {
            get => _isLeftSelected;
            set { _isLeftSelected = value; OnPropertyChanged(); }
        }
        public bool IsRightSelected
        {
            get => _isRightSelected;
            set { _isRightSelected = value; OnPropertyChanged(); }
        }
        public bool IsIncludeOverall
        {
            get => _isIncludeOverall;
            set { _isIncludeOverall = value; OnPropertyChanged(); }
        }

        public DimensionSide SelectedSides
        {
            get
            {
                var sides = DimensionSide.None;
                if (IsTopSelected)    sides |= DimensionSide.Top;
                if (IsBottomSelected) sides |= DimensionSide.Bottom;
                if (IsLeftSelected)   sides |= DimensionSide.Left;
                if (IsRightSelected)  sides |= DimensionSide.Right;
                return sides == DimensionSide.None ? DimensionSide.All : sides;
            }
        }

        public RelayCommand ConfirmCommand { get; }
        public RelayCommand CancelCommand { get; }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; OnPropertyChanged(); }
        }

        public DimensionDirectionViewModel()
        {
            ConfirmCommand = new RelayCommand(_ => DialogResult = true);
            CancelCommand = new RelayCommand(_ => DialogResult = false);
        }
    }
}
