using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewBorderAndTitleService
    {
        private readonly Document _doc;

        private const double MmPerFt = 304.8;
        private const double BorderWidthMm  = 42050.0;
        private const double BorderHeightMm = 29700.0;
        private const double TitleFontSizeMm = 14.0;
        private const double TitleOffsetMm   = 10.0;
        // 제목 텍스트를 위한 상단 여유 공간
        private const double TitleSpaceMm    = TitleOffsetMm + TitleFontSizeMm * 2 + 5.0;
        // 도곽선 크기는 정확히 유지 (인셋 없음)
        private const double BorderInsetMm   = 0.0;

        public ViewBorderAndTitleService(Document doc)
        {
            _doc = doc;
        }

        public void Apply(string viewId, string titleText)
        {
            var view = _doc.GetElement(new ElementId(long.Parse(viewId))) as View;
            if (view == null) return;

            using var tx = new Transaction(_doc, "뷰 도곽 및 제목 생성");
            tx.Start();

            DeleteExisting(view);

            if (view is ViewPlan planView)
            {
                // 1. 모델 요소 바운딩박스로 콘텐츠 중심 계산
                var contentCenter = GetContentCenterWorld(planView);

                // 2. 크롭 영역을 도곽 크기에 맞게 재설정 (상단 제목 여유 포함)
                try { SetCropRegion(planView, contentCenter); }
                catch { }

                // 3. 도곽 선 생성 (42050×28700)
                try { CreateBorder(planView, contentCenter); }
                catch { }

                // 4. 제목 텍스트 생성 (도곽 상단 위)
                try { CreateTitle(planView, contentCenter, titleText); }
                catch { }

                // 새로 생성한 도곽/제목이 뷰포트 크기 계산에 반영되도록 강제 갱신
                _doc.Regenerate();

                // 5. 뷰포트를 시트 중앙에 재배치
                try { RecenterViewport(planView); }
                catch { }
            }
            else
            {
                // 단면 뷰: 텍스트만 (크롭 박스 중심 기준)
                var center = GetCropBoxCenterWorld(view);
                CreateTitle(view, center, titleText);
            }

            tx.Commit();
        }

        // ───────────────────────────────────────────
        // 모델 요소 바운딩박스로 콘텐츠 중심(월드 좌표) 계산
        // ───────────────────────────────────────────
        private XYZ GetContentCenterWorld(ViewPlan view)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            bool found = false;

            var elements = new FilteredElementCollector(_doc, view.Id)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (var el in elements)
            {
                // 주석, 치수, 커브 등 제외
                if (el is TextNote || el is CurveElement || el is IndependentTag || el is Dimension)
                    continue;
                if (el.Category == null) continue;
                if (el.Category.CategoryType != CategoryType.Model) continue;
                if (el.Category.BuiltInCategory == BuiltInCategory.OST_Cameras) continue;

                var bb = el.get_BoundingBox(null); // 월드 좌표
                if (bb == null) continue;

                minX = Math.Min(minX, bb.Min.X); minY = Math.Min(minY, bb.Min.Y);
                maxX = Math.Max(maxX, bb.Max.X); maxY = Math.Max(maxY, bb.Max.Y);
                found = true;
            }

            if (!found)
                return GetCropBoxCenterWorld(view);

            // Z는 뷰 원점(컷 플레인 레벨)
            return new XYZ((minX + maxX) / 2.0, (minY + maxY) / 2.0, view.Origin.Z);
        }

        // ───────────────────────────────────────────
        // 크롭 영역을 도곽 크기로 재설정
        // ───────────────────────────────────────────
        // 도곽선이 크롭 경계선과 정확히 겹쳐서 잘리는 것을 피하기 위한 여유값
        private const double CropMarginMm = 500.0;

        private void SetCropRegion(ViewPlan view, XYZ worldCenter)
        {
            double halfW     = BorderWidthMm  / 2.0 / MmPerFt + CropMarginMm / MmPerFt;
            double halfH     = BorderHeightMm / 2.0 / MmPerFt + CropMarginMm / MmPerFt;
            double titleFt   = TitleSpaceMm / MmPerFt;

            // 기존 크롭이 비직사각형(커스텀 스케치)이면 CropBox 설정이 무시되므로
            // 먼저 직사각형 모양으로 리셋
            var shapeMgr = view.GetCropRegionShapeManager();
            try { shapeMgr.RemoveCropRegionShape(); } catch { }

            var cb = view.CropBox;
            if (cb == null) return;

            // 월드 좌표 → 크롭박스 로컬 좌표
            var inv = cb.Transform.Inverse;
            var lc  = inv.OfPoint(worldCenter);

            var newBB = new BoundingBoxXYZ();
            newBB.Transform = cb.Transform;
            // 도곽 크기 + 여유(margin), 상단은 제목 공간까지 포함
            newBB.Min = new XYZ(lc.X - halfW, lc.Y - halfH, cb.Min.Z);
            newBB.Max = new XYZ(lc.X + halfW, lc.Y + halfH + titleFt, cb.Max.Z);

            view.CropBoxActive = true;
            view.CropBoxVisible = false; // Revit 기본 크롭 경계선은 숨기고 우리가 그린 도곽만 표시
            view.CropBox       = newBB;
        }

        // ───────────────────────────────────────────
        // 도곽 선 4개 생성
        // ───────────────────────────────────────────
        private void CreateBorder(View view, XYZ center)
        {
            double halfW = (BorderWidthMm  - BorderInsetMm * 2) / 2.0 / MmPerFt;
            double halfH = (BorderHeightMm - BorderInsetMm * 2) / 2.0 / MmPerFt;

            var right = view.RightDirection.Normalize();
            var up    = view.UpDirection.Normalize();

            var p1 = center - right * halfW - up * halfH;
            var p2 = center + right * halfW - up * halfH;
            var p3 = center + right * halfW + up * halfH;
            var p4 = center - right * halfW + up * halfH;

            var lines = new[]
            {
                Line.CreateBound(p1, p2),
                Line.CreateBound(p2, p3),
                Line.CreateBound(p3, p4),
                Line.CreateBound(p4, p1),
            };

            foreach (var line in lines)
            {
                var dl = _doc.Create.NewDetailCurve(view, line) as CurveElement;
                dl?.LookupParameter("Comments")?.Set("DH_ViewBorder");
            }
        }

        // ───────────────────────────────────────────
        // 제목 텍스트 생성 (도곽 상단 위)
        // ───────────────────────────────────────────
        private void CreateTitle(View view, XYZ center, string titleText)
        {
            if (string.IsNullOrWhiteSpace(titleText)) return;

            double halfH      = BorderHeightMm / 2.0 / MmPerFt;
            double offsetFt   = TitleOffsetMm  / MmPerFt;
            double fontSizeFt = TitleFontSizeMm / MmPerFt;

            var up = view.UpDirection.Normalize();
            var pt = center + up * (halfH + offsetFt + fontSizeFt);

            var textTypeId = GetOrFindTextTypeId(fontSizeFt);
            if (textTypeId == ElementId.InvalidElementId) return;

            var opts = new TextNoteOptions(textTypeId)
            {
                HorizontalAlignment = HorizontalTextAlignment.Center,
            };

            var textNote = TextNote.Create(_doc, view.Id, pt, titleText, opts);
            textNote?.LookupParameter("Comments")?.Set("DH_ViewTitle");
        }

        // ───────────────────────────────────────────
        // 뷰포트를 시트 중앙에 재배치
        // ───────────────────────────────────────────
        private void RecenterViewport(View view)
        {
            var viewport = new FilteredElementCollector(_doc)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .FirstOrDefault(vp => vp.ViewId == view.Id);

            if (viewport == null) return;

            var sheet = _doc.GetElement(viewport.SheetId) as ViewSheet;
            if (sheet == null) return;

            var outline = sheet.Outline;
            var center  = new XYZ(
                (outline.Min.U + outline.Max.U) / 2.0,
                (outline.Min.V + outline.Max.V) / 2.0,
                0);

            viewport.SetBoxCenter(center);
        }

        // ───────────────────────────────────────────
        // 크롭박스 중심 (폴백용)
        // ───────────────────────────────────────────
        private XYZ GetCropBoxCenterWorld(View view)
        {
            var cb = view.CropBox;
            if (cb == null) return view.Origin;
            var lc = new XYZ(
                (cb.Min.X + cb.Max.X) / 2.0,
                (cb.Min.Y + cb.Max.Y) / 2.0,
                (cb.Min.Z + cb.Max.Z) / 2.0);
            return cb.Transform.OfPoint(lc);
        }

        // ───────────────────────────────────────────
        // 기존 도곽/제목 삭제
        // ───────────────────────────────────────────
        private void DeleteExisting(View view)
        {
            var ids = new FilteredElementCollector(_doc, view.Id)
                .OfClass(typeof(CurveElement))
                .WhereElementIsNotElementType()
                .Where(e => e.LookupParameter("Comments")?.AsString() == "DH_ViewBorder")
                .Select(e => e.Id)
                .Concat(
                    new FilteredElementCollector(_doc, view.Id)
                        .OfClass(typeof(TextNote))
                        .WhereElementIsNotElementType()
                        .Where(e => e.LookupParameter("Comments")?.AsString() == "DH_ViewTitle")
                        .Select(e => e.Id))
                .ToList();

            foreach (var id in ids)
                _doc.Delete(id);
        }

        // ───────────────────────────────────────────
        // 텍스트 타입 조회
        // ───────────────────────────────────────────
        private ElementId GetOrFindTextTypeId(double fontSizeFt)
        {
            var types = new FilteredElementCollector(_doc)
                .OfClass(typeof(TextNoteType))
                .Cast<TextNoteType>()
                .ToList();

            if (!types.Any()) return ElementId.InvalidElementId;

            return types
                .OrderBy(t => Math.Abs(
                    (t.get_Parameter(BuiltInParameter.TEXT_SIZE)?.AsDouble() ?? 0) - fontSizeFt))
                .First().Id;
        }
    }
}
