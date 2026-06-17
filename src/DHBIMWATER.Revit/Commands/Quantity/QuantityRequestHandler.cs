using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
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

        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Document doc = uidoc.Document;

            try
            {
                switch (QuantityRequest.Take())
                {
                    case QuantityRequestId.None:
                        break;
                    case QuantityRequestId.Calculate:

                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Requset 요청 실패\n{ex.Message}");
            }
        }

        public string GetName() => "QuantityRequest";
    }
}
