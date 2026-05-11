using Autodesk.Revit.DB;
using DHBIMWATER.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Infrastructure.Converters
{
    public static class RevitGeometryConverter
    {
        public static XYZ ToXYZ(Point3D p) => new XYZ(p.X, p.Y, p.Z);
        public static Curve ToCurve(Line3D l) => Line.CreateBound(ToXYZ(l.StartPoint), ToXYZ(l.EndPoint));
    }
}
