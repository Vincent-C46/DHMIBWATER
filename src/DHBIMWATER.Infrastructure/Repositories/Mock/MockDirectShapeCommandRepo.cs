using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
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
            throw new NotImplementedException();
        }

        public IReadOnlyList<int> CreateDirectShapes(IReadOnlyList<SolidExtrusionDefinition> solidExtrusionDefs)
        {
            throw new NotImplementedException();
        }
    }
}
