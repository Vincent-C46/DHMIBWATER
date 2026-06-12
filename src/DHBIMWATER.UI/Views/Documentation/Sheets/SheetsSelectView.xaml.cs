using System.Windows;
using DHBIMWATER.UI.ViewModels.Documentation.Sheets;

namespace DHBIMWATER.UI.Views.Documentation.Sheets
{
    public partial class SheetsSelectView : Window
    {
        public SheetsSelectView()
        {
            InitializeComponent();
        }

        public SheetsSelectView(SheetsSelectViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
