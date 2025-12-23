using DHBIMWATER.Application.UseCases;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Windows.Forms;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling
{
    public class Modeling1ViewModel : ViewModelBase
    {
        private readonly CountGenericModelUseCase _useCase;

        private int _modelCount;
        public int ModelCount
        {
            get => _modelCount;
            set
            {
                _modelCount = value;
                OnPropertyChanged();
            }
        }

        public ICommand CountCommand { get; }


        public Modeling1ViewModel(CountGenericModelUseCase useCase)
        {
            _useCase = useCase;

            CountCommand = new RelayCommand(CountModels);
        }

        private void CountModels(object? obj)
        {
            ModelCount = _useCase.Execute();
            //TaskDialog.ShowDialog("Model Count", $"Total Generic Models: {ModelCount}");

        }
    }
}
