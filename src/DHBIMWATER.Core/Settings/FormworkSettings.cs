using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Core.Settings
{
    public class FormworkSettings
    {
        public WallFormworkSettings Walls { get; set; } = new();
        public ColumnFormworkSettings Columns { get; set; } = new();
        public BeamFormworkSettings Beams { get; set; } = new();
        public FloorFormworkSettings Floors { get; set; } = new();
        public FoundationFormworkSettings Foundation { get; set; } = new();
    }

    public class WallFormworkSettings
    {
        public FormworkType Exterior { get; set; } = FormworkType.Euroform;
        public FormworkType Interior { get; set; } = FormworkType.Euroform;
        public FormworkType End { get; set; } = FormworkType.Plywood3;
    }

    public class ColumnFormworkSettings
    {
        public FormworkType Side { get; set; } = FormworkType.Plywood3;
    }

    public class BeamFormworkSettings
    {
        public FormworkType Bottom { get; set; } = FormworkType.Plywood4;
        public FormworkType Side { get; set; } = FormworkType.Plywood3;
        public FormworkType End { get; set; } = FormworkType.Plywood3;
    }

    public class FloorFormworkSettings
    {
        public FormworkType Bottom { get; set; } = FormworkType.Plywood4;
        public FormworkType SideRc { get; set; } = FormworkType.Plywood3;
        public FormworkType SidePlain { get; set; } = FormworkType.Plywood6;
    }

    public class FoundationFormworkSettings
    {
        public FormworkType SideRc { get; set; } = FormworkType.Plywood4;
        public FormworkType SidePlain { get; set; } = FormworkType.Plywood6;
    }
}
