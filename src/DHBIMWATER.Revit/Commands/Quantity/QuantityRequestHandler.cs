using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Revit.Commands.Quantity
{
    public class QuantityRequestHandler : IExternalEventHandler
    {
        private readonly QuantityRequest _quantityRequest = new QuantityRequest();
        public QuantityRequest QuantityRequest { get { return _quantityRequest; } }

        public void Execute(UIApplication app)
        {
            throw new NotImplementedException();
        }

        public string GetName()
        {
            throw new NotImplementedException();
        }
    }
}
