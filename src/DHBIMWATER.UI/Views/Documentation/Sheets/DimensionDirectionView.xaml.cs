using System.Windows;
using DHBIMWATER.UI.ViewModels.Documentation.Sheets;

namespace DHBIMWATER.UI.Views.Documentation.Sheets
{
    public partial class DimensionDirectionView : Window
    {
        public DimensionDirectionView(DimensionDirectionViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
