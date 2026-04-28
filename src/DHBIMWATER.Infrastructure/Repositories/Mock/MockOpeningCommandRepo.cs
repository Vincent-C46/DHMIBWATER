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
    public class MockOpeningCommandRepo : IOpeningCommandRepo
    {
        public void CreateSlabOpening(RectangularSlabOpeningDefinition openingDef)
        {
            throw new NotImplementedException();
        }

        public void CreateSlabOpening(CircularSlabOpeningDefinition openingDef)
        {
            throw new NotImplementedException();
        }

        public void CreateWallOpening(RectangularWallOpeningDefinition openingDef)
        {
            throw new NotImplementedException();
        }

        public void CreateWallOpening(CircularWallOpeningDefinition openingDef)
        {
            throw new NotImplementedException();
        }
    }
}
