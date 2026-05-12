using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DHBIMWATER.Application.DTOs.Revit.Sheet
{
    public class ViewInfoDto
    {
        public string ViewId { get; set; }
        public string ViewName { get; set; }
        public string ViewType { get; set; }
        public int Scale { get; set; }            // 실제 스케일 값 (예: 100)
        public string ScaleText { get; set; }     // 표시용 ("1:100")
        public string VisualStyle { get; set; }   // 예: "은선", "음영처리"
        public string TitleOnSheet { get; set; }
        public string SheetForm { get; set; }
    }
}
