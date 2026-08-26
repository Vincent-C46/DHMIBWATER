using Autodesk.Revit.DB;
using DHBIMWATER.Core.Gis;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Helpers;

/// <summary>DH_* 정보 매개변수를 GUID로 찾아 기록한다.</summary>
internal sealed class PipeParameterWriter
{
    private readonly HashSet<string> _missing = new(StringComparer.Ordinal);
    public IReadOnlyCollection<string> Missing => _missing;

    public void Text(Element element, string name, string? value) => Write(element, name, p => p.Set(value ?? string.Empty), StorageType.String);
    public void LengthMm(Element element, string name, double? millimeters)
    {
        if (millimeters is not null) Write(element, name, p => p.Set(UC.MmToFt(millimeters.Value)), StorageType.Double);
    }
    public void LengthM(Element element, string name, double meters) => Write(element, name, p => p.Set(UC.MToFt(meters)), StorageType.Double);
    public void AngleDeg(Element element, string name, double degrees) => Write(element, name, p => p.Set(UC.DegToRad(degrees)), StorageType.Double);
    public void Number(Element element, string name, double value) => Write(element, name, p => p.Set(value), StorageType.Double);
    public void Integer(Element element, string name, int value) => Write(element, name, p => p.Set(value), StorageType.Integer);
    public void YesNo(Element element, string name, bool value) => Write(element, name, p => p.Set(value ? 1 : 0), StorageType.Integer);

    private void Write(Element element, string name, Action<Parameter> set, StorageType expected)
    {
        if (!PipeAlignmentParameters.Guids.TryGetValue(name, out var guid)) { _missing.Add(name); return; }
        var parameter = element.get_Parameter(guid);
        if (parameter is not { IsReadOnly: false } || parameter.StorageType != expected) { _missing.Add(name); return; }
        set(parameter);
    }
}
