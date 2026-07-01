using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling
{
    internal class RevitLevelCommandRepo : ILevelCommandRepo
    {

        private readonly Func<Document?> _doc;

        public RevitLevelCommandRepo(Func<Document?> doc)
        {
            _doc = doc;
        }

        public int CreateLevel(string levelName, double elevation)
        {
            var doc = _doc();
            if (doc == null) return 0;

            Level level = Level.Create(doc, UC.MmToFt(elevation));
            level.Name = levelName;

            return (int)level.Id.Value;
        }

        public int UpdateLevel(string levelName, double elevation)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var level = new FilteredElementCollector(doc)
                          .OfCategory(BuiltInCategory.OST_Levels)
                          .WhereElementIsNotElementType()
                          .Cast<Level>()
                          .FirstOrDefault(lvl => lvl.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase));
            if (level != null)
            {
                level.Elevation = UC.MmToFt(elevation);
            }
            return (int)level.Id.Value;
        }

        public void CreatePlan(int levelId)
        {
            var doc = _doc();
            if (doc == null) return;

            ViewFamilyType structViewType = new FilteredElementCollector(doc)
                                            .OfClass(typeof(ViewFamilyType))
                                            .Cast<ViewFamilyType>()
                                            .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.StructuralPlan);

            var viewPlan = ViewPlan.Create(doc, structViewType.Id, new ElementId((long)levelId));

            var offset = UC.MmToFt(550); // 레벨 평면뷰 작성시 절단기준면 550mm 로 설정
            var viewRange = viewPlan.GetViewRange();

            var top = viewRange.GetOffset(PlanViewPlane.TopClipPlane);
            if (top >= offset) 
            {
                viewRange.SetOffset(PlanViewPlane.CutPlane, offset);
                viewPlan.SetViewRange(viewRange);
            };

            viewPlan.LookupParameter("DH_뷰 카테고리")?.Set("모델링");
            viewPlan.LookupParameter("DH_뷰 타입")?.Set("평면도");
        }
    }
}
