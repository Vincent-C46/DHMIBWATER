using DHBIMWATER.Core.Quantity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces.Geometry
{
    public interface IFaceClassifier
    {
        IReadOnlyDictionary<FaceType, double> GetFaceAreas(long elementId);
    }
}
