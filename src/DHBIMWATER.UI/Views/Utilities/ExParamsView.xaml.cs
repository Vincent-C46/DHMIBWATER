using System;
using System.Windows;
using DHBIMWATER.UI.ViewModels.GuideLine;
using DHBIMWATER.UI.ViewModels.Utilities;

namespace DHBIMWATER.UI.Views.Utilities
{
    public partial class ExParamsView : Window
    {
        public ExParamsView(ExParamsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
        }
    }
}
