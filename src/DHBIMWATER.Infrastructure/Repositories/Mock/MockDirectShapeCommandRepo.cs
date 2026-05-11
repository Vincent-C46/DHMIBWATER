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
    public class MockDirectShapeCommandRepo : IDirectShapeCommandRepo
    {
        public int CreateDirectShape(SolidExtrusionDefinition solidExtrusionDef)
        {
            var mockDialogService = new MockDialogService();
            mockDialogService.Info("DirectShaep Creation", $"다이렉트 쉐이프 작성완료");
            return 0;
        }

        public IReadOnlyList<int> CreateDirectShapes(IReadOnlyList<SolidExtrusionDefinition> solidExtrusionDefs)
        {
            var mockDialogService = new MockDialogService();
            mockDialogService.Info("DirectShaep Creation", $"다이렉트 쉐이프 작성완료");
            return new List<int>();
        }
    }
}
