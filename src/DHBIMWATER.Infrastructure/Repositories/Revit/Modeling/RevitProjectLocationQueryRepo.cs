using System.Linq;
using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Infrastructure.Converters;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling;

internal sealed class RevitProjectLocationQueryRepo : IProjectLocationQueryRepo
{
    private readonly Func<Document?> _doc;

    public RevitProjectLocationQueryRepo(Func<Document?> doc) => _doc = doc;

    public (double EastWestMeters, double NorthSouthMeters) GetProjectBasePointSharedPosition()
    {
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서를 찾을 수 없습니다.");

        var basePoint = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_ProjectBasePoint)
            .WhereElementIsNotElementType()
            .FirstOrDefault() as BasePoint;
        var position = basePoint?.Position ?? XYZ.Zero;

        // SetInternalOriginSharedPosition의 역연산: PBP 내부좌표에서의 현재 공유좌표를 그대로 읽는다.
        var projectPosition = doc.ActiveProjectLocation.GetProjectPosition(position);
        return (RevitUnitConverter.FtToM(projectPosition.EastWest), RevitUnitConverter.FtToM(projectPosition.NorthSouth));
    }
}
