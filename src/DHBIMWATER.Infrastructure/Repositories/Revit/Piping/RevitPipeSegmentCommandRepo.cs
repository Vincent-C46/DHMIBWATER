using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Piping;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Piping;

/// <summary>
/// 관을 MEP Pipe가 아니라 배관 밸브류(<c>OST_PipeAccessory</c>) 카테고리의 직관·단관 패밀리로 만든다(docs/39).
/// 절점에는 곡관(90°/45°)·T형을 자동 배치하고, 그 부속의 커넥터 위치를 실측해 양쪽 관을 그만큼 뗀다.
/// <para>MEP 경로(<see cref="RevitPipeMepCommandRepo"/>)는 그대로 남겨 두고 <see cref="OutputMode"/>로 갈린다.</para>
/// </summary>
internal sealed class RevitPipeSegmentCommandRepo : IPipeCommandRepo
{
    private const double ConnectionToleranceMm = 1;
    private readonly Func<Document?> _document;
    public RevitPipeSegmentCommandRepo(Func<Document?> document) => _document = document;
    public PipeOutputMode OutputMode => PipeOutputMode.PipeAccessorySegment;

    public PipeCreationResult CreateNetwork(PipeNetworkDefinition network)
    {
        var document = _document() ?? throw new InvalidOperationException("활성 Revit 문서가 없습니다.");
        var families = network.SegmentFamilies
            ?? throw new InvalidOperationException("직관·단관 패밀리 지정이 없습니다. 모델링 창에서 패밀리를 선택하세요.");

        var segment = ResolveSymbol(document, families.SegmentFamilyTypeName, "관");
        if (string.IsNullOrWhiteSpace(families.LengthParameterName))
            throw new InvalidOperationException("관 길이 파라미터가 지정되지 않았습니다.");
        if (!double.IsFinite(families.StraightLengthMm) || families.StraightLengthMm <= 0)
            throw new InvalidOperationException("직관 정척 길이는 0보다 큰 숫자여야 합니다.");

        // 직관·단관은 같은 패밀리를 길이 파라미터로 구동한다. 커넥터 간 거리는 배치 시점에 결정되므로
        // 여기서는 길이로 검증하지 않고, "2포트인가 / 원점이 커넥터 중앙인가 / 길이를 쓸 수 있는가"만 확인한다.
        ValidateSegmentSymbol(document, segment, families.LengthParameterName);

        var placedInstances = new List<FamilyInstance>();

        // 1) 절점 부속을 먼저 배치하고 커넥터를 실측해 엣지별 차감량을 만든다.
        var trims = PlaceNodeFittings(document, network, families, placedInstances);

        // 2) 엣지마다 구간을 분해해 직관·단관을 놓는다.
        var warnings = new List<string>();
        // null은 물리 배관 커넥터가 1개인 비인라인 부속이다. 주관을 자르지 않고 위치만 배치한다.
        var spanCache = new Dictionary<ElementId, double?>();
        var straightCount = 0;
        var shortCount = 0;
        foreach (var edge in network.Edges)
        {
            var startXyz = ToXyz(edge.Start, edge.Elevation, network.ReferencePoint);
            var endXyz = ToXyz(edge.End, edge.Elevation, network.ReferencePoint);
            var lengthMm = edge.Start.DistanceTo(edge.End);
            if (lengthMm <= 0) continue;

            var occupied = new List<OccupiedSpan>();
            if (trims.TryGetValue((NodeIdAt(network, edge.Start), edge.Id), out var head) && head > 0) occupied.Add(new OccupiedSpan(0, head));
            if (trims.TryGetValue((NodeIdAt(network, edge.End), edge.Id), out var tail) && tail > 0) occupied.Add(new OccupiedSpan(lengthMm - tail, lengthMm));
            occupied.AddRange(InlineFittingSpans(document, edge, network, spanCache));

            var plan = PipeSegmentPlan.Build(lengthMm, occupied, families.StraightLengthMm);
            if (plan.Errors.Count > 0)
                throw new InvalidOperationException($"[{FormatEdge(edge)}] {string.Join(" ", plan.Errors)}");
            warnings.AddRange(plan.Warnings.Select(x => $"[{FormatEdge(edge)}] {x}"));

            var direction = (endXyz - startXyz).Normalize();
            var rotation = Math.Atan2(direction.Y, direction.X);
            foreach (var placement in plan.Segments)
            {
                var center = startXyz + direction * UC.MmToFt(placement.CenterMm);
                var instance = PlaceAt(document, segment, center, rotation);
                placedInstances.Add(instance);

                // 직관·단관이 같은 패밀리이므로 길이 구동과 검증 경로도 하나다.
                // 직관/단관 구분은 패밀리 내부 수식이 정척 길이로 판정하며, 여기서는 집계에만 쓴다.
                var role = placement.Kind == PipeSegmentKind.Straight ? "직관" : "단관";
                SetLengthMm(instance, families.LengthParameterName, placement.LengthMm);
                document.Regenerate();
                ValidateCenteredSpan(instance, center, placement.LengthMm, role);
                if (placement.Kind == PipeSegmentKind.Straight) straightCount++; else shortCount++;
            }
        }

        // 3) 인라인 밸브류는 기존과 같은 위치에 그대로 배치한다(파이프 분할이 없으므로 끼워 넣기만 하면 된다).
        var singlePortCount = 0;
        foreach (var fitting in network.InlineFittings.Where(x => !string.IsNullOrWhiteSpace(x.FamilyTypeName)))
        {
            var edge = network.Edges.FirstOrDefault(x => x.Id == fitting.EdgeId);
            if (edge is null) continue;
            var symbol = ResolveSymbol(document, fitting.FamilyTypeName, "배관부속");
            var start = ToXyz(edge.Start, edge.Elevation, network.ReferencePoint);
            var end = ToXyz(edge.End, edge.Elevation, network.ReferencePoint);
            var direction = (end - start).Normalize();
            var point = ToXyz(fitting.Position, fitting.Elevation, network.ReferencePoint);
            var instance = PlaceAt(document, symbol, point, Math.Atan2(direction.Y, direction.X));
            var connectors = GetMepConnectors(instance);
            if (connectors.Count == 1)
            {
                // 공기밸브처럼 주관을 관통하는 2포트가 아닌 부속은 위치만 배치한다.
                // Tee/분기관 없이 주관 커넥터 2개와 한 점에서 연결하면 3중 접점이 되어 유효한 Revit 연결이 아니다.
                singlePortCount++;
            }
            else if (connectors.Count == 2)
            {
                ValidateCenteredSpan(instance, point, ConnectorSpanMm(connectors), "배관부속");
                placedInstances.Add(instance);
            }
            else
            {
                throw new InvalidOperationException(
                    $"배관부속 '{fitting.FamilyTypeName}'의 물리 배관 커넥터는 {connectors.Count}개입니다. " +
                    "1포트 위치 배치 또는 2포트 인라인 배치만 지원합니다.");
            }
        }

        // 4) 접점이 일치하는 물리 배관 커넥터를 실제로 연결한다. 위치만 맞닿은 패밀리는 Revit 관망으로 연결되지 않는다.
        ConnectCoincidentConnectors(document, placedInstances, network);

        if (singlePortCount > 0)
            warnings.Add($"1포트 배관부속 {singlePortCount}개는 주관을 분할하거나 연결하지 않고 지정 위치에 배치했습니다.");
        return new PipeCreationResult($"직관 {straightCount}본 · 단관 {shortCount}본을 배치했습니다.", warnings);
    }

