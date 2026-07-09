namespace DHBIMWATER.Core.Structures
{
    public enum StairJustification
    {
        Left,
        Center,
        Right,
    }

    public class StairsDefinition
    {
        public string BaseLevelName { get; set; } = string.Empty;
        public string TopLevelName { get; set; } = string.Empty;
        public double TreadDepth { get; set; } // mm, 디딤판 깊이 (0이면 미반영 - StairsType 기본값 사용). Stairs.ActualTreadDepth에 대응
        public double MaxRiserHeight { get; set; } // mm, 최대 챌판 높이 (0이면 미반영 - StairsType 기본값 사용)
        public int RisersNumber { get; set; }  // 단수 (0이면 미반영 - Revit 자동계산). Stairs.DesiredRisersNumber에 대응
        public double BaseOffset { get; set; } // mm, 하부 레벨 기준 오프셋 (TODO: RevitStairCommandRepo에서 미반영 - 확인 필요)
        public double TopOffset { get; set; }  // mm, 상부 레벨 기준 오프셋 (TODO: RevitStairCommandRepo에서 미반영 - 확인 필요)
        public string TypeName { get; set; } = string.Empty; // 기존 StairsType 이름 (자동 생성 안 함, 없으면 실패)

        public List<StairsRunDefinition> Runs { get; set; } = new List<StairsRunDefinition>();
        public List<StairsLandingDefinition> Landings { get; set; } = new List<StairsLandingDefinition>();

        public string Category { get; set; } = "계단";
        public string ElementCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Part { get; set; } = string.Empty;
    }
}
