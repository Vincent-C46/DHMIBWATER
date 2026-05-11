using System;
using System.Collections.ObjectModel;
using System.Linq;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.Application.UseCases.Parameter;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Utilities
{
    public class ExParamsViewModel : ViewModelBase
    {
        private readonly IExportParamsUseCase _useCase;

        public ObservableCollection<CategoryInfo> Categories { get; } = new();
        public ObservableCollection<ParamItem> Parameters { get; } = new();
        public ObservableCollection<ParamItem> SelectedParameterItems { get; } = new();

        public RelayCommand SelectAllCommand { get; }
        public RelayCommand DeselectAllCommand { get; }
        public RelayCommand ConfirmCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand ImportCommand { get; }

        public event Action<bool?> RequestClose;
        public event Action<string> ImportRequested;

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; OnPropertyChanged(); }
        }

        private CategoryInfo _selectedCategory;
        public CategoryInfo SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory == value) return;
                _selectedCategory = value;
                OnPropertyChanged();
                LoadParameters();
            }
        }

        public IList<string> SelectedParameters =>
            Parameters.Where(p => p.IsChecked).Select(p => p.Name).ToList();

        private bool _isBulkUpdating;

        public ExParamsViewModel(IExportParamsUseCase useCase)
        {
            _useCase = useCase;

            SelectAllCommand = new RelayCommand(_ => SetAll(true));
            DeselectAllCommand = new RelayCommand(_ => SetAll(false));
            ConfirmCommand = new RelayCommand(_ => DialogResult = true);
            CancelCommand = new RelayCommand(_ => DialogResult = false);
            ImportCommand = new RelayCommand(_ => Import());

            LoadCategories();
        }

        private void LoadCategories()
        {
            Categories.Clear();
            foreach (var c in _useCase.GetCategories())
                Categories.Add(c);
        }

        private void LoadParameters()
        {
            Parameters.Clear();
            SelectedParameterItems.Clear();

            if (SelectedCategory == null) return;

            var list = _useCase.GetParameters(SelectedCategory.Key);
            foreach (var p in list)
            {
                var item = new ParamItem { Name = p, IsChecked = false };
                item.IsCheckedChanged += OnItemCheckedChanged;
                Parameters.Add(item);
            }
        }

        private void SetAll(bool value)
        {
            _isBulkUpdating = true;
            try
            {
                foreach (var p in Parameters)
                    p.IsChecked = value;
            }
            finally
            {
                _isBulkUpdating = false;
            }
        }

        private void OnItemCheckedChanged(ParamItem source, bool isChecked)
        {
            if (_isBulkUpdating) return;
            if (SelectedParameterItems == null || SelectedParameterItems.Count <= 1) return;
            if (!SelectedParameterItems.Contains(source)) return;

            _isBulkUpdating = true;
            try
            {
                foreach (var item in SelectedParameterItems)
                {
                    if (ReferenceEquals(item, source)) continue;
                    item.IsChecked = isChecked;
                }
            }
            finally
            {
                _isBulkUpdating = false;
            }
        }

        private void Import()
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import",
                Filter = "Excel 파일 (*.xlsx)|*.xlsx|CSV 파일 (*.csv)|*.csv",
                CheckFileExists = true
            };

            if (ofd.ShowDialog() != true) return;

            ImportRequested?.Invoke(ofd.FileName);
        }
    }

    public class ParamItem : ViewModelBase
    {
        public string Name { get; set; }

        public event Action<ParamItem, bool> IsCheckedChanged;

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                OnPropertyChanged();
                IsCheckedChanged?.Invoke(this, _isChecked);
            }
        }
    }
}
