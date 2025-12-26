using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using DHBIMWATER.UI.Views.GuideLine;
using DHBIMWATER.UI.Views.Modeling;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;

namespace DHBIMWATER.UI.Sandbox
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;

        public ICommand OpenModeling1ViewCommand { get; }
        public ICommand OpenGuideLineViewCommand { get; }

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            OpenModeling1ViewCommand = new RelayCommand(OpenModeling1View);
            OpenGuideLineViewCommand = new RelayCommand(OpenGuideLineView);
        }

        private void OpenGuideLineView(object? obj)
        {
            var view = _serviceProvider.GetRequiredService<GuideLineView>();
            view.ShowDialog();
        }

        private void OpenModeling1View(object? obj)
        {
            var view = _serviceProvider.GetRequiredService<Modeling1View>();
            view.ShowDialog();

        }
    }
}
