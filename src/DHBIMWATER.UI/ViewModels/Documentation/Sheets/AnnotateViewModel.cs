using DHBIMWATER.Application.UseCases.Sheets;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Documentation.Sheets
{
    public class AnnotateViewModel : ViewModelBase
    {
        private DimensionMode _selectedAnnotateMode = DimensionMode.SelectedObjects;
        public DimensionMode SelectedAnnotateMode
        {
            get => _selectedAnnotateMode;
            set
            {
                _selectedAnnotateMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAllObjectsMode));
                OnPropertyChanged(nameof(IsSelectedObjectsMode));
            }
        }

        public bool IsAllObjectsMode
        {
            get => SelectedAnnotateMode == DimensionMode.AllObjects;
            set
            {
                if (!value) return;
                SelectedAnnotateMode = DimensionMode.AllObjects;
            }
        }

        public bool IsSelectedObjectsMode
        {
            get => SelectedAnnotateMode == DimensionMode.SelectedObjects;
            set
            {
                if (!value) return;
                SelectedAnnotateMode = DimensionMode.SelectedObjects;
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

        public AnnotateViewModel()
        {
            ConfirmCommand = new RelayCommand(_ => DialogResult = true);
            CancelCommand = new RelayCommand(_ => DialogResult = false);
        }
    }
}
