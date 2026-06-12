using System.Windows;
using DHBIMWATER.UI.ViewModels.Documentation.Sheets;

namespace DHBIMWATER.UI.Views.Documentation.Sheets
{
    public partial class AnnotateSelectView : Window
    {
        public AnnotateSelectView()
        {
            InitializeComponent();
        }

        public AnnotateSelectView(AnnotateSelectViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
