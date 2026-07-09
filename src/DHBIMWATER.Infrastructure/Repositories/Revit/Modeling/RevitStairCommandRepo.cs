using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using DHBIMWATER.Infrastructure.Logging;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling
{
    internal class RevitStairCommandRepo : IStairCommandRepo
    {
        #region Fields
        private readonly Func<Document?> _doc;  // Revit Document에 접근하기 위한 람다식
        private readonly IDialogService _dialog;
        private static int _createSequence;
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
            int sequence = System.Threading.Interlocked.Increment(ref _createSequence);

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

            LogStairDiagnostic(sequence, "00-input", doc, stairsDefinition, null, null, baseLevel, topLevel);

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
            bool configuredTypeCreated = false;
            if (stairsType != null && (stairsDefinition.MaxRiserHeight > 0 || stairsDefinition.TreadDepth > 0))
            {
                (configuredStairsType, configuredTypeCreated) = GetOrCreateConfiguredStairsType(doc, stairsType, stairsDefinition);
            }
            LogStairDiagnostic(sequence, "01-configured-type", doc, stairsDefinition, null, configuredStairsType, baseLevel, topLevel);
            if (configuredTypeCreated && configuredStairsType != null)
            {
                WarmUpNewStairsType(doc, stairsDefinition, configuredStairsType, baseLevel, topLevel, sequence);
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

                        // ── 1) Run 생성 "전" 타입/디딤판 깊이 적용 ──────────────────────────────
                        //    StairsEditScope.Start()가 만드는 빈 계단은 "문서 기본 StairsType"을 사용한다.
                        //    첫 계단은 기본 타입이 원하는 타입(디딤판 300 등)과 달라, Run을 기본 타입으로
                        //    먼저 만들면 챌판/디딤판 개수가 어긋난다(→ 첫 계단만 13단). 2번째부터는 첫 계단이
                        //    만든 타입이 기본으로 자리잡아 정상. 따라서 Run 생성 전에 타입/디딤판을 먼저 맞춘다.
                        var preStairs = doc.GetElement(stairsId) as Stairs;
                        if (preStairs != null)
                        {
                            try
                            {
                                if (configuredStairsType != null)
                                    preStairs.ChangeTypeId(configuredStairsType.Id);

                                if (stairsDefinition.TreadDepth > 0)
                                    preStairs.ActualTreadDepth = UC.MmToFt(stairsDefinition.TreadDepth);
                            }
                            catch (Exception ex)
                            {
                                _dialog.Warn("Warning", $"계단 타입/디딤판 사전 설정 실패: {ex.Message}");
                            }
                        }
                        doc.Regenerate();
                        LogStairDiagnostic(sequence, "02-after-pre-type-tread", doc, stairsDefinition, preStairs, configuredStairsType, baseLevel, topLevel);

                        // ── 2) Run 생성 ─────────────────────────────────────────────────────────
                        foreach (var run in stairsDefinition.Runs)
                        {
                            Line locationLine = Line.CreateBound(
                                new XYZ(UC.MmToFt(run.StartPoint.X), UC.MmToFt(run.StartPoint.Y), UC.MmToFt(run.StartPoint.Z)),
                                new XYZ(UC.MmToFt(run.EndPoint.X), UC.MmToFt(run.EndPoint.Y), UC.MmToFt(run.EndPoint.Z)));

                            StairsRun createdRun = StairsRun.CreateStraightRun(doc, stairsId, locationLine, ToRevitJustification(run.Justification));
                            //createdRun.EndsWithRiser = false;   //    챌판 끝남 체크 해제?

                            if (run.Width > 0)
                                createdRun.ActualRunWidth = UC.MmToFt(run.Width);
                        }
                        LogStairDiagnostic(sequence, "03-after-run-create", doc, stairsDefinition, doc.GetElement(stairsId) as Stairs, configuredStairsType, baseLevel, topLevel);

                        // ── 3) Run 생성 "후" 유효높이(TOP_OFFSET) → 챌판 수 확정 ─────────────────
                        //    TOP_OFFSET(-100 등)은 Run이 존재해야 유효높이(2500→2400)에 반영된다.
                        //    유효높이를 먼저 재생성으로 확정한 뒤 챌판 수를 확정해야, 최대 챌판높이 위반으로
                        //    단수가 12→13으로 튀지 않는다.
                        var editingStairs = doc.GetElement(stairsId) as Stairs;
                        if (editingStairs != null)
                        {
                            try
                            {
                                double actualStairHeight = UC.MmToFt(stairsDefinition.MaxRiserHeight * stairsDefinition.RisersNumber);
                                double levelHeight = topLevel.Elevation - baseLevel.Elevation;

                                editingStairs.get_Parameter(BuiltInParameter.STAIRS_TOP_OFFSET)?.Set(actualStairHeight - levelHeight);
                                doc.Regenerate();
                                LogStairDiagnostic(sequence, "04-after-edit-top-offset", doc, stairsDefinition, editingStairs, configuredStairsType, baseLevel, topLevel);


                                editingStairs.get_Parameter(BuiltInParameter.STAIRS_DESIRED_NUMBER_OF_RISERS)?.Set(stairsDefinition.RisersNumber);

                            }
                            catch (Exception ex)
                            {
                                _dialog.Warn("Warning", $"단수/디딤판 깊이 설정 실패: {ex.Message}");
                            }
                        }
                        doc.Regenerate();
                        LogStairDiagnostic(sequence, "05-after-edit-final-regenerate", doc, stairsDefinition, editingStairs, configuredStairsType, baseLevel, topLevel);

                        runTx.Commit();
                    }
                    stairsEditScope.Commit(new StairsFailurePreprocessor());
                    stairs = doc.GetElement(stairsId) as Stairs;
                    LogStairDiagnostic(sequence, "06-after-edit-scope-commit", doc, stairsDefinition, stairs, configuredStairsType, baseLevel, topLevel);
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
                        LogStairDiagnostic(sequence, "07-after-post-type-change", doc, stairsDefinition, stairs, configuredStairsType, baseLevel, topLevel);
                    }
                    catch (Exception ex)
                    {
                        _dialog.Warn("Warning", $"계단 유형 변경 실패: {ex.Message}");
                    }
                }

                // ✅ 첫 계단만 실제 챌판높이가 어긋나는(유효높이 2500 기준 192.3mm) 문제 방지:
                //    편집 스코프(StairsEditScope) 내부는 첫 계단만 타입 복제·재생성 등 과도 상태를 거쳐
                //    TOP_OFFSET(-100) 반영이 불안정하다. 계단이 완전히 커밋된 이 안정된 컨텍스트에서
                //    유효높이(2400)와 챌판 수를 한 번 더 확정하면 첫/나머지 계단이 동일하게 맞춰진다.
                try
                {
                    double actualStairHeight = UC.MmToFt(stairsDefinition.MaxRiserHeight * stairsDefinition.RisersNumber);
                    double levelHeight = topLevel.Elevation - baseLevel.Elevation;

                    stairs.get_Parameter(BuiltInParameter.STAIRS_TOP_OFFSET)?.Set(actualStairHeight - levelHeight);
                    doc.Regenerate();
                    LogStairDiagnostic(sequence, "08-after-post-top-offset", doc, stairsDefinition, stairs, configuredStairsType, baseLevel, topLevel);

                        
                    stairs.get_Parameter(BuiltInParameter.STAIRS_DESIRED_NUMBER_OF_RISERS)?.Set(stairsDefinition.RisersNumber);
                    doc.Regenerate();
                    LogStairDiagnostic(sequence, "09-after-post-desired-risers", doc, stairsDefinition, stairs, configuredStairsType, baseLevel, topLevel);
                }
                catch (Exception ex)
                {
                    _dialog.Warn("Warning", $"단수/높이 재확정 실패: {ex.Message}");
                }

                stairs.LookupParameter("DH_ElementCode")?.Set(stairsDefinition.ElementCode);
                stairs.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
                stairs.LookupParameter("DH_Category")?.Set(stairsDefinition.Category);
                stairs.LookupParameter("DH_Part")?.Set(stairsDefinition.Part);
                stairs.LookupParameter("DH_Zone")?.Set(stairsDefinition.Zone);
                postTx.Commit();
            }
            LogStairDiagnostic(sequence, "10-after-post-commit", doc, stairsDefinition, stairs, configuredStairsType, baseLevel, topLevel);

            return (int)stairs.Id.Value;
        }

        private static (StairsType StairsType, bool Created) GetOrCreateConfiguredStairsType(Document doc, StairsType sourceType, StairsDefinition stairsDefinition)
        {
            double runWidth = stairsDefinition.Runs.FirstOrDefault()?.Width ?? 0;   // 폭은 Run별 값 → 첫 Run 폭을 타입 최소 진행 폭으로 사용
            string typeName = $"{sourceType.Name}_DHBIMWATER_R{stairsDefinition.MaxRiserHeight:0}_T{stairsDefinition.TreadDepth:0}_W{runWidth:0}";
            StairsType? configuredType = new FilteredElementCollector(doc)
                .OfClass(typeof(StairsType))
                .Cast<StairsType>()
                .FirstOrDefault(t => t.Name == typeName);
            bool created = configuredType == null;

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
                doc.Regenerate();

                typeTx.Commit();
            }

            return (configuredType, created);
        }

        private static void WarmUpNewStairsType(
            Document doc,
            StairsDefinition stairsDefinition,
            StairsType configuredStairsType,
            Level baseLevel,
            Level topLevel,
            int sequence)
        {
            if (stairsDefinition.Runs.Count == 0) return;

            ElementId? warmupStairsId = null;
            try
            {
                using (StairsEditScope stairsEditScope = new StairsEditScope(doc, "Warm Up Stairs Type"))
                {
                    ElementId stairsId = stairsEditScope.Start(baseLevel.Id, topLevel.Id);
                    warmupStairsId = stairsId;

                    using (Transaction tx = new Transaction(doc, "Warm Up Stair Run"))
                    {
                        tx.Start();

                        Stairs? warmupStairs = doc.GetElement(stairsId) as Stairs;
                        if (warmupStairs != null)
                        {
                            warmupStairs.ChangeTypeId(configuredStairsType.Id);
                            if (stairsDefinition.TreadDepth > 0)
                                warmupStairs.ActualTreadDepth = UC.MmToFt(stairsDefinition.TreadDepth);
                        }
                        doc.Regenerate();

                        var run = stairsDefinition.Runs[0];
                        Line locationLine = Line.CreateBound(
                            new XYZ(UC.MmToFt(run.StartPoint.X), UC.MmToFt(run.StartPoint.Y), UC.MmToFt(run.StartPoint.Z)),
                            new XYZ(UC.MmToFt(run.EndPoint.X), UC.MmToFt(run.EndPoint.Y), UC.MmToFt(run.EndPoint.Z)));

                        StairsRun createdRun = StairsRun.CreateStraightRun(doc, stairsId, locationLine, ToRevitJustification(run.Justification));
                        if (run.Width > 0)
                            createdRun.ActualRunWidth = UC.MmToFt(run.Width);

                        if (warmupStairs != null)
                        {
                            double actualStairHeight = UC.MmToFt(stairsDefinition.MaxRiserHeight * stairsDefinition.RisersNumber);  // 챌판 높이 * 단수 → 목표 계단 높이
                            double levelHeight = topLevel.Elevation - baseLevel.Elevation;
                            warmupStairs.get_Parameter(BuiltInParameter.STAIRS_TOP_OFFSET)?.Set(actualStairHeight - levelHeight);
                            doc.Regenerate();
                            warmupStairs.get_Parameter(BuiltInParameter.STAIRS_DESIRED_NUMBER_OF_RISERS)?.Set(stairsDefinition.RisersNumber);
                            doc.Regenerate();
                            LogStairDiagnostic(sequence, "01w-after-warmup", doc, stairsDefinition, warmupStairs, configuredStairsType, baseLevel, topLevel);
                        }

                        tx.Commit();
                    }

                    stairsEditScope.Commit(new StairsFailurePreprocessor());
                }
            }
            catch (Exception ex)
            {
                LogManager.Logger.Warn($"STAIR_DIAG seq={sequence} stage=01w-warmup-failed\nException: {ex.Message}");
            }

            if (warmupStairsId != null)
            {
                try
                {
                    using (Transaction deleteTx = new Transaction(doc, "Delete Warm Up Stair"))
                    {
                        deleteTx.Start();
                        if (doc.GetElement(warmupStairsId) != null)
                            doc.Delete(warmupStairsId);
                        deleteTx.Commit();
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Logger.Warn($"STAIR_DIAG seq={sequence} stage=01w-delete-warmup-failed\nException: {ex.Message}");
                }
            }
        }

        private static void LogStairDiagnostic(
            int sequence,
            string stage,
            Document doc,
            StairsDefinition stairsDefinition,
            Stairs? stairs,
            StairsType? configuredStairsType,
            Level baseLevel,
            Level topLevel)
        {
            try
            {
                double levelHeightMm = UC.FtToMm(topLevel.Elevation - baseLevel.Elevation);
                double targetHeightMm = stairsDefinition.MaxRiserHeight * stairsDefinition.RisersNumber;
                double targetTopOffsetMm = targetHeightMm - levelHeightMm;

                string message =
                    $"STAIR_DIAG seq={sequence} stage={stage}\n" +
                    $"doc={doc.Title}\n" +
                    $"def: elementCode={stairsDefinition.ElementCode}, baseLevel={stairsDefinition.BaseLevelName}, topLevel={stairsDefinition.TopLevelName}, " +
                    $"maxRiserMm={stairsDefinition.MaxRiserHeight:0.###}, risers={stairsDefinition.RisersNumber}, treadMm={stairsDefinition.TreadDepth:0.###}, " +
                    $"levelHeightMm={levelHeightMm:0.###}, targetHeightMm={targetHeightMm:0.###}, targetTopOffsetMm={targetTopOffsetMm:0.###}\n" +
                    $"type: id={FormatElementId(configuredStairsType?.Id)}, name={configuredStairsType?.Name ?? "(null)"}, " +
                    $"maxRiserMm={FormatLengthParameter(configuredStairsType, BuiltInParameter.STAIRS_ATTR_MAX_RISER_HEIGHT)}, " +
                    $"minTreadMm={FormatLengthParameter(configuredStairsType, BuiltInParameter.STAIRS_ATTR_MINIMUM_TREAD_DEPTH)}, " +
                    $"minRunWidthMm={FormatLengthParameter(configuredStairsType, BuiltInParameter.STAIRSTYPE_MINIMUM_RUN_WIDTH)}\n" +
                    $"stairs: id={FormatElementId(stairs?.Id)}, typeId={FormatElementId(stairs?.GetTypeId())}, " +
                    $"topOffsetMm={FormatLengthParameter(stairs, BuiltInParameter.STAIRS_TOP_OFFSET)}, " +

                    $"desiredRisers={FormatIntegerParameter(stairs, BuiltInParameter.STAIRS_DESIRED_NUMBER_OF_RISERS)}, " +
                    $"actualRisers={FormatRevitProperty(stairs, "ActualRisersNumber")}, " +
                    $"actualRiserHeightMm={FormatLengthProperty(stairs, "ActualRiserHeight")}, " +
                    $"actualTreadDepthMm={FormatLengthProperty(stairs, "ActualTreadDepth")}";

                LogManager.Logger.Info(message);
            }
            catch
            {
                // 진단 로그 실패가 모델 생성 흐름을 막으면 안 된다.
            }
        }

        private static string FormatElementId(ElementId? id)
        {
            return id == null ? "(null)" : id.Value.ToString();
        }

        private static string FormatLengthParameter(Element? element, BuiltInParameter builtInParameter)
        {
            Parameter? parameter = element?.get_Parameter(builtInParameter);
            if (parameter == null || !parameter.HasValue) return "(null)";
            return $"{UC.FtToMm(parameter.AsDouble()):0.###}";
        }

        private static string FormatIntegerParameter(Element? element, BuiltInParameter builtInParameter)
        {
            Parameter? parameter = element?.get_Parameter(builtInParameter);
            if (parameter == null || !parameter.HasValue) return "(null)";
            return parameter.AsInteger().ToString();
        }

        private static string FormatRevitProperty(object? target, string propertyName)
        {
            if (target == null) return "(null)";

            try
            {
                object? value = target.GetType().GetProperty(propertyName)?.GetValue(target);
                return value?.ToString() ?? "(null)";
            }
            catch
            {
                return "(unavailable)";
            }
        }

        private static string FormatLengthProperty(object? target, string propertyName)
        {
            if (target == null) return "(null)";

            try
            {
                object? value = target.GetType().GetProperty(propertyName)?.GetValue(target);
                if (value is double feet)
                    return $"{UC.FtToMm(feet):0.###}";

                return value?.ToString() ?? "(null)";
            }
            catch
            {
                return "(unavailable)";
            }
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