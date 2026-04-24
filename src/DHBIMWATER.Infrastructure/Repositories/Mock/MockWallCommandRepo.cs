using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DHBIMWATER.Infrastructure.Services.Mock;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;

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
        public int CreateWall(double length, double n)
        {
            var mockDialogService = new MockDialogService();
            mockDialogService.Info("MockWallCommandRepo - CreateWall", $"Wall Length: {length}ft \nWall Count: {n}EA");
            return 123456;
        }

        public int CreateProfileWall(IList<Point3D> profilePoints_mm, string wallTypeName, string levelName)
        {
            return 123456;
        }
        #endregion
    }
}
