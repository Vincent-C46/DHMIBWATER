using DHBIMWATER.Core.Quantity;
using DHBIMWATER.UI.ViewModels.Quantity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DHBIMWATER.UI.Views.Quantity
{
    public partial class QuantityView : Window
    {
        public QuantityViewModel ViewModel => (QuantityViewModel)DataContext;

        public QuantityView(QuantityViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            vm.ManualInputRequested += (_, existing) =>
            {
                var dialogVm = new ManualQuantityViewModel(QuantityInputMode.New, null, vm.MeasureService);
                var dialog = new ManualQuantityView(dialogVm) { Owner = this };

                dialogVm.CloseRequested += ok =>
                {
                    if (ok && dialogVm.ResultItem is not null)
                        vm.AddItem(dialogVm.ResultItem);
                };
                dialog.Show();
            };

            vm.EditItemRequested += (_, args) =>
            {
                var (item, index) = args;
                var dialogVm = new ManualQuantityViewModel(QuantityInputMode.Edit, item, vm.MeasureService);
                var dialog = new ManualQuantityView(dialogVm) { Owner = this };

                dialogVm.CloseRequested += ok =>
                {
                    if (ok && dialogVm.ResultItem is not null)
                        vm.ReplaceItem(index, dialogVm.ResultItem);
                };
                dialog.Show();
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
