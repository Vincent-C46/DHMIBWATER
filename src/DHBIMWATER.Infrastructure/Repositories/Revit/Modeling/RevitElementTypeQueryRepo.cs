using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Provider;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling
{
    internal class RevitElementTypeQueryRepo : IElementTypeQueryRepo
    {
        private readonly Func<Document?> _docProvider;

        public RevitElementTypeQueryRepo(Func<Document?> docProvider)
        {
            _docProvider = docProvider ?? throw new ArgumentNullException(nameof(docProvider));
        }
        public IEnumerable<string> GetBeamTypeNames()
        {
            var doc = _docProvider();
            if (doc is null) return Enumerable.Empty<string>();

            try
            {
                var beamTypes = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsElementType()
                    .Cast<FamilySymbol>();

                var rcBeamTypes = beamTypes.Where(fs =>
                    {
                        var matParam = fs.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM);

                        if (matParam == null)
                            return true;

                        var matId = matParam.AsElementId();

                        if (matId == ElementId.InvalidElementId)
                            return true;
                        var mat = doc.GetElement(matParam.AsElementId()) as Material;
                        // 스틸 필터링
                        if (mat != null && (mat.Name.Contains("강철") || mat.Name.Contains("스틸") || mat.Name.Contains("steel")))
                            return false;

                        return true;
                    })
                    .Select(fs => fs.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .OrderBy(n => n);

                return rcBeamTypes;
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }
        public IEnumerable<string> GetAdaptiveBendTypeNames()
        {
            var doc = _docProvider(); if (doc is null) return Enumerable.Empty<string>();
            try { return new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().Where(x => AdaptiveComponentFamilyUtils.IsAdaptiveComponentFamily(x.Family)).Select(x => $"{x.Family.Name} : {x.Name}").Distinct().OrderBy(x => x).ToList(); }
            catch { return Enumerable.Empty<string>(); }
        }
        public int GetAdaptiveBendPointCount(string familyTypeName)
        {
            var doc = _docProvider(); if (doc is null || string.IsNullOrWhiteSpace(familyTypeName)) return -1;
            var separator = familyTypeName.LastIndexOf(" : ", StringComparison.Ordinal); if (separator <= 0 || separator >= familyTypeName.Length - 3) return -1;
            var familyName = familyTypeName[..separator]; var typeName = familyTypeName[(separator + 3)..];
            try
            {
                var symbol = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().FirstOrDefault(x => x.Family.Name == familyName && x.Name == typeName);
                if (symbol is null) return -1;
                var familyDocument = doc.EditFamily(symbol.Family);
                try { return new FilteredElementCollector(familyDocument).OfCategory(BuiltInCategory.OST_AdaptivePoints).WhereElementIsNotElementType().GetElementCount(); }
                finally { familyDocument.Close(false); }
            }
            catch { return -1; }
        }
        public IEnumerable<string> GetBeamInstanceParameterNames(string beamTypeName)
        {
            var doc = _docProvider();
            if (doc is null || string.IsNullOrWhiteSpace(beamTypeName)) return Enumerable.Empty<string>();

            try
            {
                var symbol = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsElementType()
                    .Cast<FamilySymbol>().FirstOrDefault(x => x.Name == beamTypeName);
                if (symbol is null) return Enumerable.Empty<string>();

                // 인스턴스 전용 파라미터는 유형 자체에서는 조회할 수 없어, 이미 배치된 인스턴스가 있으면 그걸 우선 쓴다.
                // TODO: 배치된 인스턴스가 하나도 없는 유형은 유형 파라미터만 후보로 보여준다(인스턴스 전용 파라미터 누락 가능).
                var sampleInstance = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsNotElementType()
                    .Cast<FamilyInstance>().FirstOrDefault(x => x.Symbol.Id == symbol.Id);
                var source = (Element?)sampleInstance ?? symbol;
                return source.Parameters.Cast<Parameter>().Select(p => p.Definition?.Name).Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList()!;
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }
        public IEnumerable<string> GetPipingSystemTypeNames() => GetNames(typeof(PipingSystemType));
        public IEnumerable<string> GetPipeTypeNames() => GetNames(typeof(PipeType));
        private IEnumerable<string> GetNames(Type type)
        {
            var doc = _docProvider(); if (doc is null) return Enumerable.Empty<string>();
            try { return new FilteredElementCollector(doc).OfClass(type).WhereElementIsElementType().Cast<ElementType>().Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList(); }
            catch { return Enumerable.Empty<string>(); }
        }
        public IEnumerable<string> GetColumnTypeNames()
        {
            var doc = _docProvider();
            if (doc is null) return Enumerable.Empty<string>();

            try
            {
                var collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .WhereElementIsElementType();

                return collector
                    .Cast<FamilySymbol>()
                    .Where(fs => fs.Category != null && fs.Category.Id.Value == (int)BuiltInCategory.OST_StructuralColumns)
                    .Select(fs => fs.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }
        public IEnumerable<string> GetFoundationTypeNames()
        {
            var doc = _docProvider();
            if (doc is null) return Enumerable.Empty<string>();

            try
            {
                var collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .WhereElementIsElementType();

                return collector
                    .Cast<FamilySymbol>()
                    .Where(fs => fs.Category != null && fs.Category.Id.Value == (int)BuiltInCategory.OST_StructuralFoundation)
                    .Select(fs => fs.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }
        public IEnumerable<string> GetSlabTypeNames()
        {
            var doc = _docProvider();
            if (doc is null) return Enumerable.Empty<string>();

            try
            {
                var collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FloorType))
                    .WhereElementIsElementType();

                return collector
                    .Cast<FloorType>()
                    .Select(ft => ft.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }
        public IEnumerable<string> GetWallTypeNames()
        {
            var doc = _docProvider();
            if (doc is null) return Enumerable.Empty<string>();

            try
            {
                var collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .WhereElementIsElementType();

                return collector
                    .Cast<WallType>()
                    .Where(wt => wt.Kind != WallKind.Curtain)
                    .Select(wt => wt.Name)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }
    }
}
