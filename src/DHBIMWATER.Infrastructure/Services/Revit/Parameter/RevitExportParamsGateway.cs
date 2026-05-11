using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.Application.Interfaces.Parameter;

namespace DHBIMWATER.Infrastructure.Services.Revit.Parameter
{
    public class RevitExportParamsGateway : IExportParamsGateway
    {
        private readonly Func<Document?> _docGetter;
        private readonly RevitCategoryProvider _catProvider;
        private readonly RevitCategoryParameterProvider _paramProvider;
        private readonly RevitExcelExporter _excelExporter;

        public RevitExportParamsGateway(
            Func<Document?> docGetter,
            RevitCategoryProvider catProvider,
            RevitCategoryParameterProvider paramProvider,
            RevitExcelExporter excelExporter)
        {
            _docGetter = docGetter;
            _catProvider = catProvider;
            _paramProvider = paramProvider;
            _excelExporter = excelExporter;
        }

        public IReadOnlyList<CategoryInfo> GetCategories()
        {
            var doc = _docGetter();
            if (doc == null) return Array.Empty<CategoryInfo>();
            return _catProvider.GetCategories(doc);
        }

        public IReadOnlyList<string> GetParameters(string categoryKey)
        {
            var doc = _docGetter();
            if (doc == null) return Array.Empty<string>();

            if (!Enum.TryParse(categoryKey, out BuiltInCategory bic))
                return Array.Empty<string>();

            return _paramProvider.GetParameters(doc, bic);
        }

        public void Export(string categoryKey, IList<string> paramNames, string filePath)
        {
            var doc = _docGetter();
            if (doc == null) return;

            if (!Enum.TryParse(categoryKey, out BuiltInCategory bic))
                return;

            var collector = new FilteredElementCollector(doc)
                .OfCategory(bic)
                .WhereElementIsNotElementType();

            var headers = new List<string> { "ElementId" };
            headers.AddRange(paramNames);

            var rows = new List<List<string>>();

            foreach (Element elem in collector)
            {
                var oneRow = new List<string>(headers.Count);
                oneRow.Add(elem.Id.ToString());

                foreach (string paramName in paramNames)
                {
                    Autodesk.Revit.DB.Parameter param = elem.LookupParameter(paramName);
                    string val = param != null ? GetParamValue(param, doc) : "";
                    oneRow.Add(val);
                }

                rows.Add(oneRow);
            }

            _excelExporter.Export(filePath, "Export", headers, rows);
        }

        private string GetParamValue(Autodesk.Revit.DB.Parameter param, Document doc)
        {
            try
            {
                switch (param.StorageType)
                {
                    case StorageType.String:
                        {
                            var s = param.AsString();
                            if (!string.IsNullOrEmpty(s)) return s;
                            return param.AsValueString() ?? "";
                        }

                    case StorageType.Double:
                        {
                            double d = param.AsDouble();
                            try
                            {
                                ForgeTypeId dut = param.GetUnitTypeId();
                                d = UnitUtils.ConvertFromInternalUnits(d, dut);
                            }
                            catch { }

                            if (double.IsNaN(d) || double.IsInfinity(d)) return "";
                            return d.ToString("G15", System.Globalization.CultureInfo.InvariantCulture);
                        }

                    case StorageType.Integer:
                        return param.AsInteger().ToString(System.Globalization.CultureInfo.InvariantCulture);

                    case StorageType.ElementId:
                        {
                            var id = param.AsElementId();
                            if (id == ElementId.InvalidElementId) return "";

                            var idef = param.Definition as InternalDefinition;
                            var bip = idef?.BuiltInParameter ?? BuiltInParameter.INVALID;

                            if (bip == BuiltInParameter.ELEM_FAMILY_PARAM)
                                return doc.GetElement(id)?.Name ?? "";

                            if (bip == BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM ||
                                bip == BuiltInParameter.ELEM_TYPE_PARAM)
                            {
                                if (doc.GetElement(id) is ElementType et)
                                    return et.Name ?? "";
                                return "";
                            }

                            if (bip == BuiltInParameter.LEVEL_PARAM ||
                                bip == BuiltInParameter.PHASE_CREATED ||
                                bip == BuiltInParameter.PHASE_DEMOLISHED)
                                return doc.GetElement(id)?.Name ?? "";

                            return id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }

                    default:
                        return param.AsValueString() ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

    }
}
