using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    internal class MockWallCommandRepo : IWallCommandRepo
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Constructor
        #endregion

        #region Methods
        public void CreateWall()
        {
            MessageBox.Show("MockWallCommandRepo - CreateWall");
        }
        #endregion



    }
}
