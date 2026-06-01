using Autodesk.Revit.DB;
using DHBIMWATER.Application.UseCases.Sheets;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class DimensionService
    {
        private readonly Document _doc;

        public DimensionService(Document doc)
        {
            _doc = doc;
        }

        public void ApplyAutoDimensions(string sheetId)
        {
            if (!long.TryParse(sheetId, out var sid)) return;

            var sheet = _doc.GetElement(new ElementId(sid)) as ViewSheet;
            if (sheet == null) return;

            using (var tx = new Transaction(_doc, "Auto Dimension On Sheet"))
            {
                tx.Start();

                foreach (var vpId in sheet.GetAllViewports())
                {
                    var vp = _doc.GetElement(vpId) as Viewport;
                    if (vp == null) continue;

                    var view = _doc.GetElement(vp.ViewId) as View;
                    if (view == null) continue;
                    if (view is not ViewPlan && view is not ViewSection) continue;

                    AutoDimensionView(view);

                }
                tx.Commit();
            }
        }

        private void AutoDimensionView(View view)
        {
            var targets = new FilteredElementCollector(_doc, view.Id)
                .WhereElementIsNotElementType()
                .Where(e =>
                    e.Category != null &&
                    (e.Category.Id.Value == (int)BuiltInCategory.OST_Walls ||
                     e.Category.Id.Value == (int)BuiltInCategory.OST_StructuralColumns ||
                     e.Category.Id.Value == (int)BuiltInCategory.OST_StructuralFraming ||
                     e.Category.Id.Value == (int)BuiltInCategory.OST_Floors ||
                     e.Category.Id.Value == (int)BuiltInCategory.OST_GenericModel ||
                     e.Category.Id.Value == (int)BuiltInCategory.OST_StructuralFoundation))
                .ToList();

            AutoDimensionView(view, targets);
        }
        private void AutoDimensionView(View view, List<Element> targets)
            => AutoDimensionView(view, targets, DimensionSide.All, true);

        private void AutoDimensionView(View view, List<Element> targets, DimensionSide sides)
            => AutoDimensionView(view, targets, sides, true);

        private void AutoDimensionView(View view, List<Element> targets, DimensionSide sides, bool includeOverall)
        {
            if (targets == null || !targets.Any()) return;

            var right = view.RightDirection.Normalize(); // X축
            var up = view.UpDirection.Normalize();       // Y축

            var xRefs = new List<FaceRef>();
            var yRefs = new List<FaceRef>();

            GetModelExtents(view, targets, right, up, out var minRightAll, out var maxRightAll, out var minUpAll, out var maxUpAll);

            const double chainGap = 6.0;
            const double totalGap = 12.0;

            double topChain = maxUpAll + chainGap;
            double topTotal = maxUpAll + totalGap;
            double leftChain = minRightAll - chainGap;
            double leftTotal = minRightAll - totalGap;
            double bottomChain = minUpAll - chainGap;
            double bottomTotal = minUpAll - totalGap;
            double rightChain = maxRightAll + chainGap;
            double rightTotal = maxRightAll + totalGap;

            foreach (var e in targets)
            {
                if (!TryGetFaceRefs(view, e, right, up, out var minR, out var maxR, out var minU, out var maxU))
                    continue;

                xRefs.Add(minR);
                xRefs.Add(maxR);
                yRefs.Add(minU);
                yRefs.Add(maxU);
            }
            if (xRefs.Count >= 2)
            {
                if (sides.HasFlag(DimensionSide.Top))
                {
                    CreateChainDimensionAtTop(view, xRefs, right, up, topChain);
                    if (includeOverall) CreateOverallDimensionAtTop(view, xRefs, right, up, topTotal);
                }
                if (sides.HasFlag(DimensionSide.Bottom))
                {
                    CreateChainDimensionAtBottom(view, xRefs, right, up, bottomChain);
                    if (includeOverall) CreateOverallDimensionAtBottom(view, xRefs, right, up, bottomTotal);
                }
            }
            if (yRefs.Count >= 2)
            {
                if (sides.HasFlag(DimensionSide.Left))
                {
                    CreateChainDimensionAtLeft(view, yRefs, up, right, leftChain);
                    if (includeOverall) CreateOverallDimensionAtLeft(view, yRefs, up, right, leftTotal);
                }
                if (sides.HasFlag(DimensionSide.Right))
                {
                    CreateChainDimensionAtRight(view, yRefs, up, right, rightChain);
                    if (includeOverall) CreateOverallDimensionAtRight(view, yRefs, up, right, rightTotal);
                }
            }
        }

        private bool TryGetFaceRefs(
            View view, Element e, XYZ right, XYZ up,
            out FaceRef minRight, out FaceRef maxRight,
            out FaceRef minUp, out FaceRef maxUp)
        {
            minRight = maxRight = minUp = maxUp = null;

            var opt = new Options { View = view, ComputeReferences = true };
            var geo = e.get_Geometry(opt);
            if (geo == null) return false;

            foreach (var obj in geo)
            {
                var solid = obj as Solid;
                if (solid == null || solid.Faces.IsEmpty) continue;

                foreach (Face f in solid.Faces)
                {
                    if (f is not PlanarFace pf || pf.Reference == null) continue;

                    var n = pf.FaceNormal.Normalize();
                    var alignedRight = Math.Abs(Math.Abs(n.DotProduct(right)) - 1.0) < 0.01;
                    var alignedUp = Math.Abs(Math.Abs(n.DotProduct(up)) - 1.0) < 0.01;

                    var bb = pf.GetBoundingBox();
                    var uv = (bb.Min + bb.Max) * 0.5;
                    var p = pf.Evaluate(uv);

                    var pr = p.DotProduct(right);
                    var pu = p.DotProduct(up);

                    // 좌/우 치수용 face (normal 이 right 계열)
                    if (alignedRight)
                    {
                        var itemR = new FaceRef(pf.Reference, pr, p);
                        if (minRight == null || pr < minRight.Projection) minRight = itemR;
                        if (maxRight == null || pr > maxRight.Projection) maxRight = itemR;
                    }

                    // 상/하 치수용 face (normal 이 up 계열)
                    if (alignedUp)
                    {
                        var itemU = new FaceRef(pf.Reference, pu, p);
                        if (minUp == null || pu < minUp.Projection) minUp = itemU;
                        if (maxUp == null || pu > maxUp.Projection) maxUp = itemU;
                    }
                }
            }

            return minRight != null && maxRight != null && minUp != null && maxUp != null;
        }


        private class FaceRef
        {
            public Reference Reference { get; }
            public double Projection { get; }
            public XYZ Point { get; }

            public FaceRef(Reference reference, double projection, XYZ point)
            {
                Reference = reference;
                Projection = projection;
                Point = point;
            }
        }
        private static List<FaceRef> BuildOrderedDistinctRefs(List<FaceRef> refs)
        {
            if (refs == null) return new List<FaceRef>();

            var ordered = refs.OrderBy(r => r.Projection).ToList();
            var filtered = new List<FaceRef>();
            const double tol = 1e-4;

            foreach (var r in ordered)
            {
                if (filtered.Count == 0 || Math.Abs(r.Projection - filtered[^1].Projection) > tol)
                    filtered.Add(r);
            }

            return filtered;
        }

        // 횡방향 치수선: 항상 모델 위쪽
        private void CreateChainDimensionAtTop(
        View view, List<FaceRef> refs, XYZ lineDir, XYZ upDir, double topCoord)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            for (int i = 0; i < filtered.Count - 1; i++)
            {
                var first = filtered[i];
                var second = filtered[i + 1];

                var ra = new ReferenceArray();
                ra.Append(first.Reference);
                ra.Append(second.Reference);

                var p1 = lineDir * first.Projection + upDir * topCoord;
                var p2 = lineDir * second.Projection + upDir * topCoord;

                if (p1.DistanceTo(p2) < 1e-6) continue;

                _doc.Create.NewDimension(view, Line.CreateBound(p1, p2), ra);
            }
        }

        // 종방향 치수선: 항상 모델 왼쪽
        private void CreateChainDimensionAtLeft(
        View view, List<FaceRef> refs, XYZ lineDir, XYZ rightDir, double leftCoord)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            for (int i = 0; i < filtered.Count - 1; i++)
            {
                var first = filtered[i];
                var second = filtered[i + 1];

                var ra = new ReferenceArray();
                ra.Append(first.Reference);
                ra.Append(second.Reference);

                var p1 = lineDir * first.Projection + rightDir * leftCoord;
                var p2 = lineDir * second.Projection + rightDir * leftCoord;

                if (p1.DistanceTo(p2) < 1e-6) continue;

                _doc.Create.NewDimension(view, Line.CreateBound(p1, p2), ra);
            }
        }

        private void CreateChainDimensionAtBottom(
        View view, List<FaceRef> refs, XYZ lineDir, XYZ upDir, double bottomCoord)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            for (int i = 0; i < filtered.Count - 1; i++)
            {
                var first = filtered[i];
                var second = filtered[i + 1];

                var ra = new ReferenceArray();
                ra.Append(first.Reference);
                ra.Append(second.Reference);

                var p1 = lineDir * first.Projection + upDir * bottomCoord;
                var p2 = lineDir * second.Projection + upDir * bottomCoord;

                if (p1.DistanceTo(p2) < 1e-6) continue;

                _doc.Create.NewDimension(view, Line.CreateBound(p1, p2), ra);
            }
        }

        private void CreateOverallDimensionAtBottom(
            View view, List<FaceRef> refs, XYZ rightDir, XYZ upDir, double bottomCoord)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            var first = filtered.First();
            var last = filtered.Last();

            var ra = new ReferenceArray();
            ra.Append(first.Reference);
            ra.Append(last.Reference);

            var p1 = rightDir * first.Projection + upDir * bottomCoord;
            var p2 = rightDir * last.Projection + upDir * bottomCoord;

            if (p1.DistanceTo(p2) < 1e-6) return;
            _doc.Create.NewDimension(view, Line.CreateBound(p1, p2), ra);
        }

        private void CreateChainDimensionAtRight(
            View view, List<FaceRef> refs, XYZ lineDir, XYZ rightDir, double rightCoord)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            for (int i = 0; i < filtered.Count - 1; i++)
            {
                var first = filtered[i];
                var second = filtered[i + 1];

                var ra = new ReferenceArray();
                ra.Append(first.Reference);
                ra.Append(second.Reference);

                var p1 = lineDir * first.Projection + rightDir * rightCoord;
                var p2 = lineDir * second.Projection + rightDir * rightCoord;

                if (p1.DistanceTo(p2) < 1e-6) continue;

                _doc.Create.NewDimension(view, Line.CreateBound(p1, p2), ra);
            }
        }

        private void CreateOverallDimensionAtRight(
            View view, List<FaceRef> refs, XYZ upDir, XYZ rightDir, double rightCoord)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            var first = filtered.First();
            var last = filtered.Last();

            var ra = new ReferenceArray();
            ra.Append(first.Reference);
            ra.Append(last.Reference);

            var p1 = upDir * first.Projection + rightDir * rightCoord;
            var p2 = upDir * last.Projection + rightDir * rightCoord;

            if (p1.DistanceTo(p2) < 1e-6) return;
            _doc.Create.NewDimension(view, Line.CreateBound(p1, p2), ra);
        }


        private void GetModelExtents(
            View view,
            List<Element> elems,
            XYZ right,
            XYZ up,
            out double minRight,
            out double maxRight,
            out double minUp,
            out double maxUp)
        {
            minRight = double.MaxValue;
            maxRight = double.MinValue;
            minUp = double.MaxValue;
            maxUp = double.MinValue;

            foreach (var e in elems)
            {
                var bb = e.get_BoundingBox(view);
                if (bb == null) continue;

                var corners = new[]
                {
                    new XYZ(bb.Min.X, bb.Min.Y, bb.Min.Z),
                    new XYZ(bb.Min.X, bb.Max.Y, bb.Min.Z),
                    new XYZ(bb.Max.X, bb.Min.Y, bb.Min.Z),
                    new XYZ(bb.Max.X, bb.Max.Y, bb.Min.Z),
                    new XYZ(bb.Min.X, bb.Min.Y, bb.Max.Z),
                    new XYZ(bb.Min.X, bb.Max.Y, bb.Max.Z),
                    new XYZ(bb.Max.X, bb.Min.Y, bb.Max.Z),
                    new XYZ(bb.Max.X, bb.Max.Y, bb.Max.Z),
        };

                foreach (var p in corners)
                {
                    var r = p.DotProduct(right);
                    var u = p.DotProduct(up);

                    if (r < minRight) minRight = r;
                    if (r > maxRight) maxRight = r;
                    if (u < minUp) minUp = u;
                    if (u > maxUp) maxUp = u;
                }
            }

            if (minRight == double.MaxValue) { minRight = 0; maxRight = 0; minUp = 0; maxUp = 0; }
        }
        private void CreateOverallDimensionAtTop(
            View view,
            List<FaceRef> refs,
            XYZ rightDir,
            XYZ upDir,
            double topCoord)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            var first = filtered.First();
            var last = filtered.Last();

            var ra = new ReferenceArray();
            ra.Append(first.Reference);
            ra.Append(last.Reference);

            var p1 = rightDir * first.Projection + upDir * topCoord;
            var p2 = rightDir * last.Projection + upDir * topCoord;

            if (p1.DistanceTo(p2) < 1e-6) return;
            _doc.Create.NewDimension(view, Line.CreateBound(p1, p2), ra);
        }
        private void CreateOverallDimensionAtLeft(
            View view,
            List<FaceRef> refs,
            XYZ upDir,
            XYZ rightDir,
            double leftCoord)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            var first = filtered.First();
            var last = filtered.Last();

            var ra = new ReferenceArray();
            ra.Append(first.Reference);
            ra.Append(last.Reference);

            var p1 = upDir * first.Projection + rightDir * leftCoord;
            var p2 = upDir * last.Projection + rightDir * leftCoord;

            if (p1.DistanceTo(p2) < 1e-6) return;
            _doc.Create.NewDimension(view, Line.CreateBound(p1, p2), ra);
        }

        public void ApplyDimensionsToSelected(string sheetId, IList<string> elementIds)
        {
            if (!long.TryParse(sheetId, out var sid)) return;
            if (elementIds == null || elementIds.Count == 0) return;

            var selectedIds = elementIds
                .Select(idStr => long.TryParse(idStr, out var v) ? new ElementId(v) : ElementId.InvalidElementId)
                .Where(id => id != ElementId.InvalidElementId)
                .ToList();

            if (selectedIds.Count == 0) return;

            var sheet = _doc.GetElement(new ElementId(sid)) as ViewSheet;
            if (sheet == null) return;

            using (var tx = new Transaction(_doc, "Dimension Selected Elements On Sheet"))
            {
                tx.Start();

                foreach (var vpId in sheet.GetAllViewports())
                {
                    var vp = _doc.GetElement(vpId) as Viewport;
                    if (vp == null) continue;

                    var view = _doc.GetElement(vp.ViewId) as View;
                    if (view == null) continue;
                    if (view is not ViewPlan && view is not ViewSection) continue;


                    var targets = new List<Element>();

                    foreach (var id in selectedIds)
                    {
                        var e = _doc.GetElement(id);
                        if (e == null || e.Category == null) continue;

                        // 현재 plan view에서 보이는 요소만
                        if (e.get_BoundingBox(view) == null) continue;

                        targets.Add(e);
                    }

                    AutoDimensionView(view, targets);
                }
                tx.Commit();
            }
        }
        public void ApplyAutoDimensionsOnCurrentView(string dimensionTypeName)
        {
            var view = _doc.ActiveView;
            if (view == null) return;
            if (view is not ViewPlan && view is not ViewSection) return;

            var dimensionTypeId = GetDimensionTypeId(dimensionTypeName);

            using (var tx = new Transaction(_doc, "Auto Dimension On Current View"))
            {
                tx.Start();

                var beforeIds = GetDimensionIds(view);
                AutoDimensionView(view);
                ApplyDimensionTypeToNewDimensions(view, beforeIds, dimensionTypeId);

                tx.Commit();
            }
        }

        public void ApplyDimensionsToSelectedOnCurrentView(IList<string> elementIds, string dimensionTypeName, DimensionSide sides = DimensionSide.All, bool includeOverall = true)
        {
            if (elementIds == null || elementIds.Count == 0) return;

            var view = _doc.ActiveView;
            if (view == null) return;
            if (view is not ViewPlan && view is not ViewSection) return;

            var selectedIds = elementIds
                .Select(idStr => long.TryParse(idStr, out var v) ? new ElementId(v) : ElementId.InvalidElementId)
                .Where(id => id != ElementId.InvalidElementId)
                .ToList();

            if (selectedIds.Count == 0) return;

            using (var tx = new Transaction(_doc, "Dimension Selected Elements On Current View"))
            {
                tx.Start();

                var targets = new List<Element>();

                foreach (var id in selectedIds)
                {
                    var e = _doc.GetElement(id);
                    if (e == null || e.Category == null) continue;

                    if (e.get_BoundingBox(view) == null) continue;

                    targets.Add(e);
                }

                if (targets.Count == 0)
                {
                    tx.Commit();
                    return;
                }

                var dimensionTypeId = GetDimensionTypeId(dimensionTypeName);
                var beforeIds = GetDimensionIds(view);

                AutoDimensionView(view, targets, sides, includeOverall);
                ApplyDimensionTypeToNewDimensions(view, beforeIds, dimensionTypeId);

                tx.Commit();
            }
        }

        private ElementId GetDimensionTypeId(string dimensionTypeName)
        {
            if (string.IsNullOrWhiteSpace(dimensionTypeName))
                return ElementId.InvalidElementId;

            var type = new FilteredElementCollector(_doc)
                .OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
                .FirstOrDefault(x => x.Name.Equals(dimensionTypeName, StringComparison.OrdinalIgnoreCase));

            return type?.Id ?? ElementId.InvalidElementId;
        }
        private HashSet<ElementId> GetDimensionIds(View view)
        {
            return new FilteredElementCollector(_doc, view.Id)
                .OfClass(typeof(Dimension))
                .ToElementIds()
                .ToHashSet();
        }

        private void ApplyDimensionTypeToNewDimensions(View view, HashSet<ElementId> beforeIds, ElementId dimensionTypeId)
        {
            if (dimensionTypeId == ElementId.InvalidElementId)
                return;

            var currentIds = new FilteredElementCollector(_doc, view.Id)
                .OfClass(typeof(Dimension))
                .ToElementIds();

            foreach (var id in currentIds)
            {
                if (beforeIds.Contains(id))
                    continue;

                var dim = _doc.GetElement(id) as Dimension;
                if (dim == null)
                    continue;

                try
                {
                    dim.ChangeTypeId(dimensionTypeId);
                }
                catch
                {
                }
            }
        }
    }
}
