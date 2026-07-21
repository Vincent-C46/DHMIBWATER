using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Geometry;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Geometry;

public sealed class RevitNetFaceVisualizerRepo : INetFaceVisualizerRepo
{
    private const string VisualizationCategory = "순면적시각화";
    private readonly Func<Document?> _doc;
    private readonly RevitIntersectingElementFinder _finder;

    public RevitNetFaceVisualizerRepo(Func<Document?> doc, RevitIntersectingElementFinder finder)
    {
        _doc = doc;
        _finder = finder;
    }

    public int Visualize(IReadOnlyList<long> elementIds)
    {
        var doc = _doc();
        if (doc == null) return 0;

        DeletePreviousVisualizations(doc);

        var created = 0;
        foreach (var elementId in elementIds.Distinct())
        {
            var netSolids = _finder.ComputeNetFaceSolids(elementId);
            if (netSolids.Count == 0) continue;

            var directShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            directShape.SetShape(netSolids.Select(x => (GeometryObject)x.NetSolid).ToList());
            directShape.Name = $"DH_순면적_{elementId}";
            directShape.LookupParameter("DH_Category")?.Set(VisualizationCategory);
            created++;
        }

        return created;
    }

    private static void DeletePreviousVisualizations(Document doc)
    {
        var ids = new FilteredElementCollector(doc)
            .OfClass(typeof(DirectShape))
            .Cast<DirectShape>()
            .Where(shape => shape.LookupParameter("DH_Category")?.AsString() == VisualizationCategory)
            .Select(shape => shape.Id)
            .ToList();

        if (ids.Count > 0)
            doc.Delete(ids);
    }
}
