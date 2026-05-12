using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Parameter;
using RevitElementId = Autodesk.Revit.DB.ElementId;



namespace DHBIMWATER.Infrastructure.Services.Revit.Parameter
{
    public class RevitImportParamsGateway : IImportParamsGateway
    {
        public Dictionary<int, Dictionary<string, string>> Load(string filePath)
        {
            string ext = Path.GetExtension(filePath)?.ToLowerInvariant();

            if (ext == ".xlsx")
                return XlsxImporter.LoadParametersFromXlsx(filePath);

            return new CsvImporter().LoadParametersFromCsv(filePath);
        }

        public int Apply(object context, Dictionary<int, Dictionary<string, string>> map, bool overwriteExisting)
        {
            if (context is not Document doc)
                throw new ArgumentException("Invalid context. Document required.");

            int modifiedCount = 0;

            using (Transaction tx = new Transaction(doc, "Import Parameters"))
            {
                tx.Start();

                foreach (var kvp in map)
                {
                    Element element = doc.GetElement(new RevitElementId(kvp.Key));
                    if (element == null) continue;

                    foreach (var paramKvp in kvp.Value)
                    {
                        string paramName = paramKvp.Key;
                        string paramValue = paramKvp.Value;

                        Autodesk.Revit.DB.Parameter param = element.LookupParameter(paramName);
                        if (param == null || param.IsReadOnly) continue;
                        if (!overwriteExisting && param.HasValue) continue;

                        bool changed = false;

                        switch (param.StorageType)
                        {
                            case StorageType.String:
                                string before = param.AsString();
                                string after = paramValue?.Trim() ?? "";
                                changed = before != after;
                                param.Set(after);
                                break;

                            case StorageType.Integer:
                                if (int.TryParse(paramValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intVal))
                                {
                                    changed = param.AsInteger() != intVal;
                                    param.Set(intVal);
                                }
                                break;

                            case StorageType.Double:
                                if (double.TryParse(paramValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double siValue))
                                {
                                    var displayUnitType = param.GetUnitTypeId();
                                    double internalValue = UnitUtils.ConvertToInternalUnits(siValue, displayUnitType);
                                    changed = Math.Abs(param.AsDouble() - internalValue) > 1e-9;
                                    param.Set(internalValue);
                                }
                                break;

                            case StorageType.ElementId:
                                if (XlsxImporter.TryParseIntFromNumericString(paramValue, out int refId))
                                {
                                    changed = param.AsElementId().Value != refId;
                                    param.Set(new RevitElementId(refId));
                                }
                                break;
                        }

                        if (changed) modifiedCount++;
                    }
                }

                tx.Commit();
            }

            return modifiedCount;
        }
    }
}
