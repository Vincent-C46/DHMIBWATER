using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Storage;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Core.Settings;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Geometry
{
    public class RevitFaceClassifier : IFaceClassifier
    {
        private readonly Func<Document?> _doc;
        private readonly bool _useOpeningMinVolume;
        private readonly double _openingMinVolumeM3;

        public RevitFaceClassifier(Func<Document?> doc, IQuantitySettingsRepository settingsRepo)
        {
            _doc = doc;
            var deduction = settingsRepo.Load()?.Deduction ?? new DeductionSettings();
            _useOpeningMinVolume = deduction.UseOpeningMinVolume;
            _openingMinVolumeM3 = deduction.OpeningMinVolumeM3;
        }

        public IReadOnlyDictionary<FaceType, double> GetFaceAreas(long elementId)
        {
            var doc = _doc();
            if (doc == null) return new Dictionary<FaceType, double>();

            var elem = doc.GetElement(new ElementId(elementId));
            if (elem == null) return new Dictionary<FaceType, double>();

            double? openingThreshold = _useOpeningMinVolume ? _openingMinVolumeM3 : null;

            var result = new Dictionary<FaceType, double>();
            foreach (var face in RevitGeometryHelper.GetFaces(elem).OfType<PlanarFace>())
            {
                var faceType = Classify(elem, face, openingThreshold);
                if (faceType == FaceType.None) continue; // 최소 체적 미만 오프닝 — 거푸집 면적에서 제외
                result[faceType] = result.GetValueOrDefault(faceType) + UC.Ft2ToM2(face.Area);
            }
            return result;
        }

        /// <summary>접촉면 공제(FindContactAreas) 태깅용 — 오프닝 최소 체적 임계값 미적용.</summary>
        internal static FaceType Classify(Element elem, PlanarFace face) => Classify(elem, face, null);

        private static FaceType Classify(Element elem, PlanarFace face, double? openingMinVolumeM3) =>
            (BuiltInCategory)elem.Category.Id.Value switch
            {
                BuiltInCategory.OST_StructuralFraming    => ClassifyBeam(elem, face.FaceNormal),
                BuiltInCategory.OST_Walls                => ClassifyWall(elem, face, openingMinVolumeM3),
                BuiltInCategory.OST_Floors               => ClassifyFloor(elem, face, openingMinVolumeM3),
                BuiltInCategory.OST_StructuralFoundation => ClassifyFoundation(face.FaceNormal),
                BuiltInCategory.OST_StructuralColumns    => ClassifyColumn(face.FaceNormal),
                BuiltInCategory.OST_Stairs               => ClassifyStairs(elem, face.FaceNormal),
                _                                        => FaceType.Side,
            };

        private static FaceType ClassifyBeam(Element elem, XYZ normal)
        {
            if (normal.Z < -0.9) return FaceType.Bottom;
            if (normal.Z > 0.9) return FaceType.Top;

            if (elem.Location is not LocationCurve lc) return FaceType.Side;
            var dir = (lc.Curve.GetEndPoint(1) - lc.Curve.GetEndPoint(0)).Normalize();
            if (Math.Abs(normal.DotProduct(dir)) > 0.9) return FaceType.End;

            var right = dir.CrossProduct(XYZ.BasisZ).Normalize();
            return normal.DotProduct(right) >= 0 ? FaceType.Right : FaceType.Left;
        }

        private static FaceType ClassifyWall(Element elem, PlanarFace face, double? openingMinVolumeM3)
        {
            var normal = face.FaceNormal;
            if (normal.Z > 0.9) return FaceType.Top;
            if (normal.Z < -0.9) return FaceType.Bottom;
            if (elem is not Wall wall) return FaceType.Side;

            var dot = normal.DotProduct(wall.Orientation);
            if (Math.Abs(dot) > 0.9) return dot > 0 ? FaceType.Right : FaceType.Left;

            // 벽 방향(길이 방향) 법선 → End vs OpeningSide vs 최소 체적 미만 오프닝(제외)
            // Left/Right 메인면의 내부 EdgeLoop 엣지와 공유 여부로 판별
            try
            {
                foreach (var solid in RevitGeometryHelper.GetSolids(wall))
                {
                    var (large, small) = GetInnerLoopEdges(solid,
                        f => f is PlanarFace pf && Math.Abs(pf.FaceNormal.DotProduct(wall.Orientation)) > 0.9,
                        wall.Width, openingMinVolumeM3);

                    if (SharesEdgeWith(face, large)) return FaceType.OpeningSide;
                    if (SharesEdgeWith(face, small)) return FaceType.None;
                }
            }
            catch { }

            return FaceType.End;
        }

        private static FaceType ClassifyFloor(Element elem, PlanarFace face, double? openingMinVolumeM3)
        {
            var normal = face.FaceNormal;
            if (normal.Z > 0.9) return FaceType.Top;
            if (normal.Z < -0.9) return FaceType.Bottom;

            // 수평면(Top/Bottom)의 내부 EdgeLoop 엣지와 공유 여부로 판별
            try
            {
                foreach (var solid in RevitGeometryHelper.GetSolids(elem))
                {
                    var (large, small) = GetInnerLoopEdges(solid,
                        f => f is PlanarFace pf && Math.Abs(pf.FaceNormal.Z) > 0.9,
                        GetFloorThicknessFt(elem), openingMinVolumeM3);

                    if (SharesEdgeWith(face, large)) return FaceType.OpeningSide;
                    if (SharesEdgeWith(face, small)) return FaceType.None;
                }
            }
            catch { }

            return FaceType.Side;
        }

        private static double GetFloorThicknessFt(Element elem)
        {
            if (elem is not Floor floor) return 0;
            var cs = floor.FloorType?.GetCompoundStructure();
            return cs?.GetLayers().Sum(l => l.Width) ?? 0;
        }

        // Foundation은 오프닝 분리 미적용
        private static FaceType ClassifyFoundation(XYZ normal)
        {
            if (normal.Z > 0.9) return FaceType.Top;
            if (normal.Z < -0.9) return FaceType.Bottom;
            return FaceType.Side;
        }

        private static FaceType ClassifyColumn(XYZ normal)
        {
            if (normal.Z > 0.9) return FaceType.Top;
            if (normal.Z < -0.9) return FaceType.Bottom;
            return FaceType.Side;
        }

        private static FaceType ClassifyStairs(Element elem, XYZ normal)
        {
            if (normal.Z < -0.1) return FaceType.Bottom;
            if (normal.Z > 0.9) return FaceType.Top;

            var runDir = GetStairRunDir2D(elem);
            if (runDir != null && Math.Abs(normal.DotProduct(runDir)) > 0.9)
                return FaceType.End;

            return FaceType.Side;
        }

        // 기준면의 내부 EdgeLoop(1+, 오프닝)을 체적 기준으로 large(공제 대상)/small(임계값 미만, 제외) 로 분리
        private static (HashSet<Edge> large, HashSet<Edge> small) GetInnerLoopEdges(
            Solid solid, Func<Face, bool> isFaceMatch, double thicknessFt, double? openingMinVolumeM3)
        {
            var large = new HashSet<Edge>();
            var small = new HashSet<Edge>();

            foreach (Face f in solid.Faces)
            {
                if (!isFaceMatch(f) || f.EdgeLoops.Size <= 1 || f is not PlanarFace planar) continue;

                var curveLoops = planar.GetEdgesAsCurveLoops();
                for (int i = 1; i < f.EdgeLoops.Size; i++)
                {
                    bool isSmall = openingMinVolumeM3.HasValue && i < curveLoops.Count
                        && ComputeLoopVolumeM3(curveLoops[i], planar.FaceNormal, thicknessFt) < openingMinVolumeM3.Value;

                    var target = isSmall ? small : large;
                    foreach (Edge e in f.EdgeLoops.get_Item(i))
                        target.Add(e);
                }
            }
            return (large, small);
        }

        // 오프닝 루프를 두께만큼 돌출시켜 근사 체적(m³) 계산 — 실패 시 큰 오프닝으로 간주(기존 동작 유지)
        private static double ComputeLoopVolumeM3(CurveLoop loop, XYZ normal, double thicknessFt)
        {
            if (thicknessFt <= 1e-6) return double.MaxValue;
            try
            {
                var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new[] { loop }, normal, thicknessFt);
                return UC.Ft3ToM3(solid.Volume);
            }
            catch
            {
                return double.MaxValue;
            }
        }

        // 면의 EdgeLoop 중 innerEdges와 공유되는 엣지가 있는지 확인
        private static bool SharesEdgeWith(PlanarFace face, HashSet<Edge> innerEdges)
        {
            foreach (EdgeArray loop in face.EdgeLoops)
                foreach (Edge e in loop)
                    if (innerEdges.Contains(e))
                        return true;
            return false;
        }

        private static XYZ? GetStairRunDir2D(Element elem)
        {
            if (elem is not Stairs stairs) return null;

            var runIds = stairs.GetStairsRuns();
            if (runIds.Count == 0) return null;

            var dirs = new List<XYZ>();
            foreach (var id in runIds)
            {
                if (elem.Document.GetElement(id) is not StairsRun run) continue;
                var path = run.GetStairsPath();
                var firstCurve = path?.FirstOrDefault();
                if (firstCurve == null) continue;
                var raw = firstCurve.GetEndPoint(1) - firstCurve.GetEndPoint(0);
                var dir2D = new XYZ(raw.X, raw.Y, 0);
                if (dir2D.GetLength() > 1e-6)
                    dirs.Add(dir2D.Normalize());
            }

            return dirs.Count switch
            {
                0 => null,
                1 => dirs[0],
                _ => dirs.Aggregate((a, b) => a + b).Normalize(),
            };
        }
    }
}
