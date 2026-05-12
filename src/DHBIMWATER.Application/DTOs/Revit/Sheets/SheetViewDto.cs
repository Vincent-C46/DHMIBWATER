using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.Sheet
{
    public class SheetViewDto
    {
        public string ViewId { get; set; }
        public string ViewName { get; set; }
        public string ViewType { get; set; }   // 예: "Plan", "Section"
    }

}
