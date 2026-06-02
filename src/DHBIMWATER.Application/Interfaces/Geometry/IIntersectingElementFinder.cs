using System.Collections.Generic;

namespace DHBIMWATER.Application.Interfaces.Geometry
{
    public interface IIntersectingElementFinder
    {
        IEnumerable<long> FindIntersecting(long referenceElementId);
    }
}
