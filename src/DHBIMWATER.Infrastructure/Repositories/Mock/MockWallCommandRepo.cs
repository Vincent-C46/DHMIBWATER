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
using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    internal class MockWallCommandRepo : IWallCommandRepo
    {
        public int CreateLinearWall(LinearWallDefinition linearWallDefinition)
        {
            var mockDialogService = new MockDialogService();
            mockDialogService.Info("MockWallCommandRepo - CreateWall", $"Wall Length: {linearWallDefinition.WallCurve.Length}ft \nWall Count: EA");
            return 123456;
        }

        public int CreateProfileWall(ProfileWallDefinition profileWallDefinition)
        {
            throw new NotImplementedException();
        }
    }
}
