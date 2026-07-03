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

        private static MeasureResult MeasureLength(UIDocument uidoc)
        {
            var points = new List<XYZ>();

            while (true)
            {
                try
                {
                    points.Add(uidoc.Selection.PickPoint("길이를 측정할 점을 클릭하세요. 종료: ESC"));
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    if (points.Count == 0)
                        throw;
                    break;
                }
            }

            if (points.Count < 2)
                return new MeasureResult(0, "m");

            double totalFt = 0;
            for (int i = 1; i < points.Count; i++)
                totalFt += points[i].DistanceTo(points[i - 1]);

            double meters = UnitUtils.ConvertFromInternalUnits(totalFt, UnitTypeId.Meters);
            return new MeasureResult(meters, "m");
        }

        private static MeasureResult MeasureArea(UIDocument uidoc)
        {
            var refFace = uidoc.Selection.PickObject(ObjectType.Face, "면을 선택하세요. 취소: ESC");
            var element = uidoc.Document.GetElement(refFace);
            var face = element?.GetGeometryObjectFromReference(refFace) as Face;
            if (face == null)
                return new MeasureResult(0, "m²");

            double m2 = UnitUtils.ConvertFromInternalUnits(face.Area, UnitTypeId.SquareMeters);
            return new MeasureResult(m2, "m²");
        }

        public string GetName() => "MeasurePick";
    }
}
