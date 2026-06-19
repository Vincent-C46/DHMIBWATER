using System;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Documentation.Sheets
{
    public class WaterLevelViewModel : ViewModelBase
    {
        private readonly Action<string, string> _onConfirm;

        private string _hwl;
        public string HWL
        {
            get => _hwl;
            set { _hwl = value; OnPropertyChanged(); }
        }

        private string _lwl;
        public string LWL
        {
            get => _lwl;
            set { _lwl = value; OnPropertyChanged(); }
        }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; OnPropertyChanged(); }
        }

        public RelayCommand ConfirmCommand { get; }
        public RelayCommand CancelCommand { get; }

        public WaterLevelViewModel(string initialHwl, string initialLwl, Action<string, string> onConfirm)
        {
            _onConfirm = onConfirm;
            HWL = initialHwl;
            LWL = initialLwl;

            ConfirmCommand = new RelayCommand(_ => Confirm());
            CancelCommand = new RelayCommand(_ => DialogResult = false);
        }

        private void Confirm()
        {
            _onConfirm?.Invoke(HWL, LWL);
            DialogResult = true;
        }
    }
}
