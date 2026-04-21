using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IWallCommandRepo
    {
        int CreateWall(double length, double n);
        int CreateProfileWall(IList<Point3D> profilePoints_mm, string wallTypeName, string levelName);
    }
}
