using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using DHBIMWATER.Infrastructure.Services.Mock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    public class MockOpeningCommandRepo : IOpeningCommandRepo
    {
        public void CreateSlabOpening(RectangularSlabOpeningDefinition openingDef)
        {
            var mockDialogService = new MockDialogService();
            mockDialogService.Info("Opening Creation", $"{openingDef.Name} 작성완료");
            return;
        }

        public void CreateSlabOpening(CircularSlabOpeningDefinition openingDef)
        {
                        var mockDialogService = new MockDialogService();
            mockDialogService.Info("Opening Creation", $"{openingDef.Name} 작성완료");
            return;
        }

        public void CreateWallOpening(RectangularWallOpeningDefinition openingDef)
        {
                        var mockDialogService = new MockDialogService();
            mockDialogService.Info("Opening Creation", $"{openingDef.Name} 작성완료");
            return;
        }

        public void CreateWallOpening(CircularWallOpeningDefinition openingDef)
        {
                        var mockDialogService = new MockDialogService();
            mockDialogService.Info("Opening Creation", $"{openingDef.Name} 작성완료");
            return;
        }
    }
}
