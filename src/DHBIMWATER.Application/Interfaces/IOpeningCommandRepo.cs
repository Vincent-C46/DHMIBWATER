using DHBIMWATER.Core.Structures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IOpeningCommandRepo
    {
        void CreateSlabOpening(RectangularSlabOpeningDefinition openingDef);
        void CreateSlabOpening(CircularSlabOpeningDefinition openingDef);
        void CreateWallOpening(RectangularWallOpeningDefinition openingDef);
        void CreateWallOpening(CircularWallOpeningDefinition openingDef);
    }
}
