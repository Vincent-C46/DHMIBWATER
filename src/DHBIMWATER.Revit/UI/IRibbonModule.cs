using Autodesk.Revit.UI;

namespace DHBIMWATER.Revit.UI
{
    internal interface IRibbonModule
    {
        void Build(UIControlledApplication app, string ribbonTabName);
    }
}
