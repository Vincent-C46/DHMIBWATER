using DHBIMWATER.Application.DTOs.Gis;

namespace DHBIMWATER.Application.Interfaces.Gis;

public interface IShapefileReader
{
    ShapefileReadResult Read(string shpPath);
}
