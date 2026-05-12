using System.Collections.ObjectModel;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Collections.Generic;
using System.Linq;
using DHBIMWATER.Application.DTOs.Revit.Sheets;

namespace DHBIMWATER.UI.ViewModels.Documentation.Sheets
{
    public class DImensionTypeViewModel : ViewModelBase
    {
        public ObservableCollection<DimensionTypeDto> DimensionTypes { get; }

        private DimensionTypeDto _selectedDimensionType;
        public DimensionTypeDto SelectedDimensionType
        {
            get => _selectedDimensionType;
            set { _selectedDimensionType = value; OnPropertyChanged(); }
        }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; OnPropertyChanged(); }
        }

        public RelayCommand ConfirmCommand { get; }
        public RelayCommand CancelCommand { get; }

        public DImensionTypeViewModel(IList<DimensionTypeDto> dimensionTypes)
        {
            DimensionTypes = new ObservableCollection<DimensionTypeDto>(dimensionTypes ?? new List<DimensionTypeDto>());
            SelectedDimensionType = DimensionTypes.FirstOrDefault();

            ConfirmCommand = new RelayCommand(_ => DialogResult = true, _ => SelectedDimensionType != null);
            CancelCommand = new RelayCommand(_ => DialogResult = false);
        }
    }
}
