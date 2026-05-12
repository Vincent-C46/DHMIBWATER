using DHBIMWATER.Application.DTOs.Revit.Sheets;
using DHBIMWATER.Application.UseCases.Sheets;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Collections.Generic;
using System.Linq;


namespace DHBIMWATER.UI.ViewModels.Documentation
{
    public class DimensionViewModel : ViewModelBase
    {
        private DimensionMode _selectedDimensionMode = DimensionMode.AllObjects;
        public DimensionMode SelectedDimensionMode
        {
            get => _selectedDimensionMode;
            set
            {
                _selectedDimensionMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAllObjectsMode));
                OnPropertyChanged(nameof(IsSelectedObjectsMode));
            }
        }

        public bool IsAllObjectsMode
        {
            get => SelectedDimensionMode == DimensionMode.AllObjects;
            set
            {
                if (!value) return;
                SelectedDimensionMode = DimensionMode.AllObjects;
            }
        }

        public bool IsSelectedObjectsMode
        {
            get => SelectedDimensionMode == DimensionMode.SelectedObjects;
            set
            {
                if (!value) return;
                SelectedDimensionMode = DimensionMode.SelectedObjects;
            }
        }

        public IList<DimensionTypeDto> DimensionTypes { get; }

        private DimensionTypeDto _selectedDimensionType;
        public DimensionTypeDto SelectedDimensionType
        {
            get => _selectedDimensionType;
            set
            {
                _selectedDimensionType = value;
                OnPropertyChanged();
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

        public DimensionViewModel(IList<DimensionTypeDto> dimensionTypes)
        {
            DimensionTypes = dimensionTypes ?? new List<DimensionTypeDto>();
            SelectedDimensionType = DimensionTypes.FirstOrDefault();

            ConfirmCommand = new RelayCommand(_ => DialogResult = true);
            CancelCommand = new RelayCommand(_ => DialogResult = false);
        }

    }
}
