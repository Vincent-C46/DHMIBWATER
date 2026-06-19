using System.Windows;
using DHBIMWATER.UI.ViewModels.Documentation.Sheets;

namespace DHBIMWATER.UI.Views.Documentation.Sheets
{
    public partial class ViewTemplateSelectView : Window
    {
        public ViewTemplateSelectView(ViewTemplateSelectViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
