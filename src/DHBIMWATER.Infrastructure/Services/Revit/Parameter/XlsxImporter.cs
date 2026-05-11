using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Parameter
{
    public static class XlsxImporter
    {

        public static Dictionary<int, Dictionary<string, string>> LoadParametersFromXlsx(string path)
        {
            // 결과: ElementId -> (ParamName -> Value)
            var result = new Dictionary<int, Dictionary<string, string>>();
            using var zip = ZipFile.OpenRead(path);

            // sharedStrings (있을 수도, 없을 수도)
            var sharedStrings = new List<string>();
            var sstEntry = zip.GetEntry("xl/sharedStrings.xml");
            if (sstEntry != null)
            {
                using var s = sstEntry.Open();
                var sst = XDocument.Load(s);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                sharedStrings = sst.Descendants(ns + "si")
                                   .Select(si => si.Descendants(ns + "t")
                                                   .Aggregate("", (acc, t) => acc + (string)t))
                                   .ToList();
            }

            // 워크시트 찾기 (sheet1 우선, 없으면 첫 시트)
            ZipArchiveEntry sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml")
                ?? zip.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase)
                    && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
            if (sheetEntry == null)
                throw new InvalidDataException("XLSX에서 워크시트를 찾을 수 없습니다.");
            using var wsStream = sheetEntry.Open();
            var wsDoc = XDocument.Load(wsStream);
            XNamespace m = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var rows = wsDoc.Root?.Element(m + "sheetData")?.Elements(m + "row")?.ToList()
                        ?? new List<XElement>();
            if (rows.Count == 0)
                return result;

            // 1행: 헤더
            var headerCells = ParseRowCells(rows[0], m, sharedStrings);
            if (headerCells.Count == 0)
                throw new InvalidDataException("첫 행(헤더)에 데이터가 없습니다.");

            // 셀 참조(A1, B1) 기반으로 열 인덱스 추출 → 최대 열 수
            int maxCol = headerCells.Keys.Count == 0 ? 0 : headerCells.Keys.Max() + 1;
            var headers = new string[maxCol];
            foreach (var kv in headerCells)
                headers[kv.Key] = kv.Value?.Trim() ?? "";

            // ElementId 열 찾기(대소문자 무시)
            int elementCol = Array.FindIndex(headers, h => !string.IsNullOrWhiteSpace(h) &&
                string.Equals(h, "ElementId", StringComparison.OrdinalIgnoreCase));
            if (elementCol < 0)
                throw new InvalidDataException("헤더에 'ElementId' 열이 없습니다.");

            // 2행~: 데이터
            for (int r = 1; r < rows.Count; r++)
            {
                var cells = ParseRowCells(rows[r], m, sharedStrings);
                if (!cells.TryGetValue(elementCol, out string idStr) || string.IsNullOrWhiteSpace(idStr))
                    continue;
                if (!TryParseIntFromNumericString(idStr, out int idInt))
                    continue;
                int eid = idInt;
                if (!result.TryGetValue(eid, out var map))
                {
                    map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    result[eid] = map;
                }

                for (int c = 0; c < headers.Length; c++)
                {
                    string name = headers[c];
                    if (string.IsNullOrWhiteSpace(name) || c == elementCol)
                        continue;
                    if (!cells.TryGetValue(c, out string val) || val == null)
                        val = "";
                    map[name] = val.Trim();
                }
            }

            return result;
        }

        // 한 행의 <c> 셀들을 "열 인덱스 -> 값"으로 파싱
        public static Dictionary<int, string> ParseRowCells(XElement row, XNamespace ns, List<string> sharedStrings)
        {
            var dict = new Dictionary<int, string>();
            foreach (var c in row.Elements(ns + "c"))
            {
                string r = (string)c.Attribute("r") ?? "A1";
                int col = GetColIndex(r); // A→0, B→1, ...
                string t = (string)c.Attribute("t");
                string val = "";
                if (string.Equals(t, "inlineStr", StringComparison.OrdinalIgnoreCase))
                {
                    val = c.Element(ns + "is")?.Element(ns + "t")?.Value ?? "";
                }
                else if (string.Equals(t, "s", StringComparison.OrdinalIgnoreCase))
                {
                    // shared string
                    string idx = c.Element(ns + "v")?.Value;
                    if (int.TryParse(idx, out int si) && si >= 0 && si < sharedStrings.Count)
                        val = sharedStrings[si] ?? "";
                    else
                        val = "";
                }
                else
                {
                    // 일반 숫자/텍스트
                    val = c.Element(ns + "v")?.Value ?? "";
                }

                dict[col] = val;
            }

            return dict;
        }

        // 셀 참조의 열 인덱스(A→0, Z→25, AA→26 …)

        public static int GetColIndex(string cellRef)
        {
            int i = 0;
            while (i < cellRef.Length && char.IsLetter(cellRef[i]))
                i++;
            string letters = cellRef.Substring(0, i).ToUpperInvariant();
            int col = 0;
            foreach (char ch in letters)
            {
                col = col * 26 + ch - 'A' + 1;
            }

            return col - 1;
        }

        // "123" 또는 "123.0" 같은 문자열에서 정수 파싱
        public static bool TryParseIntFromNumericString(string s, out int value)
        {
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return true;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                // 정확히 정수인지 확인(1e-6 허용)
                double r = Math.Round(d);
                if (Math.Abs(d - r) < 1e-6)
                {
                    value = (int)r;
                    return true;
                }
            }

            value = 0;
            return false;
        }

    }
}
