using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using DHBIMWATER.Application.Interfaces.Quantity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DHBIMWATER.Revit.Commands.Quantity
{
    /// <summary>
    /// Revit 뷰에서 길이/면적 측정을 수행하는 ExternalEvent 기반 서비스.
    /// </summary>
    public class RevitMeasurePickService : IMeasurePickService, IExternalEventHandler
    {
        private readonly ExternalEvent _event;
        private MeasureKind _kind;
        private TaskCompletionSource<MeasureResult?>? _tcs;

        public RevitMeasurePickService()
        {
            _event = ExternalEvent.Create(this);
        }

        public Task<MeasureResult?> PickAsync(MeasureKind kind)
        {
            _tcs?.TrySetResult(null);

            _kind = kind;
            _tcs = new TaskCompletionSource<MeasureResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _event.Raise();
            return _tcs.Task;
        }

        public void Execute(UIApplication uiapp)
        {
            var tcs = _tcs;
            if (tcs == null)
                return;

            try
            {
                var uidoc = uiapp.ActiveUIDocument;
                if (uidoc == null)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                MeasureResult? result = _kind switch
                {
                    MeasureKind.Length => MeasureLength(uidoc),
                    MeasureKind.Area => MeasureArea(uidoc),
                    _ => null
                };

                tcs.TrySetResult(result);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(null);
                TaskDialog.Show("측정 오류", ex.Message);
            }
            finally
            {
                if (ReferenceEquals(_tcs, tcs))
                    _tcs = null;
            }
        }

        private static MeasureResult? MeasureLength(UIDocument uidoc)
        {
            if (uidoc.ActiveView is View3D view3D)
                return MeasureLengthOn3D(uidoc, view3D);

            return PickAndSumLength(uidoc);
        }

        private static MeasureResult? MeasureLengthOn3D(UIDocument uidoc, View3D view)
        {
            var doc = uidoc.Document;
            var reference = uidoc.Selection.PickObject(
                ObjectType.Face,
                "작업기준면이 될 면을 먼저 선택하세요. 취소: ESC");
            var element = doc.GetElement(reference);

            if (element?.GetGeometryObjectFromReference(reference) is not PlanarFace planarFace)
            {
                TaskDialog.Show("작업기준면", "평면인 면을 선택해야 합니다.");
                return null;
            }

            var previousSketchPlane = view.SketchPlane;
            SketchPlane? tempSketchPlane = null;
            var plane = Plane.CreateByNormalAndOrigin(planarFace.FaceNormal, planarFace.Origin);

            using (var tx = new Transaction(doc, "임시 작업기준면 설정"))
            {
                tx.Start();
                tempSketchPlane = SketchPlane.Create(doc, plane);
                view.SketchPlane = tempSketchPlane;
                tx.Commit();
            }

            try
            {
                return PickAndSumLength(uidoc);
            }
            finally
            {
                using var tx = new Transaction(doc, "작업기준면 복원");
                tx.Start();
                view.SketchPlane = previousSketchPlane;

                if (previousSketchPlane == null && tempSketchPlane != null)
                    doc.Delete(tempSketchPlane.Id);

                tx.Commit();
            }
        }

        private static MeasureResult? PickAndSumLength(UIDocument uidoc)
        {
            var points = new List<XYZ>();

            try
            {
                while (true)
                {
                    points.Add(uidoc.Selection.PickPoint(
                        "점을 연속 클릭하세요. 완료: ESC"));
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
            }

            if (points.Count < 2)
                return null;

            double totalFt = 0;
            for (int i = 1; i < points.Count; i++)
                totalFt += points[i].DistanceTo(points[i - 1]);

            double meters = UnitUtils.ConvertFromInternalUnits(totalFt, UnitTypeId.Meters);
            return new MeasureResult(meters, "m");
        }

        private static MeasureResult? MeasureArea(UIDocument uidoc)
        {
            var refFace = uidoc.Selection.PickObject(ObjectType.Face, "면을 선택하세요. 취소: ESC");
            var element = uidoc.Document.GetElement(refFace);
            var face = element?.GetGeometryObjectFromReference(refFace) as Face;
            if (face == null)
                return null;

            double m2 = UnitUtils.ConvertFromInternalUnits(face.Area, UnitTypeId.SquareMeters);
            return new MeasureResult(m2, "m²");
        }

        public string GetName() => "MeasurePick";
    }
}

