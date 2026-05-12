using System.Collections.ObjectModel;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.UI.Base;

using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Documentation
{
    public class AddSheetsViewModel : ViewModelBase
    {
        public ObservableCollection<TitleBlockDto> TitleBlocks { get; } = new();

        private TitleBlockDto _selectedTitleBlock;
        public TitleBlockDto SelectedTitleBlock
        {
            get => _selectedTitleBlock;
            set { _selectedTitleBlock = value; OnPropertyChanged(); }
        }


        private string _sheetNumber;
        public string SheetNumber
        {
            get => _sheetNumber;
            set { _sheetNumber = value; OnPropertyChanged(); }
        }

        private string _sheetName;
        public string SheetName
        {
            get => _sheetName;
            set { _sheetName = value; OnPropertyChanged(); }
        }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; OnPropertyChanged(); }
        }

        public RelayCommand ConfirmCommand { get; }
        public RelayCommand CancelCommand { get; }

        public AddSheetsViewModel()
        {
            ConfirmCommand = new RelayCommand(_ => DialogResult = true);
            CancelCommand = new RelayCommand(_ => DialogResult = false);
        }
        public AddSheetsViewModel(IList<TitleBlockDto> titleBlocks)
        {
            foreach (var tb in titleBlocks)
                TitleBlocks.Add(tb);

            ConfirmCommand = new RelayCommand(_ => DialogResult = true);
            CancelCommand = new RelayCommand(_ => DialogResult = false);
        }

    }
}
