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
        public IEnumerable<string> GetAdaptiveComponentTypeNames()
        {
            var doc = _docProvider(); if (doc is null) return Enumerable.Empty<string>();
            try { return new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().Where(x => AdaptiveComponentFamilyUtils.IsAdaptiveComponentFamily(x.Family)).Select(x => $"{x.Family.Name} : {x.Name}").Distinct().OrderBy(x => x).ToList(); }
            catch { return Enumerable.Empty<string>(); }
        }
        /// <summary>
        /// 패밀리 문서에서 읽어 온 인스턴스 파라미터명 캐시. 키는 "문서Hash|패밀리명"이다.
        /// EditFamily는 패밀리 문서를 여는 무거운 호출이라 콤보박스를 열 때마다 반복하지 않도록 캐시한다.
        /// </summary>
        private readonly Dictionary<string, IReadOnlyList<string>> _familyInstanceParameterCache = new();

        /// <summary>
        /// 가변 패밀리 인스턴스에 쓸 수 있는 파라미터명.
        /// 인스턴스 전용 파라미터는 FamilySymbol에서 조회되지 않아 다음 순서로 근거를 고른다.
        /// ① 같은 <b>패밀리</b>의 배치된 인스턴스(파라미터 집합은 유형이 아니라 패밀리 단위라 형제 유형도 유효하다)
        /// ② 인스턴스가 하나도 없으면 패밀리 문서를 열어 FamilyManager의 인스턴스 파라미터를 읽고 캐시한다
        /// ③ 패밀리 문서를 열 수 없으면(인플레이스 등) 기존대로 유형 파라미터만 반환한다
        /// </summary>
        public IEnumerable<string> GetAdaptiveInstanceParameterNames(string familyTypeName)
        {
            var doc = _docProvider();
            if (doc is null || string.IsNullOrWhiteSpace(familyTypeName)) return Enumerable.Empty<string>();
            var separator = familyTypeName.LastIndexOf(" : ", StringComparison.Ordinal);
            if (separator <= 0 || separator >= familyTypeName.Length - 3) return Enumerable.Empty<string>();
            var familyName = familyTypeName.Substring(0, separator);
            var typeName = familyTypeName.Substring(separator + 3);
            try
            {
                var symbol = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                    .FirstOrDefault(x => x.Family.Name == familyName && x.Name == typeName);
                if (symbol is null) return Enumerable.Empty<string>();

                var sampleInstance = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
                    .FirstOrDefault(x => x.Symbol.Family.Id == symbol.Family.Id);
                var names = NamesOf(sampleInstance ?? (Element)symbol);
                if (sampleInstance is not null) return names;

                // 배치된 인스턴스가 없으면 유형 파라미터만으로는 OD·두께 같은 인스턴스 전용 항목이 빠진다.
                return names.Concat(GetFamilyInstanceParameterNames(doc, symbol.Family))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        private static List<string> NamesOf(Element source)
            => source.Parameters.Cast<Parameter>().Select(p => p.Definition?.Name).Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList()!;

        /// <summary>패밀리 문서를 열어 인스턴스 파라미터명을 읽는다. 실패(인플레이스·편집 불가)하면 빈 목록이다.</summary>
        private IReadOnlyList<string> GetFamilyInstanceParameterNames(Document doc, Family family)
        {
            var key = $"{doc.GetHashCode()}|{family.Name}";
            if (_familyInstanceParameterCache.TryGetValue(key, out var cached)) return cached;

            IReadOnlyList<string> names = Array.Empty<string>();
            try
            {
                var familyDocument = doc.EditFamily(family);
                try
                {
                    names = familyDocument.FamilyManager.Parameters.Cast<FamilyParameter>()
                        .Where(p => p.IsInstance).Select(p => p.Definition?.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()!;
                }
                finally { familyDocument.Close(false); }
            }
            catch { names = Array.Empty<string>(); }

            _familyInstanceParameterCache[key] = names;
            return names;
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
        public IEnumerable<string> GetPipeAccessoryTypeNames() => GetFamilySymbolNames(BuiltInCategory.OST_PipeAccessory);
        public IEnumerable<string> GetGenericModelTypeNames() => GetFamilySymbolNames(BuiltInCategory.OST_GenericModel);

        private IEnumerable<string> GetFamilySymbolNames(BuiltInCategory category)
        {
            var doc = _docProvider(); if (doc is null) return Enumerable.Empty<string>();
            try
            {
                // OfCategory(...).Cast<FamilySymbol>()는 카테고리에 FamilySymbol이 아닌 ElementType이 섞이면
                // InvalidCastException으로 조용히 빈 목록을 반환한다. GetColumnTypeNames 등과 같은
                // OfClass(FamilySymbol) 선先필터 방식으로 통일해 그 위험을 없앤다.
                return new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).WhereElementIsElementType()
                    .Cast<FamilySymbol>()
                    .Where(x => x.Category != null && x.Category.Id.Value == (int)category)
                    .Select(x => $"{x.Family.Name} : {x.Name}").Distinct().OrderBy(x => x).ToList();
            }
            catch { return Enumerable.Empty<string>(); }
        }
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
