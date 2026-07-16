using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Infrastructure.Converters;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling;

internal sealed class RevitProjectLocationCommandRepo : IProjectLocationCommandRepo
{
    private readonly Func<Document?> _doc;

    public RevitProjectLocationCommandRepo(Func<Document?> doc) => _doc = doc;

    public void SetInternalOriginSharedPosition(
        double eastWestMeters,
        double northSouthMeters,
        double elevationMeters,
        double trueNorthToProjectNorthClockwiseDegrees)
    {
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서를 찾을 수 없습니다.");

        // Revit API 양의 각도는 반시계방향이다. UI 입력값은 진북→도북 시계방향이므로 부호를 반전한다.
        var angle = -RevitUnitConverter.DegToRad(trueNorthToProjectNorthClockwiseDegrees);
        var position = doc.Application.Create.NewProjectPosition(
            RevitUnitConverter.MToFt(eastWestMeters),
            RevitUnitConverter.MToFt(northSouthMeters),
            RevitUnitConverter.MToFt(elevationMeters),
            angle);

        doc.ActiveProjectLocation.SetProjectPosition(XYZ.Zero, position);
    }
}
