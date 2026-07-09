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

        public long CreateLevel(string levelName, double elevation)
        {
            var doc = _doc();
            if (doc == null) return 0;

            Level level = Level.Create(doc, UC.MmToFt(elevation));
            level.Name = levelName;

            return (long)level.Id.Value;
        }

        public long UpdateLevel(string levelName, double elevation)
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
            return (long)level.Id.Value;
        }

        public void CreatePlan(long levelId)
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

        // 레벨의 3D 범위를 모델 지오메트리에 맞게 최대화 (우클릭 "3D 범위 최대화")
        // Transaction은 UseCase 레이어에서 관리 — 여기선 API 호출만
        public void Maximize3dExtents(long levelId)
        {
            var doc = _doc();
            if (doc == null) return;

            if (doc.GetElement(new ElementId(levelId)) is Level level)
            {
                level.Maximize3DExtents();
            }
        }
    }
}
