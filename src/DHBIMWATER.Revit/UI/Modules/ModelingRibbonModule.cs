using Autodesk.Revit.UI;
using DHBIMWATER.Revit.Commands;
using System.Reflection;

namespace DHBIMWATER.Revit.UI.Modules
{
    internal class ModelingRibbonModule : IRibbonModule
    {
        public IEnumerable<RibbonItem> Build(UIControlledApplication app, string ribbonTabName)
        {
            RibbonPanel panel = app.CreateRibbonPanel(ribbonTabName, "Modeling");

            // 버튼이름, 리본에 표시될 텍스트, 어셈블리 경로, 실행될 커맨드 클래스 풀네임
            PushButtonData reservoirBtn = new PushButtonData("ReservoirCommand", "배수지\n모델링", Assembly.GetExecutingAssembly().Location, RevitCommandType<WaterTankCommand>.FullName);
            PushButtonData pumpingStationBtn = new PushButtonData("PumpingStationCommand", "펌프장\n모델링", Assembly.GetExecutingAssembly().Location, RevitCommandType<PumpingStationCommand>.FullName);

            PushButtonData valveRoomBtn = new PushButtonData("ValveRoomCommand", "밸브실\n모델링", Assembly.GetExecutingAssembly().Location, RevitCommandType<ValveRoomCommand>.FullName);
            PushButtonData pipeLayoutBtn = new PushButtonData("PipeLayoutCommand", "밸브실\n배관", Assembly.GetExecutingAssembly().Location, RevitCommandType<PipeLayoutCommand>.FullName);

            reservoirBtn.LargeImage = RibbonButtonImages.GetIcon("water-tap.png");
            pumpingStationBtn.LargeImage = RibbonButtonImages.GetIcon("pump.png");

            valveRoomBtn.LargeImage = RibbonButtonImages.GetIcon("water-tap.png");
            pipeLayoutBtn.LargeImage = RibbonButtonImages.GetIcon("water-tap.png");

            return
            [
                panel.AddItem(reservoirBtn),
                panel.AddItem(pumpingStationBtn),
                panel.AddItem(valveRoomBtn),
                panel.AddItem(pipeLayoutBtn),
            ];
        }
    }
}