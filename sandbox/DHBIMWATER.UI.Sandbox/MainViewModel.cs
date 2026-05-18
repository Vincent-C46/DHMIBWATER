using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using DHBIMWATER.UI.Views.GuideLine;
using DHBIMWATER.UI.Views.Modeling;
using DHBIMWATER.UI.Views.Quantity;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;

namespace DHBIMWATER.UI.Sandbox
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;

        public ICommand OpenModeling1ViewCommand { get; }
        public ICommand OpenGuideLineViewCommand { get; }
        public ICommand OpenWaterTankViewCommand { get; }
        public ICommand OpenPumpingStationViewCommand { get; }
        public ICommand OpenQuantityViewCommand { get; }

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            OpenModeling1ViewCommand = new RelayCommand(OpenModeling1View);
            OpenGuideLineViewCommand = new RelayCommand(OpenGuideLineView);
            OpenWaterTankViewCommand = new RelayCommand(OpenWaterTankView);
            OpenPumpingStationViewCommand = new RelayCommand(OpenPumpingStationView);
            OpenQuantityViewCommand = new RelayCommand(OpenQuantityView);
        }


        private void OpenModeling1View(object? obj)
        {
            var view = _serviceProvider.GetRequiredService<Modeling1View>();
            view.ShowDialog();

        }

        private void OpenGuideLineView(object? obj)
        {
            var view = _serviceProvider.GetRequiredService<GuideLineView>();
            view.ShowDialog();
        }

        private void OpenWaterTankView(object? obj)
        {
            var view = _serviceProvider.GetRequiredService<WaterTankView>();
            view.ShowDialog();
        }
                private void OpenPumpingStationView(object? obj)
        {
            var view = _serviceProvider.GetRequiredService<PumpingStationView>();
            view.ShowDialog();
        }

        private void OpenQuantityView(object? obj)
        {
            var view = _serviceProvider.GetRequiredService<QuantityView>();
            view.ShowDialog();
        }
    }
}
