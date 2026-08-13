using Autodesk.Revit.Attributes;

namespace DHBIMWATER.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class CoordinateLinkCommand : EmbeddedDhBoostCommand
    {
        protected override string CommandTypeName => "DHBoost.Revit.Commands.CoordinateLink.CoordinateLinkCommandForWater";
    }
}