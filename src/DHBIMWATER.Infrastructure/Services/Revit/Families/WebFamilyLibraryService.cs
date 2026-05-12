using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Revit.Families;
using DHBIMWATER.Application.Interfaces.Families;
using System.Net.Http;
using System.Text.Json;
using System.IO;

namespace DHBIMWATER.Infrastructure.Services.Revit.Families
{
    public class WebFamilyLibraryService : IWebFamilyLibraryService
    {
        private readonly Document _doc;

        public WebFamilyLibraryService(Document doc)
        {
            _doc = doc;
        }

        public IList<WebFamilyLibraryItemDto> GetFamilies(string apiUrl)
        {
            if (string.IsNullOrWhiteSpace(apiUrl))
                return new List<WebFamilyLibraryItemDto>();

            using var client = new HttpClient();
            var json = client.GetStringAsync(apiUrl).GetAwaiter().GetResult();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<List<WebFamilyLibraryItemDto>>(json, options)
                ?? new List<WebFamilyLibraryItemDto>();
        }

        public WebFamilyLoadResultDto LoadFamily(string downloadUrl)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                return new WebFamilyLoadResultDto
                {
                    Success = false,
                    Message = "다운로드 URL이 비어 있습니다."
                };
            }

            string tempPath = string.Empty;

            try
            {
                using var client = new HttpClient();
                var bytes = client.GetByteArrayAsync(downloadUrl).GetAwaiter().GetResult();

                var fileName = TryGetFileName(downloadUrl);
                tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{fileName}");
                File.WriteAllBytes(tempPath, bytes);

                bool loaded;
                Family? family;

                using (var tx = new Transaction(_doc, "Load Web Family"))
                {
                    tx.Start();
                    loaded = _doc.LoadFamily(tempPath, new AlwaysOverwriteFamilyLoadOptions(), out family);
                    tx.Commit();
                }

                return new WebFamilyLoadResultDto
                {
                    Success = loaded,
                    Message = loaded ? "패밀리 로드가 완료되었습니다." : "패밀리 로드에 실패했습니다.",
                    FamilyName = family?.Name ?? string.Empty,
                    SavedPath = tempPath
                };
            }
            catch (Exception ex)
            {
                return new WebFamilyLoadResultDto
                {
                    Success = false,
                    Message = ex.Message,
                    SavedPath = tempPath
                };
            }
        }

        private static string TryGetFileName(string downloadUrl)
        {
            try
            {
                var uri = new Uri(downloadUrl);
                var fileName = Path.GetFileName(uri.LocalPath);
                if (!string.IsNullOrWhiteSpace(fileName))
                    return fileName;
            }
            catch
            {
            }

            return "family.rfa";
        }

        private class AlwaysOverwriteFamilyLoadOptions : IFamilyLoadOptions
        {
            public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
            {
                overwriteParameterValues = true;
                return true;
            }

            public bool OnSharedFamilyFound(
                Family sharedFamily,
                bool familyInUse,
                out FamilySource source,
                out bool overwriteParameterValues)
            {
                source = FamilySource.Family;
                overwriteParameterValues = true;
                return true;
            }
        }
    }
}
