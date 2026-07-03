using DHBIMWATER.Application.DTOs.Revit.Elements;
using Xunit;

namespace DHBIMWATER.UI.Tests.DTOs.Revit.Elements;

public class RevitElementIdTypeTests
{
    [Theory]
    [InlineData(typeof(RevitElementDto))]
    [InlineData(typeof(RevitWallDto))]
    [InlineData(typeof(RevitColumnDto))]
    [InlineData(typeof(RevitSlabDto))]
    public void ElementId_UsesLong(Type dtoType)
    {
        var property = dtoType.GetProperty("ElementId");

        Assert.NotNull(property);
        Assert.Equal(typeof(long), property!.PropertyType);
    }
}
