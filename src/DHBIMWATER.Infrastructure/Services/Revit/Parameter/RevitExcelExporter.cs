using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace DHBIMWATER.Infrastructure.Services.Revit.Parameter
{
    public class RevitExcelExporter 
    {
        public void Export(string filePath, string sheetName, List<string> headers, List<List<string>> rows)
        {
            XlsxWriter.SaveAsXlsx(filePath, sheetName, headers, rows);
        }
    } 
        public static class XlsxWriter
        {
            public static void SaveAsXlsx(string path, string sheetName, List<string> headers, List<List<string>> rows)
            {
                if (File.Exists(path)) File.Delete(path);
                using var zip = ZipFile.Open(path, ZipArchiveMode.Create);

                AddXml(zip, "[Content_Types].xml", BuildContentTypes());
                AddXml(zip, "_rels/.rels", BuildRelsRoot());
                AddXml(zip, "docProps/core.xml", BuildCoreProps());
                AddXml(zip, "docProps/app.xml", BuildAppProps(sheetName));
                AddXml(zip, "xl/workbook.xml", BuildWorkbook(sheetName));
                AddXml(zip, "xl/_rels/workbook.xml.rels", BuildWorkbookRels());
                AddXml(zip, "xl/styles.xml", BuildStyles());
                AddXml(zip, "xl/worksheets/sheet1.xml", BuildWorksheet(headers, rows));
            }

            private static XDocument BuildContentTypes()
            {
                XNamespace ct = "http://schemas.openxmlformats.org/package/2006/content-types";
                return new XDocument(
                    new XElement(ct + "Types",
                        new XElement(ct + "Default",
                            new XAttribute("Extension", "rels"),
                            new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                        new XElement(ct + "Default",
                            new XAttribute("Extension", "xml"),
                            new XAttribute("ContentType", "application/xml")),
                        new XElement(ct + "Override",
                            new XAttribute("PartName", "/docProps/core.xml"),
                            new XAttribute("ContentType", "application/vnd.openxmlformats-package.core-properties+xml")),
                        new XElement(ct + "Override",
                            new XAttribute("PartName", "/docProps/app.xml"),
                            new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.extended-properties+xml")),
                        new XElement(ct + "Override",
                            new XAttribute("PartName", "/xl/workbook.xml"),
                            new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                        new XElement(ct + "Override",
                            new XAttribute("PartName", "/xl/worksheets/sheet1.xml"),
                            new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")),
                        new XElement(ct + "Override",
                            new XAttribute("PartName", "/xl/styles.xml"),
                            new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"))
                    )
                );
            }

            private static XDocument BuildRelsRoot()
            {
                XNamespace rel = "http://schemas.openxmlformats.org/package/2006/relationships";
                return new XDocument(
                    new XElement(rel + "Relationships",
                        new XElement(rel + "Relationship",
                            new XAttribute("Id", "rId1"),
                            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                            new XAttribute("Target", "xl/workbook.xml")),
                        new XElement(rel + "Relationship",
                            new XAttribute("Id", "rId2"),
                            new XAttribute("Type", "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties"),
                            new XAttribute("Target", "docProps/core.xml")),
                        new XElement(rel + "Relationship",
                            new XAttribute("Id", "rId3"),
                            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties"),
                            new XAttribute("Target", "docProps/app.xml"))
                    )
                );
            }

            private static XDocument BuildCoreProps()
            {
                XNamespace cp = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
                XNamespace dc = "http://purl.org/dc/elements/1.1/";
                XNamespace dcterms = "http://purl.org/dc/terms/";
                XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

                string nowUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                return new XDocument(
                    new XElement(cp + "coreProperties",
                        new XAttribute(XNamespace.Xmlns + "cp", cp),
                        new XAttribute(XNamespace.Xmlns + "dc", dc),
                        new XAttribute(XNamespace.Xmlns + "dcterms", dcterms),
                        new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                        new XElement(dc + "title", "Export"),
                        new XElement(dc + "creator", "DHBIMWATER"),
                        new XElement(cp + "lastModifiedBy", "DHBIMWATER"),
                        new XElement(dcterms + "created", new XAttribute(xsi + "type", "dcterms:W3CDTF"), nowUtc),
                        new XElement(dcterms + "modified", new XAttribute(xsi + "type", "dcterms:W3CDTF"), nowUtc)
                    )
                );
            }

            private static XDocument BuildAppProps(string sheetName)
            {
                XNamespace ap = "http://schemas.openxmlformats.org/officeDocument/2006/extended-properties";
                XNamespace vt = "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes";
                string safeName = string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName;

                return new XDocument(
                    new XElement(ap + "Properties",
                        new XAttribute(XNamespace.Xmlns + "vt", vt),
                        new XElement(ap + "Application", "Microsoft Excel"),
                        new XElement(ap + "HeadingPairs",
                            new XElement(vt + "vector",
                                new XAttribute("size", 2),
                                new XAttribute("baseType", "variant"),
                                new XElement(vt + "variant", new XElement(vt + "lpstr", "Worksheets")),
                                new XElement(vt + "variant", new XElement(vt + "i4", 1))
                            )
                        ),
                        new XElement(ap + "TitlesOfParts",
                            new XElement(vt + "vector",
                                new XAttribute("size", 1),
                                new XAttribute("baseType", "lpstr"),
                                new XElement(vt + "lpstr", safeName)
                            )
                        ),
                        new XElement(ap + "AppVersion", "16.0300")
                    )
                );
            }

            private static XDocument BuildWorkbook(string sheetName)
            {
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
                string safeName = string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName;

                return new XDocument(
                    new XElement(ns + "workbook",
                        new XAttribute(XNamespace.Xmlns + "r", r),
                        new XElement(ns + "bookViews",
                            new XElement(ns + "workbookView",
                                new XAttribute("xWindow", "240"),
                                new XAttribute("yWindow", "105"),
                                new XAttribute("windowWidth", "14805"),
                                new XAttribute("windowHeight", "8010")
                            )
                        ),
                        new XElement(ns + "sheets",
                            new XElement(ns + "sheet",
                                new XAttribute("name", safeName),
                                new XAttribute("sheetId", "1"),
                                new XAttribute(r + "id", "rId1")
                            )
                        )
                    )
                );
            }

            private static XDocument BuildWorkbookRels()
            {
                XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
                return new XDocument(
                    new XElement(rel + "Relationships",
                        new XElement(rel + "Relationship",
                            new XAttribute("Id", "rId1"),
                            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                            new XAttribute("Target", "worksheets/sheet1.xml")),
                        new XElement(rel + "Relationship",
                            new XAttribute("Id", "rId2"),
                            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),
                            new XAttribute("Target", "../styles.xml"))
                    )
                );
            }

            private static XDocument BuildStyles()
            {
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                return new XDocument(
                    new XElement(ns + "styleSheet",
                        new XElement(ns + "fonts",
                            new XAttribute("count", 1),
                            new XElement(ns + "font",
                                new XElement(ns + "sz", new XAttribute("val", "11")),
                                new XElement(ns + "color", new XAttribute("rgb", "FF000000")),
                                new XElement(ns + "name", new XAttribute("val", "Calibri")),
                                new XElement(ns + "family", new XAttribute("val", "2"))
                            )
                        ),
                        new XElement(ns + "fills",
                            new XAttribute("count", 2),
                            new XElement(ns + "fill", new XElement(ns + "patternFill", new XAttribute("patternType", "none"))),
                            new XElement(ns + "fill", new XElement(ns + "patternFill", new XAttribute("patternType", "gray125")))
                        ),
                        new XElement(ns + "borders",
                            new XAttribute("count", 1),
                            new XElement(ns + "border",
                                new XElement(ns + "left"),
                                new XElement(ns + "right"),
                                new XElement(ns + "top"),
                                new XElement(ns + "bottom"),
                                new XElement(ns + "diagonal")
                            )
                        ),
                        new XElement(ns + "cellStyleXfs",
                            new XAttribute("count", 1),
                            new XElement(ns + "xf",
                                new XAttribute("numFmtId", "0"),
                                new XAttribute("fontId", "0"),
                                new XAttribute("fillId", "0"),
                                new XAttribute("borderId", "0")
                            )
                        ),
                        new XElement(ns + "cellXfs",
                            new XAttribute("count", 1),
                            new XElement(ns + "xf",
                                new XAttribute("numFmtId", "0"),
                                new XAttribute("fontId", "0"),
                                new XAttribute("fillId", "0"),
                                new XAttribute("borderId", "0"),
                                new XAttribute("xfId", "0")
                            )
                        ),
                        new XElement(ns + "cellStyles",
                            new XAttribute("count", 1),
                            new XElement(ns + "cellStyle",
                                new XAttribute("name", "Normal"),
                                new XAttribute("xfId", "0"),
                                new XAttribute("builtinId", "0")
                            )
                        )
                    )
                );
            }

            private static XDocument BuildWorksheet(List<string> headers, List<List<string>> rows)
            {
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                var sheetData = new XElement(ns + "sheetData");

                var headerRow = new XElement(ns + "row", new XAttribute("r", "1"));
                for (int c = 0; c < (headers?.Count ?? 0); c++)
                {
                    string text = SanitizeForXlsxText(headers[c] ?? "");
                    if (text.Length == 0) continue;
                    string addr = ToCol(c) + "1";
                    headerRow.Add(
                        new XElement(ns + "c",
                            new XAttribute("r", addr),
                            new XAttribute("t", "inlineStr"),
                            new XElement(ns + "is",
                                new XElement(ns + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), text))
                        )
                    );
                }
                sheetData.Add(headerRow);

                if (rows != null && rows.Count > 0)
                {
                    for (int r = 0; r < rows.Count; r++)
                    {
                        var row = rows[r] ?? new List<string>();
                        int rr = r + 2;

                        bool hasAny = false;
                        for (int c = 0; c < row.Count; c++)
                        {
                            string val = row[c] ?? "";
                            if (TryParseFiniteNumber(val, out _) || SanitizeForXlsxText(val).Length > 0) { hasAny = true; break; }
                        }
                        if (!hasAny) continue;

                        var dataRow = new XElement(ns + "row", 
                            new XAttribute("r", rr.ToString
                            (System.Globalization.CultureInfo.InvariantCulture)));

                        for (int c = 0; c < row.Count; c++)
                        {
                            string raw = row[c] ?? "";
                            if (raw.Length == 0) continue;

                            string addr = ToCol(c) + rr.ToString(System.Globalization.CultureInfo.InvariantCulture);

                            if (TryParseFiniteNumber(raw, out double d))
                            {
                                dataRow.Add(
                                    new XElement(ns + "c",
                                        new XAttribute("r", addr),
                                        new XElement(ns + "v", d.ToString(System.Globalization.CultureInfo.InvariantCulture))
                                    )
                                );
                            }
                            else
                            {
                                string text = SanitizeForXlsxText(raw);
                                if (text.Length == 0) continue;
                                dataRow.Add(
                                    new XElement(ns + "c",
                                        new XAttribute("r", addr),
                                        new XAttribute("t", "inlineStr"),
                                        new XElement(ns + "is",
                                            new XElement(ns + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), text))
                                    )
                                );
                            }
                        }

                        sheetData.Add(dataRow);
                    }
                }

                var ws = new XElement(ns + "worksheet",
                    new XElement(ns + "sheetViews",
                        new XElement(ns + "sheetView", new XAttribute("workbookViewId", "0"))
                    ),
                    new XElement(ns + "sheetFormatPr", new XAttribute("defaultRowHeight", "15")),
                    sheetData,
                    new XElement(ns + "pageMargins",
                        new XAttribute("left", "0.7"),
                        new XAttribute("right", "0.7"),
                        new XAttribute("top", "0.75"),
                        new XAttribute("bottom", "0.75"),
                        new XAttribute("header", "0.3"),
                        new XAttribute("footer", "0.3"))
                );

                return new XDocument(ws);
            }

            private static void AddXml(ZipArchive zip, string path, XDocument xdoc)
            {
                var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
                using var s = entry.Open();
                using var w = new StreamWriter(s, new UTF8Encoding(false));
                xdoc.Save(w);
            }

            private static bool TryParseFiniteNumber(string s, out double d)
            {
                if (!double.TryParse(s, System.Globalization.NumberStyles.Float,
                                     System.Globalization.CultureInfo.InvariantCulture, out d))
                    return false;
                if (double.IsNaN(d) || double.IsInfinity(d)) return false;
                return true;
            }

            private static string ToCol(int idx)
            {
                idx = Math.Max(0, idx);
                var sb = new StringBuilder();
                while (true)
                {
                    int rem = idx % 26;
                    sb.Insert(0, (char)('A' + rem));
                    idx = idx / 26 - 1;
                    if (idx < 0) break;
                }
                return sb.ToString();
            }

            private static string SanitizeForXlsxText(string s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                var sb = new StringBuilder(s.Length);
                foreach (char ch in s)
                {
                    if (ch == '\t' || ch == '\n' || ch == '\r' || ch >= 0x20) sb.Append(ch);
                }
                return sb.ToString();
            }
        }
    
}
