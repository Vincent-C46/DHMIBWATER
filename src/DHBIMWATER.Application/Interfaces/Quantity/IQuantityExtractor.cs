using DHBIMWATER.Core.Quantity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces.Quantity
{
    public interface IQuantityExtractor
    {
        bool CanExtract(long elementId);
        IEnumerable<long> CollectElementIds();
        IEnumerable<QuantityItem> Extract(long elementId);
    }
}