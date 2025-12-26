using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using DHBIMWATER.UI.Sandbox.DependencyInjection;
using DHBIMWATER.UI.Views.Modeling;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;

namespace DHBIMWATER.UI.Sandbox
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;

        public ICommand OpenModeling1ViewCommand { get; }

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            OpenModeling1ViewCommand = new RelayCommand(OpenModeling1View);
        }

        private void OpenModeling1View(object? obj)
        {
            var modeling1Window = _serviceProvider.GetRequiredService<Modeling1View>();
            modeling1Window.ShowDialog();

        }
    }
}
