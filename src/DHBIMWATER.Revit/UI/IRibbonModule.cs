using Autodesk.Revit.UI;

namespace DHBIMWATER.Revit.UI
{
    internal interface IRibbonModule
    {
        IEnumerable<RibbonItem> Build(UIControlledApplication app, string ribbonTabName);
    }
}
