using DHBIMWATER.Core.Quantity;
using DHBIMWATER.UI.ViewModels.Quantity;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DHBIMWATER.UI.Views.Quantity
{
    public partial class QuantityView : Window
    {
        public QuantityView(QuantityViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            // 수동 항목 추가 다이얼로그 연결
            vm.ManualInputRequested += (_, existing) =>
            {
                // 항상 New 모드 (추가만 가능)
                var dialogVm = new ManualQuantityViewModel();
                var dialog   = new ManualQuantityView(dialogVm) { Owner = this };

                if (dialog.ShowDialog() == true && dialogVm.ResultItem is not null)
                    vm.AddItem(dialogVm.ResultItem);
            };

            // 항목 수정 다이얼로그 연결
            vm.EditItemRequested += (_, args) =>
            {
                var (item, index) = args;
                var dialogVm = new ManualQuantityViewModel(QuantityInputMode.Edit, item);
                var dialog = new ManualQuantityView(dialogVm) { Owner = this };

                if (dialog.ShowDialog() == true && dialogVm.ResultItem is not null)
                    vm.ReplaceItem(index, dialogVm.ResultItem);
            };
        }

        private void OnItemsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not QuantityViewModel vm) return;
            if (sender is not DataGrid dataGrid) return;

            var selected = dataGrid.SelectedItems
                .OfType<QuantityItem>()
                .ToList();

            vm.UpdateSelectedItems(selected);
        }

        private void OnDataGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete) return;
            if (DataContext is not QuantityViewModel vm) return;

            if (vm.DeleteItemCommand.CanExecute(null))
            {
                vm.DeleteItemCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
