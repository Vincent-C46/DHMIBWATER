using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling
{
    internal class RevitStairCommandRepo : IStairCommandRepo
    {
        #region Fields
        private readonly Func<Document?> _doc;  // Revit Document에 접근하기 위한 람다식
        private readonly IDialogService _dialog;
        #endregion

        #region Constructor
        public RevitStairCommandRepo(Func<Document?> doc, IDialogService dialog)
        {
            _doc = doc;
            _dialog = dialog;
        }
        #endregion

        #region Methods
        public int CreateStair(StairsDefinition stairsDefinition)
        {
            Document? doc = _doc();

            if (doc == null)
            {
                _dialog.Warn("Error", "Active document is not available.");
                return 0;
            }

            if (stairsDefinition.Runs.Count == 0)
            {
                _dialog.Warn("Error", "계단 Run이 1개 이상 필요합니다.");
                return 0;
            }

            Level? baseLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == stairsDefinition.BaseLevelName);
            Level? topLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == stairsDefinition.TopLevelName);

            if (baseLevel == null || topLevel == null)
            {
                _dialog.Warn("Error", "계단 하부/상부 레벨을 찾을 수 없습니다.");
                return 0;
            }

            StairsType? stairsType = null;
            if (!string.IsNullOrEmpty(stairsDefinition.TypeName))
            {
                // 아무 첫번째 계단 X
                stairsType = new FilteredElementCollector(doc)
                    .OfClass(typeof(StairsType))
                    .Cast<StairsType>()
                    .FirstOrDefault(t => t.Name == stairsDefinition.TypeName);

                if (stairsType == null)
                {
                    _dialog.Warn("Error", $"계단 유형을 찾을 수 없습니다: {stairsDefinition.TypeName}");
                    return 0;
                }
            }

            Stairs? stairs;

            // StairsEditScope는 Revit API 제약상 열려있는 Transaction 내부에서 Start/Commit할 수 없으나,
            // 호출부(UseCase)에서 이미 트랜잭션을 열고 있는 경우를 전제로 함 (요청에 따라 별도 처리 안 함).
            try
            {
                using (StairsEditScope stairsEditScope = new StairsEditScope(doc, "Create Stairs"))
                {
                    ElementId stairsId = stairsEditScope.Start(baseLevel.Id, topLevel.Id);

                    using (Transaction runTx = new Transaction(doc, "Add Stair Runs/Landings"))
                    {
                        runTx.Start();

                        foreach (var run in stairsDefinition.Runs)
                        {
                            Line locationLine = Line.CreateBound(
                                new XYZ(UC.MmToFt(run.StartPoint.X), UC.MmToFt(run.StartPoint.Y), UC.MmToFt(run.StartPoint.Z)),
                                new XYZ(UC.MmToFt(run.EndPoint.X), UC.MmToFt(run.EndPoint.Y), UC.MmToFt(run.EndPoint.Z)));

                            StairsRun.CreateStraightRun(doc, stairsId, locationLine, ToRevitJustification(run.Justification));
                        }

                        foreach (var landing in stairsDefinition.Landings)
                        {
                            if (landing.BoundaryPoints.Count < 3) continue;

                            CurveLoop boundary = BuildBoundaryLoop(landing.BoundaryPoints);
                            double baseElevation = UC.MmToFt(landing.BoundaryPoints[0].Z);

                            StairsLanding.CreateSketchedLanding(doc, stairsId, boundary, baseElevation);
                        }

                        runTx.Commit();
                    }

                    stairsEditScope.Commit(new StairsFailurePreprocessor());
                    stairs = doc.GetElement(stairsId) as Stairs;
                }
            }
            catch (Exception ex)
            {
                _dialog.Warn("Error", $"계단 생성 실패\nElementCode: {stairsDefinition.ElementCode}\nException: {ex.Message}");
                return 0;
            }

            if (stairs == null) return 0;

            // StairsEditScope 종료 후에는 열려있는 Transaction이 없으므로(이 Repo는 단독 호출 전제),
            // 타입 변경/파라미터 설정을 위한 Transaction을 별도로 연다.
            using (Transaction postTx = new Transaction(doc, "Set Stair Type/Parameters"))
            {
                postTx.Start();

                // TODO: BaseOffset/TopOffset(mm) 반영 로직 확인 필요 - 현재 미반영
                if (stairsType != null)
                {
                    try
                    {
                        stairs.ChangeTypeId(stairsType.Id);
                    }
                    catch (Exception ex)
                    {
                        _dialog.Warn("Warning", $"계단 유형 변경 실패: {ex.Message}");
                    }
                }

                stairs.LookupParameter("DH_ElementCode")?.Set(stairsDefinition.ElementCode);
                stairs.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
                stairs.LookupParameter("DH_Category")?.Set(stairsDefinition.Category);
                stairs.LookupParameter("DH_Part")?.Set(stairsDefinition.Part);
                stairs.LookupParameter("DH_Zone")?.Set(stairsDefinition.Zone);

                postTx.Commit();
            }

            return (int)stairs.Id.Value;
        }

        private static CurveLoop BuildBoundaryLoop(List<Point3D> points)
        {
            var loop = new CurveLoop();
            int count = points.Count;

            for (int i = 0; i < count; i++)
            {
                Point3D startPt = points[i];
                Point3D endPt = points[(i + 1) % count];

                XYZ startXYZ = new XYZ(UC.MmToFt(startPt.X), UC.MmToFt(startPt.Y), UC.MmToFt(startPt.Z));
                XYZ endXYZ = new XYZ(UC.MmToFt(endPt.X), UC.MmToFt(endPt.Y), UC.MmToFt(endPt.Z));

                loop.Append(Line.CreateBound(startXYZ, endXYZ));
            }

            return loop;
        }

        private static StairsRunJustification ToRevitJustification(StairJustification justification) => justification switch
        {
            StairJustification.Left => StairsRunJustification.Left,
            StairJustification.Right => StairsRunJustification.Right,
            _ => StairsRunJustification.Center,
        };

        private class StairsFailurePreprocessor : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                failuresAccessor.DeleteAllWarnings();
                return FailureProcessingResult.Continue;
            }
        }
        #endregion
    }
}
