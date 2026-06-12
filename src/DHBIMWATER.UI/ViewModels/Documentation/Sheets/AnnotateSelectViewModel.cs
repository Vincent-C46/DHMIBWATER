using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using DHBIMWATER.Application.DTOs.Revit.Sheets;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Documentation.Sheets
{
    public class AnnotateSelectViewModel : ViewModelBase
    {
        public ObservableCollection<TagFamilyDto> AvailableTags { get; } = new();
        public ObservableCollection<TagFamilyDto> SelectedTags { get; } = new();
        public ObservableCollection<object> AvailableSelectedItems { get; } = new();
        public ObservableCollection<object> ChosenSelectedItems { get; } = new();

        public IList<string> SelectedTagFamilyIds => SelectedTags.Select(t => t.Id).ToList();

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; OnPropertyChanged(); }
        }

        public RelayCommand MoveToSelectedCommand { get; }
        public RelayCommand MoveToAvailableCommand { get; }
        public RelayCommand SelectAllCommand { get; }
        public RelayCommand DeselectAllCommand { get; }
        public RelayCommand ConfirmCommand { get; }
        public RelayCommand CancelCommand { get; }

        public AnnotateSelectViewModel(IList<TagFamilyDto> tagFamilies)
        {
            foreach (var tag in tagFamilies ?? new List<TagFamilyDto>())
                AvailableTags.Add(tag);

            MoveToSelectedCommand = new RelayCommand(_ => MoveItems(AvailableSelectedItems, AvailableTags, SelectedTags));
            MoveToAvailableCommand = new RelayCommand(_ => MoveItems(ChosenSelectedItems, SelectedTags, AvailableTags));
            SelectAllCommand = new RelayCommand(_ => MoveAll(AvailableTags, SelectedTags));
            DeselectAllCommand = new RelayCommand(_ => MoveAll(SelectedTags, AvailableTags));
            ConfirmCommand = new RelayCommand(_ => DialogResult = true);
            CancelCommand = new RelayCommand(_ => DialogResult = false);
        }

        private static void MoveItems(ObservableCollection<object> selectedItems, ObservableCollection<TagFamilyDto> from, ObservableCollection<TagFamilyDto> to)
        {
            var items = selectedItems.Cast<TagFamilyDto>().ToList();
            foreach (var item in items)
            {
                from.Remove(item);
                to.Add(item);
            }
            selectedItems.Clear();
        }

        private static void MoveAll(ObservableCollection<TagFamilyDto> from, ObservableCollection<TagFamilyDto> to)
        {
            foreach (var item in from.ToList())
            {
                from.Remove(item);
                to.Add(item);
            }
        }
    }
}
