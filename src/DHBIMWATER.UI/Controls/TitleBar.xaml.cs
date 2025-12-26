using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DHBIMWATER.UI.Controls
{
    /// <summary>
    /// TitleBar.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TitleBar : UserControl
    {
        Window _parentWindow;

        public TitleBar()
        {
            InitializeComponent();
            this.Loaded += TitleBar_Loaded;
        }

        private void TitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = Window.GetWindow(this)!;
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            if (_parentWindow.WindowState != WindowState.Minimized)
                _parentWindow.WindowState = WindowState.Minimized;
            else
                _parentWindow.WindowState = WindowState.Normal;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.Close();
        }

        private void MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if(_parentWindow == null)
                    _parentWindow = Window.GetWindow(this)!;
            }

            if (_parentWindow.WindowState == WindowState.Maximized) return;

            _parentWindow.DragMove();
        }
    }
}