    private static Guid NodeIdAt(PipeNetworkDefinition network, DHBIMWATER.Core.Geometry.Point2D point)
        => network.Nodes.FirstOrDefault(x => x.Position.DistanceTo(point) <= 0.01)?.Id ?? Guid.Empty;

    /// <summary>절점마다 곡관·T형을 배치하고, 각 레그 방향의 커넥터까지 거리를 (절점, 엣지)별 차감량으로 돌려준다.</summary>
    private static Dictionary<(Guid NodeId, Guid EdgeId), double> PlaceNodeFittings(
        Document document, PipeNetworkDefinition network, PipeSegmentFamilySelection families,
        ICollection<FamilyInstance> placedInstances)
    {
        var trims = new Dictionary<(Guid, Guid), double>();
        foreach (var node in network.Nodes)
        {
            var legs = PipeNodeAngle.Legs(node, network.Edges);
            var origin = ToXyz(node.Position, network.Elevation, network.ReferencePoint);

            FamilySymbol symbol;
            double targetRotation;
            int expectedConnectors;
            switch (node.NodeKind)
            {
                case NodeKind.Elbow when legs.Count == 2:
                    var deflection = PipeNodeAngle.Deflection(node, network.Edges);
                    var name = PipeNodeAngle.IsRightAngle(deflection) ? families.Bend90FamilyTypeName
                        : PipeNodeAngle.IsHalfRightAngle(deflection) ? families.Bend45FamilyTypeName
                        : throw new InvalidOperationException(
                            FormattableString.Invariant($"절점 꺾임각 {deflection:N1}°에 맞는 곡관 유형이 없습니다. 90° 또는 45°로 그리세요."));
                    symbol = ResolveSymbol(document, name, "곡관");
                    // 곡관은 두 레그의 이등분 방향을 기준으로 놓는다. 실제 각 레그 정렬은 커넥터 매칭으로 확인한다.
                    targetRotation = Bisector(legs[0], legs[1]);
                    expectedConnectors = 2;
                    break;

                case NodeKind.Tee when legs.Count == 3:
                    var order = PipeTeeResolver.Resolve(node, network.Edges)
                        ?? throw new InvalidOperationException("직교 T로 판별할 수 없습니다. 주관은 일직선, 분기관은 90°로 그려야 합니다.");
                    symbol = ResolveSymbol(document, families.TeeFamilyTypeName, "T형");
                    var branch = legs.First(x => x.Edge.Id == order.BranchEdgeId);
                    targetRotation = Math.Atan2(branch.DirectionY, branch.DirectionX);
                    expectedConnectors = 3;
                    break;

                default:
                    continue;   // Cap·Inline은 부속이 없고, Cross는 생성 전 검증에서 걸러진다.
            }

            var instance = PlaceAt(document, symbol, origin, 0);
            placedInstances.Add(instance);
            var connectors = GetMepConnectors(instance);
            if (connectors.Count != expectedConnectors)
                throw new InvalidOperationException(
                    $"절점 부속 '{symbol.Family.Name} : {symbol.Name}'의 커넥터가 {connectors.Count}개입니다. {expectedConnectors}개짜리 배관 밸브류 패밀리를 선택하세요.");

            RotateTo(document, instance, origin, connectors, targetRotation, node.NodeKind);

            // 회전 후 실측: 레그 방향과 가장 잘 맞는 커넥터까지의 거리가 그 레그의 차감량이다.
            foreach (var leg in legs)
            {
                var legDirection = new XYZ(leg.DirectionX, leg.DirectionY, 0);
                var best = GetMepConnectors(instance)
                    .OrderByDescending(c => Direction(c.Origin - origin).DotProduct(legDirection))
                    .First();
                trims[(node.Id, leg.Edge.Id)] = UC.FtToMm(best.Origin.DistanceTo(origin));
            }
        }
        return trims;
    }

