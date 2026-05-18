using DHBIMWATER.Core.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IDirectShapeCommandRepo
    {
        int CreateDirectShape(SolidExtrusionDefinition solidExtrusionDef);
        IReadOnlyList<int> CreateDirectShapes(IReadOnlyList<SolidExtrusionDefinition> solidExtrusionDefs);
    }
}
