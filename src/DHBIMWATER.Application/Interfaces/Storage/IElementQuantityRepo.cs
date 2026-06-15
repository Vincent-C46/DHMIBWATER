using DHBIMWATER.Core.Quantity;
using System.Collections.Generic;

namespace DHBIMWATER.Application.Interfaces.Storage
{
    public interface IElementQuantityRepo
    {
        void Save(long elementId, IEnumerable<QuantityItem> items);
        IReadOnlyList<QuantityItem> Load(long elementId);
        void Delete(long elementId);
        bool HasData(long elementId);
    }
}