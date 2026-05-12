using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using DHBIMWATER.Application.DTOs.Revit.Sheet;


namespace DHBIMWATER.UI.ViewModels.Documentation
{
    public class ViewManageViewModel : ViewModelBase    
    {
        public RelayCommand AddViewCommand { get; }
        public RelayCommand ChangeViewCommand { get; }
        public ObservableCollection<ViewRow> Views { get; } = new();
        public ObservableCollection<string> ViewTypes { get; } = new();

        private readonly List<ViewRow> _allViews = new();

        public ObservableCollection<string> ViewScaleOptions { get; } = new();
        public ObservableCollection<string> VisualStyleOptions { get; } = new();
        public ObservableCollection<string> SheetFormOptions { get; } = new();
        public ObservableCollection<string> BulkVisualStyleOptions { get; } = new();
        public ObservableCollection<string> BulkScaleOptions { get; } = new();
        public ObservableCollection<string> BulkSheetFormOptions { get; } = new();




        private string _selectedScaleOption = "1:100";
        public string SelectedScaleOption
        {
            get => _selectedScaleOption;
            set
            {
                if (_selectedScaleOption == value) return;
                _selectedScaleOption = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCustomScaleEnabled));

                if (SelectedView == null) return;

                if (value != "사용자 입력")
                {
                    SelectedView.ScaleText = value;
                    if (TryParseScale(value, out var s)) SelectedView.Scale = s;
                }
            }
        }

        private string _customScaleValue;
        public string CustomScaleValue
        {
            get => _customScaleValue;
            set
            {
                if (_customScaleValue == value) return;
                _customScaleValue = value;
                OnPropertyChanged();

                if (SelectedView == null) return;
                if (!IsCustomScaleEnabled) return;

                if (int.TryParse(value, out var s) && s > 0)
                {
                    SelectedView.Scale = s;
                    SelectedView.ScaleText = $"1:{s}";
                }
            }
        }

        public bool IsCustomScaleEnabled => SelectedScaleOption == "사용자 입력";



        private ViewRow _selectedView;
        public ViewRow SelectedView
        {
            get => _selectedView;
            set { _selectedView = value; OnPropertyChanged();
                if (_selectedView == null) return;

                var txt = string.IsNullOrWhiteSpace(_selectedView.ScaleText)
                    ? $"1:{_selectedView.Scale}"
                    : _selectedView.ScaleText;

                if (ViewScaleOptions.Contains(txt))
                {
                    SelectedScaleOption = txt;
                    CustomScaleValue = "";
                }
                else
                {
                    SelectedScaleOption = "사용자 입력";
                    CustomScaleValue = _selectedView.Scale > 0 ? _selectedView.Scale.ToString() : "";
                }
            }
        }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; OnPropertyChanged(); }
        }

        private string _no;
        public string No
        {
            get => _no;
            set { _no = value; OnPropertyChanged(); }
        }

        private string _sheetName;
        public string SheetName
        {
            get => _sheetName;
            set { _sheetName = value; OnPropertyChanged(); }
        }

        private string _selectedViewType = "All";
        public string SelectedViewType
        {
            get => _selectedViewType;
            set
            {
                if (_selectedViewType == value) return;
                _selectedViewType = value;
                OnPropertyChanged();
                ApplyTypeFilter();
            }
        }

        public RelayCommand ConfirmCommand { get; }
        public RelayCommand CancelCommand { get; }

        public ViewManageViewModel(IEnumerable<ViewInfoDto> views)
        {
            VisualStyleOptions.Add("와이어프레임");
            VisualStyleOptions.Add("은선");
            VisualStyleOptions.Add("음영처리");
            VisualStyleOptions.Add("일관된 색상");
            VisualStyleOptions.Add("텍스처");
            VisualStyleOptions.Add("사실적");

            ViewScaleOptions.Add("1:1");
            ViewScaleOptions.Add("1:2");
            ViewScaleOptions.Add("1:5");
            ViewScaleOptions.Add("1:10");
            ViewScaleOptions.Add("1:20");
            ViewScaleOptions.Add("1:25");
            ViewScaleOptions.Add("1:50");
            ViewScaleOptions.Add("1:100");
            ViewScaleOptions.Add("1:200");
            ViewScaleOptions.Add("1:500");
            ViewScaleOptions.Add("1:1000");
            ViewScaleOptions.Add("1:2000");
            ViewScaleOptions.Add("1:5000");
            ViewScaleOptions.Add("사용자 입력");

            SheetFormOptions.Add("없음");
            SheetFormOptions.Add("일반도");
            SheetFormOptions.Add("구조도");
            SheetFormOptions.Add("KeyMap");


            BulkVisualStyleOptions.Add("All");
            foreach (var s in VisualStyleOptions) BulkVisualStyleOptions.Add(s);

            BulkScaleOptions.Add("All");
            foreach (var s in ViewScaleOptions.Where(x => x != "사용자 입력")) BulkScaleOptions.Add(s);

            BulkSheetFormOptions.Add("All");
            foreach (var s in SheetFormOptions) BulkSheetFormOptions.Add(s);


            int index = 1;

            foreach (var v in views)
            {
                _allViews.Add(new ViewRow
                {
                    No = index.ToString(),
                    ViewId = v.ViewId,
                    ViewName = v.ViewName,
                    ViewType = v.ViewType,
                    SheetName = v.TitleOnSheet ?? "",
                    Scale = v.Scale,
                    ScaleText = v.ScaleText,
                    ScaleInput = v.Scale > 0 ? v.Scale.ToString() : "",
                    VisualStyle = v.VisualStyle,
                    SheetForm = string.IsNullOrWhiteSpace(v.SheetForm) ? "없음" : v.SheetForm
                });

                index++;
            }


            BuildTypeList();
            ApplyTypeFilter();

            if (Views.Count > 0)
                SelectedView = Views[0];

            ConfirmCommand = new RelayCommand(_ =>
            {
                if (SelectedView == null && Views.Count > 0)
                    SelectedView = Views[0];

                DialogResult = true;
            }, _ => Views.Count > 0);

            CancelCommand = new RelayCommand(_ => DialogResult = false);
        }

        private void BuildTypeList()
        {
            ViewTypes.Clear();
            ViewTypes.Add("All");

            foreach (var t in _allViews
                .Select(x => x.ViewType)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x))
            {
                ViewTypes.Add(t);
            }

            SelectedViewType = "All";
        }

        private void ApplyTypeFilter()
        {
            Views.Clear();

            IEnumerable<ViewRow> query = _allViews;
            if (!string.Equals(SelectedViewType, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(v => string.Equals(v.ViewType, SelectedViewType, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var v in query)
                Views.Add(v);

            if (SelectedView != null && !Views.Contains(SelectedView))
                SelectedView = null;
        }
        private static bool TryParseScale(string text, out int scale)
        {
            scale = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (!text.StartsWith("1:")) return false;
            return int.TryParse(text.Substring(2), out scale) && scale > 0;
        }

        private void ApplyBulkVisualStyle()
        {
            if (SelectedBulkVisualStyle == "All") return;
            foreach (var row in Views) row.VisualStyle = SelectedBulkVisualStyle;
        }

        private void ApplyBulkScale()
        {
            if (SelectedBulkScale == "All") return;
            foreach (var row in Views) row.ScaleText = SelectedBulkScale;
        }

        private void ApplyBulkSheetForm()
        {
            if (SelectedBulkSheetForm == "All") return;
            foreach (var row in Views) row.SheetForm = SelectedBulkSheetForm;
        }
        private string _selectedBulkVisualStyle = "All";
        public string SelectedBulkVisualStyle
        {
            get => _selectedBulkVisualStyle;
            set
            {
                if (_selectedBulkVisualStyle == value) return;
                _selectedBulkVisualStyle = value;
                OnPropertyChanged();
                ApplyBulkVisualStyle();
            }
        }

        private string _selectedBulkScale = "All";
        public string SelectedBulkScale
        {
            get => _selectedBulkScale;
            set
            {
                if (_selectedBulkScale == value) return;
                _selectedBulkScale = value;
                OnPropertyChanged();
                ApplyBulkScale();
            }
        }

        private string _selectedBulkSheetForm = "All";
        public string SelectedBulkSheetForm
        {
            get => _selectedBulkSheetForm;
            set
            {
                if (_selectedBulkSheetForm == value) return;
                _selectedBulkSheetForm = value;
                OnPropertyChanged();
                ApplyBulkSheetForm();
            }
        }


        public class ViewRow : ViewModelBase
        {
            public string ViewId { get; set; }
            public string ViewName { get; set; }
            public string ViewType { get; set; }
            public string No { get; set; }
            public string SheetName { get; set; }

            private int _scale;
            public int Scale
            {
                get => _scale;
                set { _scale = value; OnPropertyChanged(); }
            }

            private string _scaleText;
            public string ScaleText
            {
                get => _scaleText;
                set
                {
                    if (_scaleText == value) return;
                    _scaleText = value;
                    OnPropertyChanged();

                    IsCustomScaleEnabled = string.Equals(value, "사용자 입력", StringComparison.Ordinal);

                    if (!IsCustomScaleEnabled && TryParseScale(value, out var s))
                    {
                        Scale = s;
                        ScaleInput = s.ToString();
                    }
                }
            }

            private string _scaleInput;
            public string ScaleInput
            {
                get => _scaleInput;
                set
                {
                    if (_scaleInput == value) return;
                    _scaleInput = value;
                    OnPropertyChanged();

                    if (IsCustomScaleEnabled && int.TryParse(value, out var s) && s > 0)
                        Scale = s;
                }
            }

            private bool _isCustomScaleEnabled;
            public bool IsCustomScaleEnabled
            {
                get => _isCustomScaleEnabled;
                set { _isCustomScaleEnabled = value; OnPropertyChanged(); }
            }

            private static bool TryParseScale(string text, out int scale)
            {
                scale = 0;
                if (string.IsNullOrWhiteSpace(text)) return false;
                if (!text.StartsWith("1:")) return false;
                return int.TryParse(text.Substring(2), out scale) && scale > 0;
            }
            
            private string _visualStyle;
            public string VisualStyle
            {
                get => _visualStyle;
                set { _visualStyle = value; OnPropertyChanged(); }
            }
            private string _sheetForm;
            public string SheetForm
            {
                get => _sheetForm;
                set { _sheetForm = value; OnPropertyChanged(); }
            }                       
        }
    }
}
