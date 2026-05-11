using System.Windows;
using DHBIMWATER.UI.ViewModels.Documentation.Sheets;

namespace DHBIMWATER.UI.Views.Documentation.Sheets
{
    public partial class DimensionTypeView : Window
    {
        public DimensionTypeView()
        {
            InitializeComponent();
        }

        public DimensionTypeView(DImensionTypeViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
