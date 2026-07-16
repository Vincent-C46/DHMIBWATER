using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Piping;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Piping;

/// <summary>패밀리 의존성이 없는 GenericModel 대체 출력. 선과 부속 위치를 DirectShape로 기록한다.</summary>
internal sealed class RevitPipeGenericModelCommandRepo : IPipeCommandRepo
{
    private readonly Func<Document?> _document;
    public RevitPipeGenericModelCommandRepo(Func<Document?> document) => _document = document;
    public PipeOutputMode OutputMode => PipeOutputMode.GenericModel;

    public void CreateNetwork(PipeNetworkDefinition network)
    {
        var document = _document() ?? throw new InvalidOperationException("활성 Revit 문서가 없습니다.");
        foreach (var edge in network.Edges)
        {
            var shape = DirectShape.CreateElement(document, new ElementId(BuiltInCategory.OST_GenericModel));
            shape.ApplicationId = "DHBIMWATER";
            shape.ApplicationDataId = edge.Id.ToString();
            shape.SetShape([Line.CreateBound(ToXyz(edge.Start, edge.Elevation), ToXyz(edge.End, edge.Elevation))]);
        }
        foreach (var fitting in network.InlineFittings)
        {
            var point = ToXyz(fitting.Position, fitting.Elevation);
            var shape = DirectShape.CreateElement(document, new ElementId(BuiltInCategory.OST_GenericModel));
            shape.ApplicationId = "DHBIMWATER";
            shape.ApplicationDataId = fitting.Id.ToString();
            shape.SetShape([Line.CreateBound(point, point + XYZ.BasisZ * UC.MmToFt(50))]);
        }
    }

    private static XYZ ToXyz(DHBIMWATER.Core.Geometry.Point2D point, double elevation) => new(UC.MmToFt(point.X), UC.MmToFt(point.Y), UC.MmToFt(elevation));
}
