using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Collections.ObjectModel;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling
{
    public class PumpingStationViewModel : ViewModelBase
    {
        #region Fields
        // 설계조건
        private double _d = 800.0;
        private double _hd = 5.0;
        private int _n = 3;
        private double _lwl = 0;
        private double _hwl = 800;

        // 종단제원
        private double _b1 = 1200;
        private double _b3 = 5000;
        private double _b4 = 3000;
        private double _b5 = 1600;
        private double _b6 = 500;
        private double _b7 = 3000;
        private double _h1 = 500;

        // 평면제원


        // 부재 유형
        private double _t1 = 400;
        private double _t2;
        private double _t3 = 400;
        private double _t4;
        #endregion

        #region Properties
        // 설계조건
        public double D
        {
            get { return _d; }
            set
            {
                if (_d != value)
                {
                    _d = value;
                    OnPropertyChanged(nameof(D));
                }
            }
        }
        public double HD
        {
            get { return _hd; }
            set
            {
                if (_hd != value)
                {
                    _hd = value;
                    OnPropertyChanged(nameof(HD));
                }
            }
        }
        public int N
        {
            get { return _n; }
            set
            {
                if (_n != value)
                {
                    _n = value;
                    OnPropertyChanged(nameof(N));
                }
            }
        }
        public double LWL
        {
            get { return _lwl; }
            set
            {
                if (_lwl != value)
                {
                    _lwl = value;
                    OnPropertyChanged(nameof(LWL));
                }
            }
        }
        public double HWL
        {
            get { return _hwl; }
            set
            {
                if (_hwl != value)
                {
                    _hwl = value;
                    OnPropertyChanged(nameof(HWL));
                }
            }
        }


        // 종단제원
        public double B1
        {
            get { return _b1; }
            set
            {
                if (_b1 != value)
                {
                    _b1 = value;
                    OnPropertyChanged(nameof(B1));
                }
                if (value < 0)
                {
                    _b1 = 400;
                    OnPropertyChanged(nameof(B1));
                }
            }
        }
        public double B3
        {
            get { return _b3; }
            set
            {
                if (_b3 != value)
                {
                    _b3 = value;
                    OnPropertyChanged(nameof(B3));
                }
            }
        }
        public double B4
        {
            get { return _b4; }
            set
            {
                if (_b4 != value)
                {
                    _b4 = value;
                    OnPropertyChanged(nameof(B4));
                }
            }
        }
        public double B5
        {
            get { return _b5; }
            set
            {
                if (_b5 != value)
                {
                    _b5 = value;
                    OnPropertyChanged(nameof(B5));
                }
            }
        }
        public double B6
        {
            get { return _b6; }
            set
            {
                if (_b6 != value)
                {
                    _b6 = value;
                    OnPropertyChanged(nameof(B6));
                }
            }
        }
        public double B7
        {
            get { return _b7; }
            set
            {
                if (_b7 != value)
                {
                    _b7 = value;
                    OnPropertyChanged(nameof(B7));
                }
            }
        }
        public double H1
        {
            get { return _h1; }
            set
            {
                if (_h1 != value)
                {
                    _h1 = value;
                    OnPropertyChanged(nameof(H1));
                }
            }
        }

        // 부재 유형
        public double T1
        {
            get { return _t1; }
            set
            {
                if (_t1 != value)
                {
                    _t1 = value;
                    OnPropertyChanged(nameof(T1));
                }
                if (value < 0)
                {
                    _t1 = 400;
                    OnPropertyChanged(nameof(T1));
                }
            }
        }


        #endregion


        #region Commands
        public ICommand CreatePumpingStationCommand { get; }
        #endregion


        #region Constructor
        public PumpingStationViewModel(CreateReservoirUseCase useCase, IDialogService dialogService, IElementTypeQueryRepo elementTypeQueryRepo)
        {
            CreatePumpingStationCommand = new RelayCommand(CreatePumpingStation);

        }
        #endregion

        #region Methods
        private void CreatePumpingStation(object? obj)
        {
        }

        #endregion
    }
}
