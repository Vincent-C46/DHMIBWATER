using System.Windows;
using DHBIMWATER.UI.ViewModels.Documentation.Sheets;

namespace DHBIMWATER.UI.Views.Documentation.Sheets
{
    public partial class WaterReservoirView : Window
    {
        public WaterReservoirView()
        {
            InitializeComponent();
        }

        public WaterReservoirView(WaterReservoirViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
