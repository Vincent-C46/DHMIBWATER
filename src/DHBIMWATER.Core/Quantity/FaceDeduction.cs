using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Quantity
{
    public record FaceDeduction(FaceType TargetFaceType, long NeighborId, double Area);
}