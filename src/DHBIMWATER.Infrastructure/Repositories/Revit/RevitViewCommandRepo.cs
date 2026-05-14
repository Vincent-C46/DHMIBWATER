using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using System.Diagnostics;
using System.Windows.Controls;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    public class RevitViewCommandRepo : IViewCommandRepo
    {
        private readonly Func<Document?> _doc;

        public RevitViewCommandRepo(Func<Document?> doc)
        {
            _doc = doc;
        }

        public int CreateSectionView(SectionViewDefinition sectionViewDef)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var vft = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.Section);

            // 기존 SectionView 있으면 삭제
            var existingSectionView = new FilteredElementCollector(doc)
                              .OfClass(typeof(ViewSection))
                              .WhereElementIsNotElementType()
                              .Cast<ViewSection>()
                              .FirstOrDefault(vs => vs.Name == sectionViewDef.Name);

            if(existingSectionView!=null)
            {
                using var subTx = new SubTransaction(doc);
                subTx.Start();
                doc.Delete(existingSectionView.Id);
                subTx.Commit();
            }
            doc.Regenerate();

            Transform transform = new Transform(Transform.Identity);
            transform.Origin = XYZ.Zero;
            transform.BasisX = new XYZ(sectionViewDef.BasisX.X, sectionViewDef.BasisX.Y, sectionViewDef.BasisX.Z).Normalize();
            transform.BasisZ = new XYZ(sectionViewDef.BasisZ.X, sectionViewDef.BasisZ.Y, sectionViewDef.BasisZ.Z).Normalize();
            transform.BasisY = transform.BasisX.CrossProduct(transform.BasisZ).Normalize();
       
            var boundingBox = new BoundingBoxXYZ();
            boundingBox.Transform = transform;

            var boundingBoxInversedMin = transform.Inverse.OfPoint(
                new XYZ(UC.MmToFt(sectionViewDef.Min.X),
                UC.MmToFt(sectionViewDef.Min.Y),
                UC.MmToFt(sectionViewDef.Min.Z)));
            var boundingBoxInversedMax = transform.Inverse.OfPoint(
                new XYZ(UC.MmToFt(sectionViewDef.Max.X),
                UC.MmToFt(sectionViewDef.Max.Y),
                UC.MmToFt(sectionViewDef.Max.Z)));

            boundingBox.Min = new XYZ(boundingBoxInversedMin.X < boundingBoxInversedMax.X ? boundingBoxInversedMin.X : boundingBoxInversedMax.X,
                                      boundingBoxInversedMin.Y < boundingBoxInversedMax.Y ? boundingBoxInversedMin.Y : boundingBoxInversedMax.Y,
                                      boundingBoxInversedMin.Z < boundingBoxInversedMax.Z ? boundingBoxInversedMin.Z : boundingBoxInversedMax.Z);
            boundingBox.Max = new XYZ(boundingBoxInversedMin.X > boundingBoxInversedMax.X ? boundingBoxInversedMin.X : boundingBoxInversedMax.X,
                                      boundingBoxInversedMin.Y > boundingBoxInversedMax.Y ? boundingBoxInversedMin.Y : boundingBoxInversedMax.Y,
                                      boundingBoxInversedMin.Z > boundingBoxInversedMax.Z ? boundingBoxInversedMin.Z : boundingBoxInversedMax.Z);

            if (boundingBox.Min.X - boundingBox.Max.X == 0 || boundingBox.Min.Y - boundingBox.Max.Y == 0 || boundingBox.Min.Z - boundingBox.Max.Z == 0)
            {
                TaskDialog.Show("Error", "Min, Max 확인");
            }

            var viewSection = ViewSection.CreateSection(doc, vft.Id, boundingBox);
            viewSection.Name = sectionViewDef.Name;

            return (int)viewSection.Id.Value;

        }

        //public int UpdateSectionView(SectionViewDefinition sectionViewDef)
        //{
        //    var doc = _doc();
        //    if (doc == null) return 0;

        //    var existingSectionView = new FilteredElementCollector(doc)
        //                                  .OfClass(typeof(ViewSection))
        //                                  .WhereElementIsNotElementType()
        //                                  .Cast<ViewSection>()
        //                                  .FirstOrDefault(vs => vs.Name == sectionViewDef.Name);

        //    Transform transform = new Transform(Transform.Identity);
        //    transform.Origin = XYZ.Zero;
        //    transform.BasisX = new XYZ(sectionViewDef.BasisX.X, sectionViewDef.BasisX.Y, sectionViewDef.BasisX.Z).Normalize();
        //    transform.BasisZ = new XYZ(sectionViewDef.BasisZ.X, sectionViewDef.BasisZ.Y, sectionViewDef.BasisZ.Z).Normalize();
        //    transform.BasisY = transform.BasisX.CrossProduct(transform.BasisZ).Normalize();

        //    var boundingBox = new BoundingBoxXYZ();
        //    boundingBox.Transform = transform;

        //    var boundingBoxInversedMin = transform.Inverse.OfPoint(
        //        new XYZ(UC.MmToFt(sectionViewDef.Min.X),
        //        UC.MmToFt(sectionViewDef.Min.Y),
        //        UC.MmToFt(sectionViewDef.Min.Z)));
        //    var boundingBoxInversedMax = transform.Inverse.OfPoint(
        //        new XYZ(UC.MmToFt(sectionViewDef.Max.X),
        //        UC.MmToFt(sectionViewDef.Max.Y),
        //        UC.MmToFt(sectionViewDef.Max.Z)));

        //    boundingBox.Min = new XYZ(boundingBoxInversedMin.X < boundingBoxInversedMax.X ? boundingBoxInversedMin.X : boundingBoxInversedMax.X,
        //                              boundingBoxInversedMin.Y < boundingBoxInversedMax.Y ? boundingBoxInversedMin.Y : boundingBoxInversedMax.Y,
        //                              boundingBoxInversedMin.Z < boundingBoxInversedMax.Z ? boundingBoxInversedMin.Z : boundingBoxInversedMax.Z);
        //    boundingBox.Max = new XYZ(boundingBoxInversedMin.X > boundingBoxInversedMax.X ? boundingBoxInversedMin.X : boundingBoxInversedMax.X,
        //                              boundingBoxInversedMin.Y > boundingBoxInversedMax.Y ? boundingBoxInversedMin.Y : boundingBoxInversedMax.Y,
        //                              boundingBoxInversedMin.Z > boundingBoxInversedMax.Z ? boundingBoxInversedMin.Z : boundingBoxInversedMax.Z);

        //    existingSectionView.CropBox = boundingBox;

        //    return (int)existingSectionView.Id.Value;
        //}
    }
}
