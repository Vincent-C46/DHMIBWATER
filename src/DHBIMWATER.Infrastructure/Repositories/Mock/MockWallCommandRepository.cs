using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DHBIMWATER.Infrastructure.Services.Mock;
using DHBIMWATER.Application.Interfaces;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    internal class MockWallCommandRepository : IWallCommandRepo
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Constructor
        #endregion

        #region Methods
        public void CreateWall(double length, double n)
        {
            var mockDialogService = new MockDialogService();
            mockDialogService.Info("MockWallCommandRepo - CreateWall", $"Wall Length: {length}ft \nWall Count: {n}EA");
        }
        #endregion



    }
}
