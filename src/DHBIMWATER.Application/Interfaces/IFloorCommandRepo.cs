using DHBIMWATER.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IFloorCommandRepo
    {
        int CreateFloor (IList<Point3D> profilePoints, string slabTypeId);
    }
}
