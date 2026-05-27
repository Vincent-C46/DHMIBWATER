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
                // existing == null  → New 모드
                // existing != null  → Edit 모드 (추후 구현)
                var mode     = existing is null ? QuantityInputMode.New : QuantityInputMode.Edit;
                var dialogVm = new ManualQuantityViewModel(mode);
                var dialog   = new ManualQuantityView(dialogVm) { Owner = this };

                if (dialog.ShowDialog() == true && dialogVm.ResultItem is not null)
                    vm.AddItem(dialogVm.ResultItem);
            };
        }
    }
}
