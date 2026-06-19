using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Documentation.Sheets
{
    public class SheetsSelectViewModel : ViewModelBase
    {
        public ObservableCollection<TitleBlockDto> TitleBlocks { get; }

        private TitleBlockDto _selectedTitleBlock;
        public TitleBlockDto SelectedTitleBlock
        {
            get => _selectedTitleBlock;
            set { _selectedTitleBlock = value; OnPropertyChanged(); }
        }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; OnPropertyChanged(); }
        }

        public RelayCommand ConfirmCommand { get; }
        public RelayCommand CancelCommand { get; }

        public SheetsSelectViewModel(IList<TitleBlockDto> titleBlocks)
        {
            TitleBlocks = new ObservableCollection<TitleBlockDto>(titleBlocks ?? new List<TitleBlockDto>());
            SelectedTitleBlock = TitleBlocks.FirstOrDefault(tb =>
                tb.DisplayName != null && tb.DisplayName.Contains("A1")) ?? TitleBlocks.FirstOrDefault();

            ConfirmCommand = new RelayCommand(_ => DialogResult = true, _ => SelectedTitleBlock != null);
            CancelCommand = new RelayCommand(_ => DialogResult = false);
        }
    }
}
