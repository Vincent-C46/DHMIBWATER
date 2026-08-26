using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Helpers;

/// <summary>Adaptive 패밀리의 형상 구동 매개변수를 Double/문자열/정수 저장 형식에 맞춰 기록한다.</summary>
internal static class AdaptiveParameterWriter
{
    public static bool TryWrite(FamilyInstance instance, string name, string textValue, double? lengthValueFt, int? integerValue)
    {
        var parameter = instance.LookupParameter(name);
        if (parameter is not { IsReadOnly: false }) return false;

        return parameter.StorageType switch
        {
            StorageType.Double when lengthValueFt is not null => parameter.Set(lengthValueFt.Value),
            StorageType.String => parameter.Set(textValue),
            StorageType.Integer when integerValue is not null => parameter.Set(integerValue.Value),
            _ => false
        };
    }
}
