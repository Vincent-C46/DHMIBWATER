using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Revit.Commands.Quantity
{
    public enum QuantityRequestId
    {
        None = 0,
        Calculate = 1,
        SelectInRevit = 2,
    }
    public class QuantityRequest
    {
        private int _requestId = (int)QuantityRequestId.None;   // None으로 초기화

        public QuantityRequestId Take()
        {
            return (QuantityRequestId)Interlocked.Exchange(ref _requestId, (int)QuantityRequestId.None);
        }
        public void Make(QuantityRequestId requestId)
        {
            Interlocked.Exchange(ref _requestId, (int)requestId);
        }
    }
}