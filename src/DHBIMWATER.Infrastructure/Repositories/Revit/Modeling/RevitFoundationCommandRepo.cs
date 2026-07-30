using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling;

/// <summary>문서에 있는 첫 독립기초를 기준으로 공기밸브실용 유형과 인스턴스를 만든다.</summary>
public sealed class RevitFoundationCommandRepo : IFoundationCommandRepo
{
    private readonly Func<Document?> _doc;

    public RevitFoundationCommandRepo(Func<Document?> doc) => _doc = doc;

    public int CreateFoundationFromFirstInstance(FoundationDefinition definition)
    {
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서를 찾을 수 없습니다.");
        var source = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .WhereElementIsNotElementType()
            .OfType<FamilyInstance>()
            .OrderBy(instance => instance.Id.Value)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("공기밸브실을 만들기 전에 복제 기준이 될 독립기초를 하나 배치해야 합니다.");

        var targetType = GetOrCreateAirValveFoundationType(doc, source.Symbol, definition.Thickness);
        var sourceBox = source.get_BoundingBox(null)
            ?? throw new InvalidOperationException("복제 기준 독립기초의 위치 정보를 읽을 수 없습니다.");
        var sourceCenter = (sourceBox.Min + sourceBox.Max) * 0.5;
        var targetTop = UC.MmToFt(definition.Position.Z);
        var translation = new XYZ(
            UC.MmToFt(definition.Position.X) - sourceCenter.X,
            UC.MmToFt(definition.Position.Y) - sourceCenter.Y,
            targetTop - sourceBox.Max.Z);

        var copiedId = ElementTransformUtils.CopyElement(doc, source.Id, translation).Single();
        var foundation = (FamilyInstance)doc.GetElement(copiedId);
        foundation.ChangeTypeId(targetType.Id);
        foundation.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
        foundation.LookupParameter("DH_Category")?.Set("독립기초");
        foundation.LookupParameter("DH_ElementCode")?.Set(definition.ElementCode);
        foundation.LookupParameter("DH_Part")?.Set(definition.Part);
        foundation.LookupParameter("DH_Zone")?.Set(definition.Zone);
        return (int)foundation.Id.Value;
    }

    private static FamilySymbol GetOrCreateAirValveFoundationType(Document doc, FamilySymbol sourceType, double thicknessMm)
    {
        var typeName = $"공기밸브실 기초 - {thicknessMm:0.##}mm";
        var existing = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .WhereElementIsElementType()
            .OfType<FamilySymbol>()
            .FirstOrDefault(symbol => symbol.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
        if (existing != null) return existing;

        var duplicated = (FamilySymbol)sourceType.Duplicate(typeName);
        var thicknessParameter = duplicated.get_Parameter(BuiltInParameter.STRUCTURAL_FOUNDATION_THICKNESS);
        if (thicknessParameter == null || thicknessParameter.IsReadOnly)
            throw new InvalidOperationException("복제 기준 독립기초 유형에서 기초 두께 파라미터를 찾을 수 없습니다.");

        thicknessParameter.Set(UC.MmToFt(thicknessMm));
        return duplicated;
    }
}
