using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Provider;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
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
