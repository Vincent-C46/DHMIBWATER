using System.Windows;
using DHBIMWATER.UI.ViewModels.Modeling;
namespace DHBIMWATER.UI.Views.Modeling;
public partial class AlignmentFamilyPlacementView : Window
{
    public AlignmentFamilyPlacementView(AlignmentFamilyPlacementViewModel vm) { InitializeComponent(); DataContext = vm; vm.CloseAction = Close; }
}
