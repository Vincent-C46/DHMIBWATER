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
                    .FirstOrDefault(t => t.Name.Contains(stairsDefinition.TypeName));

                if (stairsType == null)
                {
                    _dialog.Warn("Error", $"계단 유형을 찾을 수 없습니다: {stairsDefinition.TypeName}");
                    return 0;
                }
            }
            StairsType? configuredStairsType = stairsType;
            if (stairsType != null && (stairsDefinition.MaxRiserHeight > 0 || stairsDefinition.TreadDepth > 0))
            {
                configuredStairsType = GetOrCreateConfiguredStairsType(doc, stairsType, stairsDefinition);
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

                            StairsRun createdRun = StairsRun.CreateStraightRun(doc, stairsId, locationLine, ToRevitJustification(run.Justification));
                            //createdRun.EndsWithRiser = false;   //챌판 끝남 체크 해제?

                            if (run.Width > 0)
                                createdRun.ActualRunWidth = UC.MmToFt(run.Width);
                        }

                        var editingStairs = doc.GetElement(stairsId) as Stairs;
                        if (editingStairs != null)
                        {
                            try
                            {
                                // 타입 변경
                                if (configuredStairsType != null)
                                    editingStairs.ChangeTypeId(configuredStairsType.Id);

                                // ✅ 중요: 디딤판 깊이와 챌판 수를 Run 생성 전에 설정
                                if (stairsDefinition.TreadDepth > 0)
                                    editingStairs.ActualTreadDepth = UC.MmToFt(stairsDefinition.TreadDepth);


                                double actualStairHeight = UC.MmToFt(stairsDefinition.MaxRiserHeight * stairsDefinition.RisersNumber);
                                double levelHeight = topLevel.Elevation - baseLevel.Elevation;

                                editingStairs.get_Parameter(BuiltInParameter.STAIRS_TOP_OFFSET)?.Set(actualStairHeight - levelHeight);
                                editingStairs.get_Parameter(BuiltInParameter.STAIRS_DESIRED_NUMBER_OF_RISERS)?.Set(stairsDefinition.RisersNumber);

                            }
                            catch (Exception ex)
                            {
                                _dialog.Warn("Warning", $"단수/디딤판 깊이 설정 실패: {ex.Message}");
                            }
                        }
                        doc.Regenerate();

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

                // 계단 생성 시 Revit이 자동으로 붙이는 난간(Railing) 제거 - 난간 없이 작성
                var autoRailingIds = stairs.GetAssociatedRailings();

                if (autoRailingIds.Count > 0)
                    doc.Delete(autoRailingIds);


                // TODO: BaseOffset/TopOffset(mm) 반영 로직 확인 필요 - 현재 미반영
                if (configuredStairsType != null)
                {
                    try
                    {
                        stairs.ChangeTypeId(configuredStairsType.Id);
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

        private static StairsType GetOrCreateConfiguredStairsType(Document doc, StairsType sourceType, StairsDefinition stairsDefinition)
        {
            double runWidth = stairsDefinition.Runs.FirstOrDefault()?.Width ?? 0;   // 폭은 Run별 값 → 첫 Run 폭을 타입 최소 진행 폭으로 사용
            string typeName = $"{sourceType.Name}_DHBIMWATER_R{stairsDefinition.MaxRiserHeight:0}_T{stairsDefinition.TreadDepth:0}_W{runWidth:0}";
            StairsType? configuredType = new FilteredElementCollector(doc)
                .OfClass(typeof(StairsType))
                .Cast<StairsType>()
                .FirstOrDefault(t => t.Name == typeName);

            using (Transaction typeTx = new Transaction(doc, "Prepare Stair Type"))
            {
                typeTx.Start();

                configuredType ??= sourceType.Duplicate(typeName) as StairsType;
                if (configuredType == null)
                    throw new InvalidOperationException($"계단 유형 복사 실패: {typeName}");

                if (stairsDefinition.MaxRiserHeight > 0)
                {
                    configuredType.get_Parameter(BuiltInParameter.STAIRS_ATTR_MAX_RISER_HEIGHT)
                        ?.Set(UC.MmToFt(stairsDefinition.MaxRiserHeight));
                }

                if (stairsDefinition.TreadDepth > 0)
                {
                    configuredType.get_Parameter(BuiltInParameter.STAIRS_ATTR_MINIMUM_TREAD_DEPTH)
                        ?.Set(UC.MmToFt(stairsDefinition.TreadDepth));
                }

                if (runWidth > 0)
                {
                    configuredType.get_Parameter(BuiltInParameter.STAIRSTYPE_MINIMUM_RUN_WIDTH)
                        ?.Set(UC.MmToFt(runWidth));
                }

                typeTx.Commit();
            }

            return configuredType;
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
