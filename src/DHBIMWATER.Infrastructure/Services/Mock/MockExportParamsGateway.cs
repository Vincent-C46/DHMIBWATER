using System.IO;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.Application.Interfaces.Parameter;

namespace DHBIMWATER.Infrastructure.Services.Mock
{
    public class MockExportParamsGateway : IExportParamsGateway
    {
        public IReadOnlyList<CategoryInfo> GetCategories()
        {
            return new List<CategoryInfo>
            {
                new CategoryInfo { Key = "OST_Walls", DisplayName = "Walls" },
                new CategoryInfo { Key = "OST_Doors", DisplayName = "Doors" },
                new CategoryInfo { Key = "OST_Windows", DisplayName = "Windows" }
            };
        }

        public IReadOnlyList<string> GetParameters(string categoryKey)
        {
            return new List<string>
            {
                "Type Name",
                "Width",
                "Height",
                "Mark"
            };
        }

        public void Export(string categoryKey, IList<string> paramNames, string filePath)
        {
            // Mock에서는 실제 Excel 대신 텍스트로 저장만 해도 OK
            var lines = new List<string>
            {
                $"Category: {categoryKey}",
                $"Params: {string.Join(",", paramNames)}",
                $"Mock Export OK"
            };

            File.WriteAllLines(filePath, lines);
        }
    }
}
