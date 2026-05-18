using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IWallCommandRepo
    {
        int CreateLinearWall(LinearWallDefinition linearWallDefinition);
        int CreateProfileWall(ProfileWallDefinition profileWallDefinition);
    }
}
