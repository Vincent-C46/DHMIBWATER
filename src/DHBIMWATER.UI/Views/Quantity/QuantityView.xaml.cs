using DHBIMWATER.UI.ViewModels.Quantity;
using System.Windows;

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
    }
}
