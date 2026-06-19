using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DHBIMWATER.Application.DTOs.Revit.Sheets;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Documentation.Sheets
{
    public class ViewTemplateSelectViewModel : ViewModelBase
    {
        private const string PlanTemplateDefaultName = "DH_평면도";
        private const string SectionTemplateDefaultName = "DH_단면도";

        private static readonly string[] PlanViewTypes = { "FloorPlan", "CeilingPlan", "AreaPlan", "EngineeringPlan" };
        private static readonly string[] SectionViewTypes = { "Section", "Detail", "Elevation" };

        public ObservableCollection<ViewTemplateOption> PlanTemplates { get; } = new();
        public ObservableCollection<ViewTemplateOption> SectionTemplates { get; } = new();

        private ViewTemplateOption _selectedPlanTemplate;
        public ViewTemplateOption SelectedPlanTemplate
        {
            get => _selectedPlanTemplate;
            set { _selectedPlanTemplate = value; OnPropertyChanged(); }
        }

        private ViewTemplateOption _selectedSectionTemplate;
        public ViewTemplateOption SelectedSectionTemplate
        {
            get => _selectedSectionTemplate;
            set { _selectedSectionTemplate = value; OnPropertyChanged(); }
        }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; OnPropertyChanged(); }
        }

        private static readonly string[] ScaleOptionValues =
        {
            "변경 안함",
            "1:1", "1:2", "1:5", "1:10", "1:20", "1:25", "1:50",
            "1:100", "1:200", "1:500", "1:1000", "1:2000", "1:5000",
            "사용자 입력"
        };

        public ObservableCollection<string> ScaleOptions { get; } = new(ScaleOptionValues);

        private string _selectedPlanScaleOption = "변경 안함";
        public string SelectedPlanScaleOption
        {
            get => _selectedPlanScaleOption;
            set
            {
                if (_selectedPlanScaleOption == value) return;
                _selectedPlanScaleOption = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPlanScaleCustomEnabled));
            }
        }

        private string _planScaleCustomValue;
        public string PlanScaleCustomValue
        {
            get => _planScaleCustomValue;
            set { _planScaleCustomValue = value; OnPropertyChanged(); }
        }

        public bool IsPlanScaleCustomEnabled => SelectedPlanScaleOption == "사용자 입력";

        private string _selectedSectionScaleOption = "변경 안함";
        public string SelectedSectionScaleOption
        {
            get => _selectedSectionScaleOption;
            set
            {
                if (_selectedSectionScaleOption == value) return;
                _selectedSectionScaleOption = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSectionScaleCustomEnabled));
            }
        }

        private string _sectionScaleCustomValue;
        public string SectionScaleCustomValue
        {
            get => _sectionScaleCustomValue;
            set { _sectionScaleCustomValue = value; OnPropertyChanged(); }
        }

        public bool IsSectionScaleCustomEnabled => SelectedSectionScaleOption == "사용자 입력";

        // "변경 안함" → null(기존 값 유지), "사용자 입력" → 입력값, 그 외 → "1:N"의 N
        public int? PlanScale => ResolveScale(SelectedPlanScaleOption, PlanScaleCustomValue);
        public int? SectionScale => ResolveScale(SelectedSectionScaleOption, SectionScaleCustomValue);

        private static int? ResolveScale(string option, string customValue)
        {
            if (string.IsNullOrEmpty(option) || option == "변경 안함") return null;

            if (option == "사용자 입력")
                return int.TryParse(customValue, out var c) && c > 0 ? c : (int?)null;

            if (option.StartsWith("1:") && int.TryParse(option.Substring(2), out var s) && s > 0)
                return s;

            return null;
        }

        public RelayCommand ConfirmCommand { get; }
        public RelayCommand CancelCommand { get; }

        public ViewTemplateSelectViewModel(IEnumerable<ViewTemplateDto> templates)
        {
            var none = new ViewTemplateOption("", "없음");

            PlanTemplates.Add(none);
            SectionTemplates.Add(none);

            foreach (var t in templates.Where(t => PlanViewTypes.Contains(t.ViewType)).OrderBy(t => t.Name))
                PlanTemplates.Add(new ViewTemplateOption(t.Id, t.Name));

            foreach (var t in templates.Where(t => SectionViewTypes.Contains(t.ViewType)).OrderBy(t => t.Name))
                SectionTemplates.Add(new ViewTemplateOption(t.Id, t.Name));

            SelectedPlanTemplate = PlanTemplates.FirstOrDefault(t =>
                t.Name.Equals(PlanTemplateDefaultName, StringComparison.OrdinalIgnoreCase)) ?? none;

            SelectedSectionTemplate = SectionTemplates.FirstOrDefault(t =>
                t.Name.Equals(SectionTemplateDefaultName, StringComparison.OrdinalIgnoreCase)) ?? none;

            ConfirmCommand = new RelayCommand(_ => DialogResult = true);
            CancelCommand = new RelayCommand(_ => DialogResult = false);
        }
    }

    public class ViewTemplateOption
    {
        public ViewTemplateOption(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; }
        public string Name { get; }
    }
}