    /// <summary>부속의 기준 방향(곡관은 커넥터 이등분, T형은 나머지 둘과 직교하는 분기 커넥터)이
    /// <paramref name="targetRotation"/>을 향하도록 Z축으로 돌린다.</summary>
    private static void RotateTo(Document document, FamilyInstance instance, XYZ origin,
        IReadOnlyList<Connector> connectors, double targetRotation, NodeKind kind)
    {
        var directions = connectors.Select(c => Direction(c.Origin - origin)).ToList();
        var current = kind == NodeKind.Tee ? BranchDirection(directions) : Bisector(directions[0], directions[1]);
        var delta = targetRotation - current;
        if (Math.Abs(delta) <= 1e-9) return;
        ElementTransformUtils.RotateElement(document, instance.Id, Line.CreateBound(origin, origin + XYZ.BasisZ), delta);
        document.Regenerate();
    }

    /// <summary>3개 커넥터 중 나머지 둘과 거의 직교하는 것이 분기 커넥터다.</summary>
    private static double BranchDirection(IReadOnlyList<XYZ> directions)
    {
        var branch = directions
            .OrderBy(d => directions.Where(o => !ReferenceEquals(o, d)).Sum(o => Math.Abs(d.DotProduct(o))))
            .First();
        return Math.Atan2(branch.Y, branch.X);
    }

