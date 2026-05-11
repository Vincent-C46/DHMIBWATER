using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class WaterReservoirDimensionService
    {
        private readonly Document _doc;

        public WaterReservoirDimensionService(Document doc)
        {
            _doc = doc;
        }

        public void ApplyToSheet(string sheetId, string dimensionTypeName)
        {
            if (!long.TryParse(sheetId, out var sid)) return;

            var sheet = _doc.GetElement(new ElementId(sid)) as ViewSheet;
            if (sheet == null) return;

            using (var tx = new Transaction(_doc, "Water Reservoir Dimensions"))
            {
                var dimensionTypeId = FindDimensionTypeId(dimensionTypeName);

                tx.Start();
                _doc.Regenerate();

                var viewportIds = sheet.GetAllViewports();
                if (viewportIds == null || viewportIds.Count == 0)
                    return;

                foreach (var vpId in viewportIds)
                {
                    _doc.Regenerate();

                    var vp = _doc.GetElement(vpId) as Viewport;
                    if (vp == null) continue;

                    var view = _doc.GetElement(vp.ViewId) as View;

                    if (view == null) continue;
                    if (view is not ViewPlan && view is not ViewSection) continue;

                    if (view.Name.Contains("KeyMap", StringComparison.OrdinalIgnoreCase) ||
                        view.Name.Contains("KEY PLAN", StringComparison.OrdinalIgnoreCase))
                        continue;

                    ApplyToView(view, dimensionTypeId);
                }

                tx.Commit();
            }
        }

        private ElementId FindDimensionTypeId(string dimensionTypeName)
        {
            if (string.IsNullOrWhiteSpace(dimensionTypeName))
                return ElementId.InvalidElementId;

            var dimType = new FilteredElementCollector(_doc)
                .OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
                .FirstOrDefault(x => x.Name.Equals(dimensionTypeName, StringComparison.OrdinalIgnoreCase));

            return dimType?.Id ?? ElementId.InvalidElementId;
        }


        private void ApplyToView(View view, ElementId dimensionTypeId)
        {
            var preset = GetPreset(view);
            if (preset == null) return;

            _doc.Regenerate();

            var allCandidates = new FilteredElementCollector(_doc, view.Id)

                        .WhereElementIsNotElementType()
                .Where(e => e.Category != null)
                .OrderBy(e => e.Id.Value)
                .ToList();

            var topTargets = preset.UseTop ? allCandidates.Where(e => MatchesRule(e, preset.TopRule)).ToList() : new List<Element>();
            var bottomTargets = preset.UseBottom ? allCandidates.Where(e => MatchesRule(e, preset.BottomRule)).ToList() : new List<Element>();
            var leftTargets = preset.UseLeft ? allCandidates.Where(e => MatchesRule(e, preset.LeftRule)).ToList() : new List<Element>();
            var rightTargets = preset.UseRight ? allCandidates.Where(e => MatchesRule(e, preset.RightRule)).ToList() : new List<Element>();

            var extentTargets = allCandidates
                .Where(e =>
                    (preset.UseTop && MatchesRule(e, preset.TopRule)) ||
                    (preset.UseBottom && MatchesRule(e, preset.BottomRule)) ||
                    (preset.UseLeft && MatchesRule(e, preset.LeftRule)) ||
                    (preset.UseRight && MatchesRule(e, preset.RightRule)))
                .ToList();


            if (extentTargets.Count == 0) return;

            var right = view.RightDirection.Normalize();
            var up = view.UpDirection.Normalize();

            var topRefs = new List<FaceRef>();
            var bottomRefs = new List<FaceRef>();
            var leftRefs = new List<FaceRef>();
            var rightRefs = new List<FaceRef>();


            GetModelExtents(view, extentTargets, right, up, out var minRightAll, out var maxRightAll, out var minUpAll, out var maxUpAll);
            GetModelExtents(view, leftTargets, right, up, out var minRightLeft, out var maxRightLeft, out var minUpLeft, out var maxUpLeft);
            GetModelExtents(view, rightTargets, right, up, out var minRightRight, out var maxRightRight, out var minUpRight, out var maxUpRight);


            foreach (var e in topTargets)
            {
                TryGetFaceRefs(view, e, right, up, out var minR, out var maxR, out var minU, out var maxU);
                if (minR != null) topRefs.Add(minR);
                if (maxR != null) topRefs.Add(maxR);
            }

            foreach (var e in bottomTargets)
            {
                TryGetFaceRefs(view, e, right, up, out var minR, out var maxR, out var minU, out var maxU);
                if (minR != null) bottomRefs.Add(minR);
                if (maxR != null) bottomRefs.Add(maxR);
            }

            foreach (var e in leftTargets)
            {
                TryGetFaceRefs(view, e, right, up, out var minR, out var maxR, out var minU, out var maxU);
                if (minU != null) leftRefs.Add(minU);
                if (maxU != null) leftRefs.Add(maxU);
            }

            foreach (var e in rightTargets)
            {
                TryGetFaceRefs(view, e, right, up, out var minR, out var maxR, out var minU, out var maxU);
                if (minU != null) rightRefs.Add(minU);
                if (maxU != null) rightRefs.Add(maxU);
            }

            leftRefs = CollapseNearbyRefs(leftRefs, 1.0);
            rightRefs = CollapseNearbyRefs(rightRefs, 1.0);

            _doc.Regenerate();

            bool isPlanView =
                 view.Name.StartsWith("수조부 상부슬래브_시트", StringComparison.OrdinalIgnoreCase) ||
                 view.Name.StartsWith("밸브실 중간슬래브_시트", StringComparison.OrdinalIgnoreCase) ||
                 view.Name.StartsWith("수조부 바닥슬래브_시트", StringComparison.OrdinalIgnoreCase);

                                               //평면  단면//
            double segmentOffset = isPlanView ? 12.0 : 5.0;
            double overallOffset = isPlanView ? 17.0 : 10.0;


            if (preset.UseTop && topRefs.Count >= 2)
                CreateSegmentDimensionsAtTop(view, topRefs, right, up, maxUpAll + segmentOffset, dimensionTypeId);
            if (preset.UseTopOverall && topRefs.Count >= 2)
                CreateOverallDimensionAtTop(view, topRefs, right, up, maxUpAll + overallOffset, dimensionTypeId);

            if (preset.UseBottom && bottomRefs.Count >= 2)
                CreateSegmentDimensionAtBottom(view, bottomRefs, right, up, minUpAll - segmentOffset, dimensionTypeId);
            if (preset.UseBottomOverall && bottomRefs.Count >= 2)
                CreateOverallDimensionAtBottom(view, bottomRefs, right, up, minUpAll - overallOffset, dimensionTypeId);

            if (preset.UseLeft && leftRefs.Count >= 2)
                CreateSegmentDimensionsAtLeft(view, leftRefs, up, right, minRightLeft - segmentOffset, dimensionTypeId);
            if (preset.UseLeftOverall && leftRefs.Count >= 2)
                CreateOverallDimensionsAtLeft(view, leftRefs, up, right, minRightLeft - overallOffset, dimensionTypeId);

            if (preset.UseRight && rightRefs.Count >= 2)
                CreateSegmentDimensionAtRight(view, rightRefs, up, right, maxRightRight + segmentOffset, dimensionTypeId);
            if (preset.UseRightOverall && rightRefs.Count >= 2)
                CreateOverallDimensionAtRight(view, rightRefs, up, right, maxRightRight + overallOffset, dimensionTypeId);

        }

        private void TryGetFaceRefs(
                     View view, Element e, XYZ right, XYZ up,
                     out FaceRef minRight, out FaceRef maxRight,
                     out FaceRef minUp, out FaceRef maxUp)
        {
            minRight = maxRight = minUp = maxUp = null;

            var opt = new Options { View = view, ComputeReferences = true };
            var geo = e.get_Geometry(opt);
            if (geo == null) return;

            var ebb = e.get_BoundingBox(view);
            if (ebb == null) return;

            const double dirTol = 0.01;
            const double areaTol = 1e-4;
            const double bboxTol = 1.0;

            var corners = new[]
            {
                new XYZ(ebb.Min.X, ebb.Min.Y, ebb.Min.Z),
                new XYZ(ebb.Min.X, ebb.Max.Y, ebb.Min.Z),
                new XYZ(ebb.Max.X, ebb.Min.Y, ebb.Min.Z),
                new XYZ(ebb.Max.X, ebb.Max.Y, ebb.Min.Z),
                new XYZ(ebb.Min.X, ebb.Min.Y, ebb.Max.Z),
                new XYZ(ebb.Min.X, ebb.Max.Y, ebb.Max.Z),
                new XYZ(ebb.Max.X, ebb.Min.Y, ebb.Max.Z),
                new XYZ(ebb.Max.X, ebb.Max.Y, ebb.Max.Z),
            };

            var elemMinR = corners.Min(p => p.DotProduct(right));
            var elemMaxR = corners.Max(p => p.DotProduct(right));
            var elemMinU = corners.Min(p => p.DotProduct(up));
            var elemMaxU = corners.Max(p => p.DotProduct(up));

            var minRightCandidates = new List<FaceRef>();
            var maxRightCandidates = new List<FaceRef>();
            var minUpCandidates = new List<FaceRef>();
            var maxUpCandidates = new List<FaceRef>();

            foreach (var obj in geo)
            {
                var solid = obj as Solid;
                if (solid == null || solid.Faces.IsEmpty) continue;

                foreach (Face f in solid.Faces)
                {
                    if (f is not PlanarFace pf || pf.Reference == null) continue;
                    if (pf.Area < areaTol) continue;

                    var n = pf.FaceNormal.Normalize();

                    var alignedRight = Math.Abs(Math.Abs(n.DotProduct(right)) - 1.0) < dirTol;
                    var alignedUp = Math.Abs(Math.Abs(n.DotProduct(up)) - 1.0) < dirTol;

                    if (!alignedRight && !alignedUp)
                        continue;

                    var bb = pf.GetBoundingBox();
                    var uv = (bb.Min + bb.Max) * 0.5;
                    var p = pf.Evaluate(uv);

                    var pr = p.DotProduct(right);
                    var pu = p.DotProduct(up);

                    var stableKey = pf.Reference.ConvertToStableRepresentation(_doc);
                    var elementCode = e.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty;
                    var elementIdValue = e.Id.Value;

                    var itemR = new FaceRef(pf.Reference, pr, pf.Area, stableKey, elementCode, elementIdValue);
                    var itemU = new FaceRef(pf.Reference, pu, pf.Area, stableKey, elementCode, elementIdValue);

                    if (alignedRight)
                    {
                        if (Math.Abs(pr - elemMinR) <= bboxTol)
                            minRightCandidates.Add(itemR);

                        if (Math.Abs(pr - elemMaxR) <= bboxTol)
                            maxRightCandidates.Add(itemR);
                    }

                    if (alignedUp)
                    {
                        if (Math.Abs(pu - elemMinU) <= bboxTol)
                            minUpCandidates.Add(itemU);

                        if (Math.Abs(pu - elemMaxU) <= bboxTol)
                            maxUpCandidates.Add(itemU);
                    }
                }
            }

            minRight = minRightCandidates
                .OrderByDescending(x => x.Area)
                .ThenBy(x => x.StableKey)
                .FirstOrDefault();

            maxRight = maxRightCandidates
                .OrderByDescending(x => x.Area)
                .ThenBy(x => x.StableKey)
                .FirstOrDefault();

            minUp = minUpCandidates
                .OrderByDescending(x => x.Area)
                .ThenBy(x => x.StableKey)
                .FirstOrDefault();

            maxUp = maxUpCandidates
                .OrderByDescending(x => x.Area)
                .ThenBy(x => x.StableKey)
                .FirstOrDefault();
        }



        private class FaceRef
        {
            public Reference Reference { get; }
            public double Projection { get; }
            public double Area { get; }
            public string StableKey { get; }
            public string ElementCode { get; }
            public long ElementIdValue { get; }

            public FaceRef(
                Reference reference, double projection, double area, 
                string stableKey, string elementCode, long elementIdValue)
            {
                Reference = reference;
                Projection = projection;
                Area = area;
                StableKey = stableKey ?? string.Empty;
                ElementCode = elementCode ?? string.Empty;
                ElementIdValue = elementIdValue;
            }
        }

        private bool MatchesRule(Element e, DimensionFilterRule rule)
        {
            if (e?.Category == null) return false;

            var bic = (BuiltInCategory)e.Category.Id.Value;

            if (rule.ExcludeCategories.Contains(bic))
                return false;
            if (!string.IsNullOrWhiteSpace(rule.ExcludeParameterName) &&
                !string.IsNullOrWhiteSpace(rule.ExcludeParameterValue) &&
                MatchesParameter(e, rule.ExcludeParameterName, rule.ExcludeParameterValue))
                return false;

            var names = new List<string>();

            if (!string.IsNullOrWhiteSpace(e.Name))
                names.Add(e.Name);

            var type = _doc.GetElement(e.GetTypeId());
            if (type != null && !string.IsNullOrWhiteSpace(type.Name))
                names.Add(type.Name);

            if (e is FamilyInstance fi)
            {
                if (!string.IsNullOrWhiteSpace(fi.Symbol?.Name))
                    names.Add(fi.Symbol.Name);

                if (!string.IsNullOrWhiteSpace(fi.Symbol?.FamilyName))
                    names.Add(fi.Symbol.FamilyName);
            }


            if (rule.ExcludeNameKeywords.Length > 0 &&
                names.Any(n => rule.ExcludeNameKeywords.Any(k => n.Contains(k, StringComparison.OrdinalIgnoreCase))))
                return false;

            bool hasIncludeCategoryRule = rule.IncludeCategories.Count > 0;
            bool hasIncludeNameRule = rule.IncludeNameKeywords.Length > 0;
            bool hasIncludeParameterRule =
                !string.IsNullOrWhiteSpace(rule.IncludeParameterName) &&
                !string.IsNullOrWhiteSpace(rule.IncludeParameterValue);

            bool categoryMatched = !hasIncludeCategoryRule || rule.IncludeCategories.Contains(bic);

            bool nameMatched = !hasIncludeNameRule ||
                names.Any(n => rule.IncludeNameKeywords.Any(k => n.Contains(k, StringComparison.OrdinalIgnoreCase)));

            bool parameterMatched = !hasIncludeParameterRule ||
                MatchesParameter(e, rule.IncludeParameterName, rule.IncludeParameterValue);

            return categoryMatched && nameMatched && parameterMatched;

        }

        private ViewDimensionPreset GetPreset(View view)
        {
            if (view == null) return null;

            if (Presets.TryGetValue(view.Name, out var preset))
                return preset;

            foreach (var pair in Presets)
            {
                if (view.Name.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }

            return null;
        }

        private class ViewDimensionPreset
        {
            public bool UseTop { get; set; }
            public bool UseBottom { get; set; }
            public bool UseLeft { get; set; }
            public bool UseRight { get; set; }
            public bool UseTopOverall { get; set; }
            public bool UseBottomOverall { get; set; }
            public bool UseLeftOverall { get; set; }
            public bool UseRightOverall { get; set; }


            public DimensionFilterRule TopRule { get; } = new();
            public DimensionFilterRule BottomRule { get; } = new();
            public DimensionFilterRule LeftRule { get; } = new();
            public DimensionFilterRule RightRule { get; } = new();
        }


        private static List<FaceRef> BuildOrderedDistinctRefs(List<FaceRef> refs)
        {
            var ordered = refs
                .OrderBy(r => r.Projection)
                .ThenBy(r => GetElementCodePriority(r.ElementCode))
                .ThenByDescending(r => r.Area)
                .ThenBy(r => r.ElementIdValue)
                .ThenBy(r => r.StableKey)
                .ToList();

            var filtered = new List<FaceRef>();
            const double tol = 1.0;

            foreach (var r in ordered)
            {
                if (filtered.Count == 0 || Math.Abs(r.Projection - filtered[^1].Projection) > tol)
                    filtered.Add(r);
            }

            return filtered;
        }

        private static List<FaceRef> CollapseNearbyRefs(List<FaceRef> refs, double tol)
        {
            var ordered = refs
                .OrderBy(r => r.Projection)
                .ThenBy(r => GetElementCodePriority(r.ElementCode))
                .ThenByDescending(r => r.Area)
                .ThenBy(r => r.ElementIdValue)
                .ThenBy(r => r.StableKey)
                .ToList();
            var result = new List<FaceRef>();

            foreach (var r in ordered)
            {
                if (result.Count == 0)
                {
                    result.Add(r);
                    continue;
                }

                var last = result[^1];
                if (Math.Abs(r.Projection - last.Projection) <= tol)
                {
                    var rPriority = GetElementCodePriority(r.ElementCode);
                    var lastPriority = GetElementCodePriority(last.ElementCode);

                    if (rPriority < lastPriority ||
                        (rPriority == lastPriority && r.Area > last.Area) ||
                        (rPriority == lastPriority && Math.Abs(r.Area - last.Area) < 1e-9 && r.ElementIdValue < last.ElementIdValue) ||
                        (rPriority == lastPriority && Math.Abs(r.Area - last.Area) < 1e-9 && r.ElementIdValue == last.ElementIdValue &&
                         string.CompareOrdinal(r.StableKey, last.StableKey) < 0))
                    {
                        result[^1] = r;
                    }
                }

                else
                {
                    result.Add(r);
                }
            }

            return result;
        }

        private void CreateSegmentDimensionsAtTop(View view, List<FaceRef> refs, XYZ lineDir, XYZ upDir, double topCoord, ElementId dimensionTypeId)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            for (int i = 0; i < filtered.Count - 1; i++)
            {
                var first = filtered[i];
                var second = filtered[i + 1];

                var p1 = lineDir * first.Projection + upDir * topCoord;
                var p2 = lineDir * second.Projection + upDir * topCoord;

                TryCreateDimension(view, p1, p2, first.Reference, second.Reference, dimensionTypeId);
            }
        }
        private void CreateOverallDimensionAtTop(View view, List<FaceRef> refs, XYZ rightDir, XYZ upDir, double topCoord, ElementId dimensionTypeId)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            var first = filtered.First();
            var last = filtered.Last();

            var p1 = rightDir * first.Projection + upDir * topCoord;
            var p2 = rightDir * last.Projection + upDir * topCoord;

            TryCreateDimension(view, p1, p2, first.Reference, last.Reference, dimensionTypeId);
        }

        private void CreateSegmentDimensionsAtLeft(View view, List<FaceRef> refs, XYZ lineDir, XYZ rightDir, double leftCoord, ElementId dimensionTypeId)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            for (int i = 0; i < filtered.Count - 1; i++)
            {
                var first = filtered[i];
                var second = filtered[i + 1];

                var p1 = lineDir * first.Projection + rightDir * leftCoord;
                var p2 = lineDir * second.Projection + rightDir * leftCoord;

                TryCreateDimension(view, p1, p2, first.Reference, second.Reference, dimensionTypeId);

            }
        }

        private void CreateOverallDimensionsAtLeft(View view, List<FaceRef> refs, XYZ upDir, XYZ rightDir, double leftCoord, ElementId dimensionTypeId)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            var first = filtered.First();
            var last = filtered.Last();

            var p1 = upDir * first.Projection + rightDir * leftCoord;
            var p2 = upDir * last.Projection + rightDir * leftCoord;

            TryCreateDimension(view, p1, p2, first.Reference, last.Reference, dimensionTypeId);
        }

        private void CreateSegmentDimensionAtBottom(View view, List<FaceRef> refs, XYZ lineDir, XYZ upDir, double bottomCoord, ElementId dimensionTypeId)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            for (int i = 0; i < filtered.Count - 1; i++)
            {
                var first = filtered[i];
                var second = filtered[i + 1];

                var p1 = lineDir * first.Projection + upDir * bottomCoord;
                var p2 = lineDir * second.Projection + upDir * bottomCoord;

                TryCreateDimension(view, p1, p2, first.Reference, second.Reference, dimensionTypeId);
            }
        }

        private void CreateOverallDimensionAtBottom(View view, List<FaceRef> refs, XYZ rightDir, XYZ upDir, double bottomCoord, ElementId dimensionTypeId)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            var first = filtered.First();
            var last = filtered.Last();

            var p1 = rightDir * first.Projection + upDir * bottomCoord;
            var p2 = rightDir * last.Projection + upDir * bottomCoord;

            TryCreateDimension(view, p1, p2, first.Reference, last.Reference, dimensionTypeId);
        }
        private void CreateSegmentDimensionAtRight(View view, List<FaceRef> refs, XYZ lineDir, XYZ rightDir, double rightCoord, ElementId dimensionTypeId)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            for (int i = 0; i < filtered.Count - 1; i++)
            {
                var first = filtered[i];
                var second = filtered[i + 1];

                var p1 = lineDir * first.Projection + rightDir * rightCoord;
                var p2 = lineDir * second.Projection + rightDir * rightCoord;

                TryCreateDimension(view, p1, p2, first.Reference, second.Reference, dimensionTypeId);
            }
        }

        private void CreateOverallDimensionAtRight(View view, List<FaceRef> refs, XYZ upDir, XYZ rightDir, double rightCoord, ElementId dimensionTypeId)
        {
            var filtered = BuildOrderedDistinctRefs(refs);
            if (filtered.Count < 2) return;

            var first = filtered.First();
            var last = filtered.Last();

            var p1 = upDir * first.Projection + rightDir * rightCoord;
            var p2 = upDir * last.Projection + rightDir * rightCoord;

            TryCreateDimension(view, p1, p2, first.Reference, last.Reference, dimensionTypeId);

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

            if (minRight == double.MaxValue)
            {
                minRight = maxRight = minUp = maxUp = 0;
            }
        }

        private bool HasSameReferenceDimensionAtSamePlace(View view, XYZ p1, XYZ p2, Reference firstRef, Reference secondRef)
        {
            if (view == null || firstRef == null || secondRef == null)
                return false;

            var firstKey = firstRef.ConvertToStableRepresentation(_doc);
            var secondKey = secondRef.ConvertToStableRepresentation(_doc);

            const double pointTol = 1.0;

            var dims = new FilteredElementCollector(_doc, view.Id)
                .OfClass(typeof(Dimension))
                .Cast<Dimension>();

            foreach (var dim in dims)
            {
                var refs = dim.References;
                if (refs == null || refs.Size != 2)
                    continue;

                var a = refs.get_Item(0)?.ConvertToStableRepresentation(_doc);
                var b = refs.get_Item(1)?.ConvertToStableRepresentation(_doc);

                if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                    continue;

                var sameRefs =
                    (a == firstKey && b == secondKey) ||
                    (a == secondKey && b == firstKey);

                if (!sameRefs)
                    continue;

                if (dim.Curve is not Line line)
                    continue;

                if (!line.IsBound)
                    continue;

                var d0 = line.GetEndPoint(0);
                var d1 = line.GetEndPoint(1);

                var samePlace =
                    (d0.DistanceTo(p1) <= pointTol && d1.DistanceTo(p2) <= pointTol) ||
                    (d0.DistanceTo(p2) <= pointTol && d1.DistanceTo(p1) <= pointTol);

                if (samePlace)
                    return true;
            }

            return false;
        }

        private void TryCreateDimension(View view, XYZ p1, XYZ p2, Reference firstRef, Reference secondRef, ElementId dimensionTypeId)
        {
            if (view == null || firstRef == null || secondRef == null)
                return;

            if (p1.DistanceTo(p2) < 1e-6)
                return;

            if (HasSameReferenceDimensionAtSamePlace(view, p1, p2, firstRef, secondRef))
                return;

            var ra = new ReferenceArray();

            ra.Append(firstRef);
            ra.Append(secondRef);

            try
            {
                var dim = _doc.Create.NewDimension(view, Line.CreateBound(p1, p2), ra);
                if (dim != null && dimensionTypeId != ElementId.InvalidElementId)
                    dim.ChangeTypeId(dimensionTypeId);
            }
            catch (Exception ex)
            {
                
            }
        }


        private static readonly Dictionary<string, ViewDimensionPreset> Presets = new(StringComparer.OrdinalIgnoreCase);

        private class DimensionFilterRule
        {
            public HashSet<BuiltInCategory> IncludeCategories { get; } = new();
            public HashSet<BuiltInCategory> ExcludeCategories { get; } = new();
            public string[] IncludeNameKeywords { get; set; } = Array.Empty<string>();
            public string[] ExcludeNameKeywords { get; set; } = Array.Empty<string>();
            public string IncludeParameterName { get; set; }
            public string IncludeParameterValue { get; set; }
            public string ExcludeParameterName { get; set; }
            public string ExcludeParameterValue { get; set; }
        }

        private bool MatchesParameter(Element e, string parameterName, string expectedValue)
        {
            if (e == null) return false;
            if (string.IsNullOrWhiteSpace(parameterName)) return false;
            if (string.IsNullOrWhiteSpace(expectedValue)) return false;

            var p = e.LookupParameter(parameterName);
            if (p == null) return false;

            string actual = p.AsString();

            if (string.IsNullOrWhiteSpace(actual))
                actual = p.AsValueString();

            if (string.IsNullOrWhiteSpace(actual))
            {
                switch (p.StorageType)
                {
                    case StorageType.Integer:
                        actual = p.AsInteger().ToString();
                        break;
                    case StorageType.Double:
                        actual = p.AsDouble().ToString();
                        break;
                    case StorageType.ElementId:
                        actual = p.AsElementId().Value.ToString();
                        break;
                }
            }
            if (string.IsNullOrWhiteSpace(actual))
                return false;

            var expectedValues = expectedValue
                .Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            return expectedValues.Any(x =>
                string.Equals(actual.Trim(), x, StringComparison.OrdinalIgnoreCase));
        }



        // 뷰별 치수 생성 규칙 지정 //
        //////////////////////////////
        static WaterReservoirDimensionService()
        {

            // 수조부 상부 슬래브 //
            var slabTop = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // Sample //
            //slabTop.BottomRule.IncludeCategories.Add(BuiltInCategory.OST_Floors);
            //slabTop.TopRule.IncludeParameterName = "DH_ElemnetCode";
            //slabTop.TopRule.IncludeParameterValue = "S1, S2, W3, W4, G1, W9";

            //slabTop.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            //slabTop.TopRule.ExcludeParameterName = "ITEM NAME";
            //slabTop.TopRule.ExcludeParameterValue = "SKIP";
            //slabTop.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 상부 //
            slabTop.TopRule.IncludeParameterName = "DH_ElementCode";
            slabTop.TopRule.IncludeParameterValue = "S1, W1, W2, W3, W6, G1";
            slabTop.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            slabTop.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            slabTop.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            slabTop.BottomRule.IncludeParameterName = "DH_ElementCode";
            slabTop.BottomRule.IncludeParameterValue = "S2, W4, W5, W6, W7, W8, W9, W10 ";
            slabTop.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            slabTop.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            slabTop.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            slabTop.LeftRule.IncludeParameterName = "DH_ElementCode";
            slabTop.LeftRule.IncludeParameterValue = "S1, S2, W3, W4, G1, W9";
            slabTop.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            slabTop.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            slabTop.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            slabTop.RightRule.IncludeParameterName = "DH_ElementCode";
            slabTop.RightRule.IncludeParameterValue = "S1, S2, W3, W4, G1, W9";
            slabTop.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            slabTop.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            slabTop.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };


            // 수조부 중간 슬래브 //
            var slabMid = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            slabMid.TopRule.IncludeParameterName = "DH_ElementCode";
            slabMid.TopRule.IncludeParameterValue = "B1, H2, W1, W2, W3, W6, C1";
            slabMid.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            slabMid.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            slabMid.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            slabMid.BottomRule.IncludeParameterName = "DH_ElementCode";
            slabMid.BottomRule.IncludeParameterValue = "W1, W2, W4, W5, W6, W7, W8, W9, W10, MS1";
            slabMid.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            slabMid.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            slabMid.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            slabMid.LeftRule.IncludeParameterName = "DH_ElementCode";
            slabMid.LeftRule.IncludeParameterValue = "B1, W1, W3, W4, C1, W9";
            slabMid.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            slabMid.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            slabMid.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };
            // 우측 //
            slabMid.RightRule.IncludeParameterName = "DH_ElementCode";
            slabMid.RightRule.IncludeParameterValue = "B1, W2, W3, W4, C1, W9";
            slabMid.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            slabMid.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            slabMid.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            var slabBottom = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            slabBottom.TopRule.IncludeParameterName = "DH_ElementCode";
            slabBottom.TopRule.IncludeParameterValue = "B1, W1, W2, W3, W6, C1";
            slabBottom.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            slabBottom.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            slabBottom.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            slabBottom.BottomRule.IncludeParameterName = "DH_ElementCode";
            slabBottom.BottomRule.IncludeParameterValue = "B4, W4, W5, W6, W7, W8, W9";
            slabBottom.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            slabBottom.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            slabBottom.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            slabBottom.LeftRule.IncludeParameterName = "DH_ElementCode";
            slabBottom.LeftRule.IncludeParameterValue = "B1, W1, W3, W4, C1, W9";
            slabBottom.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            slabBottom.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            slabBottom.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            slabBottom.RightRule.IncludeParameterName = "DH_ElementCode";
            slabBottom.RightRule.IncludeParameterValue = "B1, W2, W3, W4, C1, W9";
            slabBottom.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            slabBottom.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            slabBottom.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };



            var SectionA = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionA.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionA.TopRule.IncludeParameterValue = "S1, C1, W1, W2, W6";
            SectionA.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionA.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionA.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionA.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionA.BottomRule.IncludeParameterValue = "B1, B2, B3, C1, W1, W2, W6";
            SectionA.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionA.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionA.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionA.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionA.LeftRule.IncludeParameterValue = "S1, B1, B2, B3, L1, W1, G1";
            SectionA.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionA.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionA.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionA.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionA.RightRule.IncludeParameterValue = "S1, B1, B2, B3, L1, W2, G1";
            SectionA.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionA.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionA.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            var SectionB = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionB.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionB.TopRule.IncludeParameterValue = "S1, B1, B2, B3, L1, W2, G1";
            SectionB.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionB.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionB.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionB.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionB.BottomRule.IncludeParameterValue = "B1, W1, W2, C1, L1";
            SectionB.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionB.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionB.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionB.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionB.LeftRule.IncludeParameterValue = "S1, B1, W1, G1, L1";        
            SectionB.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionB.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionB.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionB.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionB.RightRule.IncludeParameterValue = "S1, B1, W2, G1, L1";
            SectionB.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionB.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionB.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            var SectionC = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionC.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionC.TopRule.IncludeParameterValue = "S2, W7, W8, W10";
            SectionC.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionC.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionC.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionC.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionC.BottomRule.IncludeParameterValue = "L4, B4, W7, W8, W10";
            SectionC.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionC.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionC.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionC.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionC.LeftRule.IncludeParameterValue = "S2, MS1, B4, L4, W7";       
            SectionC.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionC.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionC.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionC.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionC.RightRule.IncludeParameterValue = "S2, MS1, B4, L4, W8";          
            SectionC.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionC.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionC.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            var SectionD = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionD.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionD.TopRule.IncludeParameterValue = "S2, W7, W8, W10";
            SectionD.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionD.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionD.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionD.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionD.BottomRule.IncludeParameterValue = "B4, L4, W7, W8";
            SectionD.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionD.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionD.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionD.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionD.LeftRule.IncludeParameterValue = "S2, MS1, B4, L4";
            SectionD.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionD.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionD.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionD.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionD.RightRule.IncludeParameterValue = "S2, MS1, B4, L4";
            SectionD.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionD.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionD.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            var SectionE = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionE.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionE.TopRule.IncludeParameterValue = "S1, W3, W4, C1";
            SectionE.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionE.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionE.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionE.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionE.BottomRule.IncludeParameterValue = "B1, L1, W3, W4, C1";
            SectionE.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionE.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionE.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionE.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionE.LeftRule.IncludeParameterValue = "S1, G1, B1, L1, W3";
            SectionE.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionE.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionE.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionE.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionE.RightRule.IncludeParameterValue = "S1, G1, B1, L1, W4";
            SectionE.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionE.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionE.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            var SectionF = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionF.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionF.TopRule.IncludeParameterValue = "S1, S2, W3, W5, W6, W9, G1";
            SectionF.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionF.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionF.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionF.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionF.BottomRule.IncludeParameterValue = "B1, B2, B4, L1, L2, L4, W4, W5, W6, W9, G1";
            SectionF.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionF.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionF.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionF.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionF.LeftRule.IncludeParameterValue = "S1, B1, B2, B3, L1, L2, H1, H3, W3";
            SectionF.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionF.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionF.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionF.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionF.RightRule.IncludeParameterValue = "S2, B4, L4, MS1, W9, H2, B1, B3";
            SectionF.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionF.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionF.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };


            var SectionG = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionG.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionG.TopRule.IncludeParameterValue = "W3, W5, W9, C1, G1";
            SectionG.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionG.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionG.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionG.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionG.BottomRule.IncludeParameterValue = "B1, B2, B4, W3, W5, W9, C1";
            SectionG.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionG.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionG.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionG.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionG.LeftRule.IncludeParameterValue = "S1, B1, B2, B3, L1, L2, W3";
            SectionG.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionG.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionG.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionG.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionG.RightRule.IncludeParameterValue = "S2, MS1, B4, L4, W9";
            SectionG.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionG.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionG.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };


            var SectionH = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionH.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionH.TopRule.IncludeParameterValue = "S1, W3, W5, W9, C1";
            SectionH.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionH.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionH.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionH.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionH.BottomRule.IncludeParameterValue = "B1, B4, L1, L4, W3, W5, W9, C1";
            SectionH.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionH.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionH.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionH.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionH.LeftRule.IncludeParameterValue = "S1, B1, L1, W3, G1";
            SectionH.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionH.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionH.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionH.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionH.RightRule.IncludeParameterValue = "S2, H4, MS1, H3. B4, L4";
            SectionH.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionH.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionH.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };


            var SectionI = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionI.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionI.TopRule.IncludeParameterValue = "S1, W3, W5, W9, C1";
            SectionI.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionI.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionI.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionI.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionI.BottomRule.IncludeParameterValue = "B1, B4, L1, L4, W3, W5, W9, C1";
            SectionI.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionI.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionI.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionI.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionI.LeftRule.IncludeParameterValue = "S1, B1, L1, W3, G1";
            SectionI.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionI.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionI.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionI.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionI.RightRule.IncludeParameterValue = "S2, H4, MS1, H3. B4, L4";
            SectionI.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionI.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionI.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };


            var SectionJ = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionJ.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionJ.TopRule.IncludeParameterValue = "S1, W3, W5, W9, C1";
            SectionJ.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionJ.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionJ.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionJ.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionJ.BottomRule.IncludeParameterValue = "B1, B4, L1, L4, W3, W5, W9, C1";
            SectionJ.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionJ.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionJ.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionJ.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionJ.LeftRule.IncludeParameterValue = "S1, B1, L1, W3, G1";
            SectionJ.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionJ.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionJ.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionJ.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionJ.RightRule.IncludeParameterValue = "S2, H4, MS1, H3. B4, L4";
            SectionJ.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionJ.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionJ.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };


            var SectionK = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionK.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionK.TopRule.IncludeParameterValue = "S1, W3, W5, W9, C1";
            SectionK.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionK.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionK.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionK.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionK.BottomRule.IncludeParameterValue = "B1, B4, L1, L4, W3, W5, W9, C1";
            SectionK.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionK.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionK.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionK.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionK.LeftRule.IncludeParameterValue = "S1, B1, L1, W3, G1";
            SectionK.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionK.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionK.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionK.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionK.RightRule.IncludeParameterValue = "S2, H4, MS1, H3. B4, L4";
            SectionK.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionK.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionK.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };


            var SectionL = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionL.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionL.TopRule.IncludeParameterValue = "S1, W3, W5, W9, C1";
            SectionL.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionL.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionL.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionL.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionL.BottomRule.IncludeParameterValue = "B1, B4, L1, L4, W3, W5, W9, C1";
            SectionL.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionL.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionL.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionL.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionL.LeftRule.IncludeParameterValue = "S1, B1, L1, W3, G1";
            SectionL.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionL.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionL.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionL.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionL.RightRule.IncludeParameterValue = "S2, H4, MS1, H3. B4, L4";
            SectionL.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionL.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionL.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };


            var SectionM = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionM.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionM.TopRule.IncludeParameterValue = "S1, W3, W5, W9, C1";
            SectionM.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionM.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionM.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionM.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionM.BottomRule.IncludeParameterValue = "B1, B4, L1, L4, W3, W5, W9, C1";
            SectionM.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionM.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionM.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionM.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionM.LeftRule.IncludeParameterValue = "S1, B1, L1, W3, G1";
            SectionM.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionM.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionM.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionM.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionM.RightRule.IncludeParameterValue = "S2, H4, MS1, H3. B4, L4";
            SectionM.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionM.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionM.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };


            var SectionN = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionN.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionN.TopRule.IncludeParameterValue = "S1, W3, W5, W9, C1";
            SectionN.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionN.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionN.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionN.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionN.BottomRule.IncludeParameterValue = "B1, B4, L1, L4, W3, W5, W9, C1";
            SectionN.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionN.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionN.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionN.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionN.LeftRule.IncludeParameterValue = "S1, B1, L1, W3, G1";
            SectionN.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionN.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionN.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionN.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionN.RightRule.IncludeParameterValue = "S2, H4, MS1, H3. B4, L4";
            SectionN.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionN.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionN.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };


            var SectionO = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionO.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionO.TopRule.IncludeParameterValue = "S1, W3, W5, W9, C1";
            SectionO.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionO.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionO.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionO.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionO.BottomRule.IncludeParameterValue = "B1, B4, L1, L4, W3, W5, W9, C1";
            SectionO.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionO.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionO.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionO.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionO.LeftRule.IncludeParameterValue = "S1, B1, L1, W3, G1";
            SectionO.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionO.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionO.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionO.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionO.RightRule.IncludeParameterValue = "S2, H4, MS1, H3. B4, L4";
            SectionO.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionO.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionO.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };


            var SectionP = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionP.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionP.TopRule.IncludeParameterValue = "S1, W3, W5, W9, C1";
            SectionP.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionP.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionP.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionP.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionP.BottomRule.IncludeParameterValue = "B1, B4, L1, L4, W3, W5, W9, C1";
            SectionP.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionP.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionP.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionP.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionP.LeftRule.IncludeParameterValue = "S1, B1, L1, W3, G1";
            SectionP.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionP.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionP.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionP.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionP.RightRule.IncludeParameterValue = "S2, H4, MS1, H3. B4, L4";
            SectionP.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionP.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionP.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };


            var SectionQ = new ViewDimensionPreset
            {
                UseTop = true,
                UseBottom = true,
                UseLeft = true,
                UseRight = true,

                UseTopOverall = true,
                UseBottomOverall = true,
                UseLeftOverall = true,
                UseRightOverall = true
            };

            // 상부 //
            SectionQ.TopRule.IncludeParameterName = "DH_ElementCode";
            SectionQ.TopRule.IncludeParameterValue = "S1, W3, W5, W9, C1";
            SectionQ.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionQ.TopRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionQ.TopRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 하부 //
            SectionQ.BottomRule.IncludeParameterName = "DH_ElementCode";
            SectionQ.BottomRule.IncludeParameterValue = "B1, B4, L1, L4, W3, W5, W9, C1";
            SectionQ.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionQ.BottomRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionQ.BottomRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 좌측 //
            SectionQ.LeftRule.IncludeParameterName = "DH_ElementCode";
            SectionQ.LeftRule.IncludeParameterValue = "S1, B1, L1, W3, G1";
            SectionQ.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionQ.LeftRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionQ.LeftRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };

            // 우측 //
            SectionQ.RightRule.IncludeParameterName = "DH_ElementCode";
            SectionQ.RightRule.IncludeParameterValue = "S2, H4, MS1, H3. B4, L4";
            SectionQ.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Stairs);
            SectionQ.RightRule.ExcludeCategories.Add(BuiltInCategory.OST_Railings);
            SectionQ.RightRule.ExcludeNameKeywords = new[] { "도류벽", "헌치" };


            // 시트 목록 //
            Presets["수조부 상부슬래브_시트"] = slabTop;
            Presets["밸브실 중간슬래브_시트"] = slabMid;
            Presets["수조부 바닥슬래브_시트"] = slabBottom;
            Presets["A_시트"] = SectionA;
            Presets["B_시트"] = SectionB;
            Presets["C_시트"] = SectionC;
            Presets["D_시트"] = SectionD;
            Presets["E_시트"] = SectionE;
            Presets["F_시트"] = SectionF;
            Presets["G_시트"] = SectionG;
            Presets["H_시트"] = SectionH;
            Presets["H_시트"] = SectionH;

            Presets["I_시트"] = SectionI;
            Presets["J_시트"] = SectionJ;
            Presets["K_시트"] = SectionK;
            Presets["L_시트"] = SectionL;
            Presets["M_시트"] = SectionM;
            Presets["N_시트"] = SectionN;
            Presets["O_시트"] = SectionO;
            Presets["P_시트"] = SectionP;
            Presets["Q_시트"] = SectionQ;



        }
        private static int GetElementCodePriority(string code)
        {
            return code switch
            {
                "W1" => 1,
                "W2" => 1,
                "W3" => 2,
                "W4" => 2,
                "B1" => 3,
                "B4" => 3,
                "C1" => 4,
                "L1" => 5,
                "H2" => 6,
                _ => 999
            };
        }
    }
}


