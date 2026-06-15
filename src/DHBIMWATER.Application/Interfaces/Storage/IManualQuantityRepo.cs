using DHBIMWATER.Core.Quantity;
using System.Collections.Generic;

namespace DHBIMWATER.Application.Interfaces.Storage
{
    public interface IManualQuantityRepo
    {
        void Save(IEnumerable<QuantityItem> items);
        IReadOnlyList<QuantityItem> LoadAll();
        void Clear();
    }
}