    private static double Bisector(PipeNodeLeg a, PipeNodeLeg b)
        => Bisector(new XYZ(a.DirectionX, a.DirectionY, 0), new XYZ(b.DirectionX, b.DirectionY, 0));

    private static double Bisector(XYZ a, XYZ b)
    {
        var sum = a + b;
        // 두 레그가 정반대(일직선)면 합이 0이라 방향을 못 정한다 — 그때는 한쪽 방향을 쓴다.
        return sum.GetLength() <= 1e-9 ? Math.Atan2(a.Y, a.X) : Math.Atan2(sum.Y, sum.X);
    }

    private static XYZ Direction(XYZ vector)
    {
        var flat = new XYZ(vector.X, vector.Y, 0);
        return flat.GetLength() <= 1e-9 ? XYZ.BasisX : flat.Normalize();
    }

    /// <summary>인라인 밸브가 축 위에서 차지하는 구간. 커넥터 2개 사이 길이를 실측해 쓴다.</summary>
    private static IEnumerable<OccupiedSpan> InlineFittingSpans(
        Document document, PipeEdgeDefinition edge, PipeNetworkDefinition network,
        Dictionary<ElementId, double?> spanCache)
    {
        foreach (var fitting in network.InlineFittings.Where(x => x.EdgeId == edge.Id && !string.IsNullOrWhiteSpace(x.FamilyTypeName)))
        {
            var symbol = ResolveSymbol(document, fitting.FamilyTypeName, "배관부속");
            if (!spanCache.TryGetValue(symbol.Id, out var widthMm))
                spanCache[symbol.Id] = widthMm = InlineConnectorSpanMm(document, symbol);
            if (widthMm is null) continue; // 1포트는 주관을 자르지 않는 위치 배치다.
            var center = fitting.Position.DistanceTo(edge.Start);
            yield return new OccupiedSpan(center - widthMm.Value / 2, center + widthMm.Value / 2);
        }
    }

    /// <summary>인라인 부속이면 커넥터 간 길이, 1포트 위치 부속이면 null. 그 외 커넥터 수는 지원하지 않는다.</summary>
    private static double? InlineConnectorSpanMm(Document document, FamilySymbol symbol)
    {
        var probe = PlaceAt(document, symbol, XYZ.Zero, 0);
        try
        {
            var connectors = GetMepConnectors(probe);
            if (connectors.Count == 1) return null;
            if (connectors.Count != 2)
                throw new InvalidOperationException(
                    $"배관부속 '{symbol.Family.Name} : {symbol.Name}'의 물리 배관 커넥터는 {connectors.Count}개입니다. " +
                    "1포트 위치 배치 또는 2포트 인라인 배치만 지원합니다.");
            ValidateConnectorMidpoint(symbol, connectors, XYZ.Zero, "배관부속");
            return ConnectorSpanMm(connectors);
        }
        finally
        {
            document.Delete(probe.Id);
            document.Regenerate();
        }
    }

    /// <summary>관 패밀리가 인라인 배치 조건을 만족하는지 임시 인스턴스로 확인한다.
    /// 길이는 배치 시점에 파라미터로 구동하므로 여기서 길이를 검증하지 않는다.</summary>
    private static void ValidateSegmentSymbol(Document document, FamilySymbol symbol, string lengthParameterName)
    {
        var probe = PlaceAt(document, symbol, XYZ.Zero, 0);
        try
        {
            var connectors = GetMepConnectors(probe);
            if (connectors.Count != 2)
                throw new InvalidOperationException(
                    $"관 '{symbol.Family.Name} : {symbol.Name}'의 물리 배관 커넥터는 {connectors.Count}개입니다. " +
                    "인라인 배치에는 양쪽 끝 커넥터가 있는 2포트 패밀리가 필요합니다.");
            ValidateConnectorMidpoint(symbol, connectors, XYZ.Zero, "관");

            // 길이 파라미터를 실제로 쓸 수 있는지 배치 전에 확인한다(전체 롤백보다 이른 실패가 낫다).
            var parameter = probe.LookupParameter(lengthParameterName);
            if (parameter is null || parameter.IsReadOnly || parameter.StorageType != StorageType.Double)
                throw new InvalidOperationException(
                    $"관 길이 파라미터 '{lengthParameterName}'에 값을 쓸 수 없습니다. " +
                    "쓰기 가능한 길이형 인스턴스 파라미터인지 확인하세요.");
        }
        finally
        {
            document.Delete(probe.Id);
            document.Regenerate();
        }
    }

