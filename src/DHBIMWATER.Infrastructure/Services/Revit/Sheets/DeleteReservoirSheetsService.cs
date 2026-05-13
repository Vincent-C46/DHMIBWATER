using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class DeleteReservoirSheetsService

    {
        private readonly Document _doc;

        public DeleteReservoirSheetsService(Document doc)
        {
            _doc = doc;
        }

        public void DeleteAll(string startSheetNumber, int totalSheetCount)
        {
            DeleteSheets(startSheetNumber, totalSheetCount);
            DeleteViews();
        }


        public void DeleteSheets(string startSheetNumber, int totalSheetCount)
        {
            var targetSheetNumbers = BuildReservoirSheetNumbers(startSheetNumber, totalSheetCount);

            var activeViewId = _doc.ActiveView?.Id;

            var sheetIds = new FilteredElementCollector(_doc)
                           .OfClass(typeof(ViewSheet))
                           .Cast<ViewSheet>()
                           .Where(x => x.Name.StartsWith("배수지 일반도(", StringComparison.OrdinalIgnoreCase))
                           .Where(x => activeViewId == null || x.Id != activeViewId)
                           .Select(x => x.Id)
                           .ToList();


            using (var tx = new Transaction(_doc, "Delete Reservoir Sheets"))
            {
                tx.Start();

                foreach (var id in sheetIds)
                {
                    try
                    {
                        _doc.Delete(id);
                    }
                    catch
                    {
                    }
                }

                tx.Commit();
            }
        }
        public void DeleteViews()
        {
            var activeViewId = _doc.ActiveView?.Id;

            var shtViewIds = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate)
                .Where(v => ViewCategoryService.HasViewCategory(v, "출력"))
                .Where(v => activeViewId == null || v.Id != activeViewId)
                .Select(v => v.Id)
                .ToList();

            using (var tx = new Transaction(_doc, "Delete Reservoir Views"))
            {
                tx.Start();

                foreach (var id in shtViewIds)
                {
                    try
                    {
                        _doc.Delete(id);
                    }
                    catch
                    {
                    }
                }

                tx.Commit();
            }
        }

        private static HashSet<string> BuildReservoirSheetNumbers(string startSheetNumber, int totalSheetCount)
        {
            var numbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < totalSheetCount; i++)
                numbers.Add(BuildSheetNumber(startSheetNumber, i));

            return numbers;
        }

        private static string BuildSheetNumber(string startSheetNumber, int offset)
        {
            var prefix = new string(startSheetNumber.TakeWhile(c => !char.IsDigit(c)).ToArray());
            var numberText = new string(startSheetNumber.SkipWhile(c => !char.IsDigit(c)).ToArray());

            if (!int.TryParse(numberText, out var startNumber))
                throw new InvalidOperationException($"시작 도면번호 형식이 올바르지 않습니다: {startSheetNumber}");

            var width = numberText.Length;
            return $"{prefix}{(startNumber + offset).ToString().PadLeft(width, '0')}";
        }

    }
}
