using DHBIMWATER.Application.Interface;
using DHBIMWATER.Application.UseCases;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling
{
    public class Modeling1ViewModel : ViewModelBase
    {
        private readonly CountGenericModelUseCase _useCase;
        private readonly IDialogService _dialogService;

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


        public Modeling1ViewModel(CountGenericModelUseCase useCase, IDialogService dialogService)
        {
            _useCase = useCase;
            _dialogService = dialogService;


            CountCommand = new RelayCommand(CountModels);
        }

        private void CountModels(object? obj)
        {
            ModelCount = _useCase.Execute();
            //TaskDialog.Show("Model Count", $"Total Generic Models: {ModelCount}");
            _dialogService.Info("Model Count", $"Total Generic Models: {ModelCount}");

        }
    }
}
