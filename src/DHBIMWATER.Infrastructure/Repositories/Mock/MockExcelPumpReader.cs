using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    public class MockExcelPumpReader : IExcelReader
    {
        public IEnumerable<PumpExcelDto> Read(string filePath, string sheetName)
        {
            throw new NotImplementedException();
        }
    }
}