    private static FamilyInstance PlaceAt(Document document, FamilySymbol symbol, XYZ point, double rotation)
    {
        if (!symbol.IsActive) { symbol.Activate(); document.Regenerate(); }
        var instance = document.Create.NewFamilyInstance(point, symbol, StructuralType.NonStructural);
        document.Regenerate();
        if (Math.Abs(rotation) > 1e-9)
        {
            ElementTransformUtils.RotateElement(document, instance.Id, Line.CreateBound(point, point + XYZ.BasisZ), rotation);
            document.Regenerate();
        }
        return instance;
    }

    private static void SetLengthMm(FamilyInstance instance, string parameterName, double lengthMm)
    {
        var parameter = instance.LookupParameter(parameterName);
        if (parameter is null || parameter.IsReadOnly || parameter.StorageType != StorageType.Double)
            throw new InvalidOperationException(
                $"관 길이 파라미터 '{parameterName}'에 값을 쓸 수 없습니다. 쓰기 가능한 길이형 인스턴스 파라미터인지 확인하세요.");
        if (!parameter.Set(UC.MmToFt(lengthMm)))
            throw new InvalidOperationException($"관 길이 파라미터 '{parameterName}' 설정에 실패했습니다.");
    }

    private static void ValidateCenteredSpan(FamilyInstance instance, XYZ expectedCenter, double expectedLengthMm, string role)
    {
        var connectors = GetMepConnectors(instance);
        if (connectors.Count != 2)
            throw new InvalidOperationException($"{role} '{instance.Symbol.Family.Name} : {instance.Symbol.Name}'의 물리 배관 커넥터는 {connectors.Count}개입니다.");
        var midpoint = (connectors[0].Origin + connectors[1].Origin) / 2;
        var centerErrorMm = UC.FtToMm(midpoint.DistanceTo(expectedCenter));
        var spanMm = ConnectorSpanMm(connectors);
        if (centerErrorMm > ConnectionToleranceMm || Math.Abs(spanMm - expectedLengthMm) > ConnectionToleranceMm)
            throw new InvalidOperationException(
                $"{role} '{instance.Symbol.Family.Name} : {instance.Symbol.Name}'의 커넥터 형상이 요청 구간과 맞지 않습니다. " +
                $"중앙 오차 {centerErrorMm:N1}mm, 길이 {spanMm:N1}mm(요청 {expectedLengthMm:N1}mm)입니다.");
    }

    private static void ValidateConnectorMidpoint(
        FamilySymbol symbol, IReadOnlyList<Connector> connectors, XYZ expectedCenter, string role)
    {
        var midpoint = (connectors[0].Origin + connectors[1].Origin) / 2;
        if (UC.FtToMm(midpoint.DistanceTo(expectedCenter)) <= ConnectionToleranceMm) return;
        throw new InvalidOperationException(
            $"{role} '{symbol.Family.Name} : {symbol.Name}'의 원점이 두 배관 커넥터의 중앙과 일치하지 않습니다. " +
            "관 중앙이 원점인 패밀리를 선택하세요.");
    }

    private static double ConnectorSpanMm(IReadOnlyList<Connector> connectors)
        => UC.FtToMm(connectors[0].Origin.DistanceTo(connectors[1].Origin));

