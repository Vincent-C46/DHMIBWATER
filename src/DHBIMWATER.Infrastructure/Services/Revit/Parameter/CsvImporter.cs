using System.IO;

namespace DHBIMWATER.Infrastructure.Services.Revit.Parameter
{
    public class CsvImporter
    {
        public Dictionary<int, Dictionary<string, string>> LoadParametersFromCsv(string filePath)
        {
            var result = new Dictionary<int, Dictionary<string, string>>();

            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 2)
                throw new Exception("CSV 파일에 데이터가 없습니다.");

            var headers = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                var cells = lines[i].Split(',');

                if (!int.TryParse(cells[0], out int eidInt)) continue;

                int eid = eidInt;
                var paramDict = new Dictionary<string, string>();

                for (int j = 1; j < headers.Length && j < cells.Length; j++)
                {
                    string paramName = headers[j].Trim();
                    string paramValue = cells[j].Trim();
                    paramDict[paramName] = paramValue;
                }

                result[eid] = paramDict;
            }

            return result;
        }
    }
}
