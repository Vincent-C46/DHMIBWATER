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
            mockDialogService.Info("LinearWall Creation", $"{linearWallDefinition.ElementCode} 작성완료");
            return 0;
        }

        public int CreateProfileWall(ProfileWallDefinition profileWallDefinition)
        {
            var mockDialogService = new MockDialogService();
            mockDialogService.Info("ProfileWall Creation", $"{profileWallDefinition.ElementCode} 작성완료");
            return 0;
        }
    }
}