    /// <summary>서로 다른 패밀리의 물리 배관 커넥터가 같은 접점에 하나씩 있으면 실제 Revit 연결을 만든다.
    /// Cap 이외의 열린 커넥터나 한 점에 3개 이상 모인 모호한 연결은 관망 단절이므로 전체 생성을 중단한다.</summary>
    private static void ConnectCoincidentConnectors(
        Document document, IReadOnlyCollection<FamilyInstance> instances, PipeNetworkDefinition network)
    {
        var tolerance = UC.MmToFt(ConnectionToleranceMm);
        var capPoints = network.Nodes.Where(x => x.NodeKind == NodeKind.Cap)
            .Select(x => ToXyz(x.Position, network.Elevation, network.ReferencePoint)).ToList();
        var connectors = instances.SelectMany(GetMepConnectors).ToList();
        var handled = new HashSet<Connector>();

        foreach (var connector in connectors)
        {
            if (handled.Contains(connector) || connector.IsConnected) continue;
            var candidates = connectors.Where(other =>
                    !ReferenceEquals(other, connector) &&
                    !handled.Contains(other) &&
                    !other.IsConnected &&
                    other.Owner.Id != connector.Owner.Id &&
                    other.Origin.DistanceTo(connector.Origin) <= tolerance)
                .ToList();

            if (candidates.Count == 1)
            {
                try
                {
                    connector.ConnectTo(candidates[0]);
                    handled.Add(connector);
                    handled.Add(candidates[0]);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"배관 부속류 커넥터 연결에 실패했습니다. 접점 {FormatPoint(connector.Origin)}의 두 패밀리 방향과 커넥터 규격을 확인하세요.", ex);
                }
                continue;
            }

            if (candidates.Count > 1)
                throw new InvalidOperationException(
                    $"접점 {FormatPoint(connector.Origin)}에 연결 가능한 배관 커넥터가 {candidates.Count + 1}개 겹쳐 있습니다. " +
                    "1포트 분기 부속은 인라인 부속으로 직접 연결할 수 없습니다.");

            if (capPoints.Any(x => x.DistanceTo(connector.Origin) <= tolerance))
            {
                handled.Add(connector);
                continue;
            }

            throw new InvalidOperationException(
                $"접점 {FormatPoint(connector.Origin)}에서 연결 상대를 찾지 못했습니다. " +
                "직관·단관 길이와 곡관/T형 커넥터 방향을 확인하세요.");
        }

        document.Regenerate();
    }

    private static FamilySymbol ResolveSymbol(Document document, string familyTypeName, string role)
    {
        if (string.IsNullOrWhiteSpace(familyTypeName))
            throw new InvalidOperationException($"{role} 패밀리가 선택되지 않았습니다.");
        var separator = familyTypeName.LastIndexOf(" : ", StringComparison.Ordinal);
        if (separator <= 0 || separator >= familyTypeName.Length - 3)
            throw new InvalidOperationException($"{role} 패밀리명 '{familyTypeName}' 형식이 올바르지 않습니다(\"패밀리명 : 타입명\").");
        var familyName = familyTypeName[..separator];
        var typeName = familyTypeName[(separator + 3)..];
        return new FilteredElementCollector(document).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                   .FirstOrDefault(x => x.Family.Name == familyName && x.Name == typeName)
               ?? throw new InvalidOperationException($"{role} 패밀리 '{familyTypeName}'를 찾을 수 없습니다. 프로젝트에 로드돼 있는지 확인하세요.");
    }

    /// <summary>논리·전기 커넥터를 제외하고 실제 관망 연결에 쓰는 끝단 배관 커넥터만 반환한다.</summary>
    private static List<Connector> GetMepConnectors(FamilyInstance instance)
        => instance.MEPModel?.ConnectorManager?.Connectors.Cast<Connector>()
            .Where(x => x.Domain == Domain.DomainPiping && x.ConnectorType == ConnectorType.End)
            .ToList() ?? [];

    private static string FormatPoint(XYZ point)
        => FormattableString.Invariant($"({UC.FtToMm(point.X):N0}, {UC.FtToMm(point.Y):N0}, {UC.FtToMm(point.Z):N0})mm");

    private static string FormatEdge(PipeEdgeDefinition edge)
        => FormattableString.Invariant($"({edge.Start.X:N0}, {edge.Start.Y:N0}) → ({edge.End.X:N0}, {edge.End.Y:N0})");

    private static XYZ ToXyz(DHBIMWATER.Core.Geometry.Point2D point, double elevation, DHBIMWATER.Core.Geometry.Point2D referencePoint)
        => new(UC.MmToFt(point.X + referencePoint.X), UC.MmToFt(point.Y + referencePoint.Y), UC.MmToFt(elevation));
}
