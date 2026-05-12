using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.Application.DTOs.Revit.Sheet;
using DHBIMWATER.Application.DTOs.Revit.Sheets;


namespace DHBIMWATER.Application.UseCases.Sheets
{
    public class WaterReservoirUseCase : IWaterReservoirUseCase
    {
        private readonly ISheetUseCase _sheetUseCase;
        private string _originalViewId;
        private string _reservoirStartSheetNumber = "C-080";
        private int _reservoirTotalSheetCount = 12;

        public WaterReservoirUseCase(ISheetUseCase sheetUseCase)
        {
            _sheetUseCase = sheetUseCase;
        }

        public WaterReservoirCreateResult CreateReservoirSheets(string startSheetNumber, int totalSheetCount)
        {
            if (totalSheetCount < 3)
                throw new InvalidOperationException("배수지 도면은 최소 3장 이상이어야 합니다.");

            var result = new WaterReservoirCreateResult();

            _reservoirStartSheetNumber = startSheetNumber;
            _reservoirTotalSheetCount = totalSheetCount;

            var titleBlocks = _sheetUseCase.GetTitleBlocks();
            if (titleBlocks == null || titleBlocks.Count == 0)
                throw new InvalidOperationException("사용 가능한 타이틀블록이 없습니다.");

            var placements = BuildReservoirViewPlacements();
            var sheets = BuildReservoirSheetDefinitions(startSheetNumber, totalSheetCount, placements);

            var existingSheets = _sheetUseCase.GetSheets();

            var existingNumbers = new HashSet<string>(
                existingSheets.Select(x => x.SheetNumber),
                StringComparer.OrdinalIgnoreCase);

            var existingNames = new HashSet<string>(
                existingSheets.Select(x => x.SheetName),
                StringComparer.OrdinalIgnoreCase);

            foreach (var sheet in sheets)
            {
                bool duplicateNumber = existingNumbers.Contains(sheet.Number);
                bool duplicateName = existingNames.Contains(sheet.Name);

                if (duplicateNumber)
                    result.DuplicateSheetNumbers.Add(sheet.Number);

                if (duplicateName)
                    result.DuplicateSheetNames.Add(sheet.Name);

                if (duplicateNumber || duplicateName)
                    continue;

                var titleBlock = titleBlocks.FirstOrDefault(x => x.DisplayName == sheet.TitleBlockName);
                if (titleBlock == null)
                    throw new InvalidOperationException($"타이틀블록 '{sheet.TitleBlockName}'을 찾을 수 없습니다.");

                _sheetUseCase.CreateSheet(titleBlock.Id, sheet.Number, sheet.Name);


                existingNumbers.Add(sheet.Number);
                existingNames.Add(sheet.Name);
                result.CreatedCount++;
            }
            return result;
        }

        public void SetReservoirSheetRange(string startSheetNumber, int totalSheetCount)
        {
            if (string.IsNullOrWhiteSpace(startSheetNumber))
                throw new InvalidOperationException("시작 도면번호가 비어 있습니다.");

            if (totalSheetCount <= 0)
                throw new InvalidOperationException("총 장수는 1 이상이어야 합니다.");

            _reservoirStartSheetNumber = startSheetNumber;
            _reservoirTotalSheetCount = totalSheetCount;
        }

        private static List<ReservoirSheetDefinition> BuildReservoirSheetDefinitions(
        string startSheetNumber,
        int totalSheetCount,
        IList<WaterReservoirViewPlacementDto> placements)

        {
            var sheets = new List<ReservoirSheetDefinition>();

            for (int i = 0; i < totalSheetCount; i++)
            {
                var sheetIndex = i + 1;

                var titleBlockName = i < placements.Count
                    ? placements[i].TitleBlockName
                    : "A1_Key";

                sheets.Add(new ReservoirSheetDefinition
                {
                    Number = BuildSheetNumber(startSheetNumber, i),
                    Name = $"배수지 일반도({sheetIndex}/{totalSheetCount})",
                    TitleBlockName = titleBlockName
                });
            }
            return sheets;
        }
        private static string BuildSheetNumber(string startSheetNumber, int offset)
        {
            if (string.IsNullOrWhiteSpace(startSheetNumber))
                throw new InvalidOperationException("시작 도면번호가 비어 있습니다.");

            var prefix = new string(startSheetNumber.TakeWhile(c => !char.IsDigit(c)).ToArray());
            var numberText = new string(startSheetNumber.SkipWhile(c => !char.IsDigit(c)).ToArray());

            if (string.IsNullOrWhiteSpace(numberText) || !int.TryParse(numberText, out var startNumber))
                throw new InvalidOperationException($"시작 도면번호 형식이 올바르지 않습니다: {startSheetNumber}");

            var width = numberText.Length;
            return $"{prefix}{(startNumber + offset).ToString().PadLeft(width, '0')}";
        }
        private class ReservoirSheetDefinition
        {
            public string Number { get; set; }
            public string Name { get; set; }
            public string TitleBlockName { get; set; }
        }

        private static List<WaterReservoirViewPlacementDto> BuildReservoirViewPlacements()
        {
            return new List<WaterReservoirViewPlacementDto>
            {
                new WaterReservoirViewPlacementDto{ViewName = "수조부 상부슬래브", ViewTitleOnSheet = "상 부 슬 래 브", Scale = 80, Form = "일반도", TitleBlockName = "A1",     VisualStyle = "은선", TitleOffsetX = 1.9,   TitleOffsetY = 0,     TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "밸브실 중간슬래브", ViewTitleOnSheet = "중 간 슬 래 브", Scale = 80, Form = "일반도", TitleBlockName = "A1",     VisualStyle = "은선", TitleOffsetX = 1.9,   TitleOffsetY = 0,     TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "수조부 바닥슬래브", ViewTitleOnSheet = "하 부 슬 래 브", Scale = 80, Form = "일반도", TitleBlockName = "A1",     VisualStyle = "은선", TitleOffsetX = 1.9,   TitleOffsetY = 0,     TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "A",                 ViewTitleOnSheet = "A-A 단 면 도",   Scale = 80, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.77,  TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "B",                 ViewTitleOnSheet = "B-B 단 면 도",   Scale = 80, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.77,  TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "C",                 ViewTitleOnSheet = "C-C 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 1.27,  TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "D",                 ViewTitleOnSheet = "D-D 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 1.27,  TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "E",                 ViewTitleOnSheet = "E-E 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.9,   TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "F",                 ViewTitleOnSheet = "F-F 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.9,   TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "G",                 ViewTitleOnSheet = "G-G 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.9,   TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "H",                 ViewTitleOnSheet = "H-H 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.9,   TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "I",                 ViewTitleOnSheet = "I-I 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.9,   TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "J",                 ViewTitleOnSheet = "J-J 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.9,   TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "K",                 ViewTitleOnSheet = "K-K 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.9,   TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "L",                 ViewTitleOnSheet = "L-L 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.9,   TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "M",                 ViewTitleOnSheet = "M-M 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.9,   TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "N",                 ViewTitleOnSheet = "N-N 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.9,   TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "O",                 ViewTitleOnSheet = "O-O 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.9,   TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "P",                 ViewTitleOnSheet = "P-P 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.9,   TitleOffsetY = -0.3,  TitleLineLength = 0.18},
                new WaterReservoirViewPlacementDto{ViewName = "Q",                 ViewTitleOnSheet = "Q-Q 단 면 도",   Scale = 50, Form = "일반도", TitleBlockName = "A1_Key", VisualStyle = "은선", TitleOffsetX = 0.9,   TitleOffsetY = -0.3,  TitleLineLength = 0.18}
            };
        }


        // 시트에 배치할 뷰 설정
        public void PlaceReservoirViews()
        {
            var sheets = _sheetUseCase.GetSheets();
            var views = _sheetUseCase.GetViews();

            var sheetByNumber = sheets.ToDictionary(x => x.SheetNumber, StringComparer.OrdinalIgnoreCase);
            var viewByName = views
                .GroupBy(x => x.ViewName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var placements = BuildReservoirViewPlacements();

            for (int i = 0; i < placements.Count; i++)
            {
                placements[i].SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber, i);
            }
            var placementLimit = Math.Max(0, Math.Min(_reservoirTotalSheetCount - 1, placements.Count));

            var keyMaps = new[]
            {
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber,  3),  BasePlanViewName = "수조부 상부슬래브", SectionName = "A", Scale = 400, Title = "KEY PLAN(A)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber,  4),  BasePlanViewName = "수조부 상부슬래브", SectionName = "B", Scale = 400, Title = "KEY PLAN(B)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber,  5),  BasePlanViewName = "수조부 상부슬래브", SectionName = "C", Scale = 400, Title = "KEY PLAN(C)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber,  6),  BasePlanViewName = "수조부 상부슬래브", SectionName = "D", Scale = 400, Title = "KEY PLAN(D)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber,  7),  BasePlanViewName = "수조부 상부슬래브", SectionName = "E", Scale = 400, Title = "KEY PLAN(E)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber,  8),  BasePlanViewName = "수조부 상부슬래브", SectionName = "F", Scale = 400, Title = "KEY PLAN(F)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber,  9),  BasePlanViewName = "수조부 상부슬래브", SectionName = "G", Scale = 400, Title = "KEY PLAN(G)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber, 10),  BasePlanViewName = "수조부 상부슬래브", SectionName = "H", Scale = 400, Title = "KEY PLAN(H)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber, 11),  BasePlanViewName = "수조부 상부슬래브", SectionName = "I", Scale = 400, Title = "KEY PLAN(I)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber, 12),  BasePlanViewName = "수조부 상부슬래브", SectionName = "J", Scale = 400, Title = "KEY PLAN(J)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber, 13),  BasePlanViewName = "수조부 상부슬래브", SectionName = "K", Scale = 400, Title = "KEY PLAN(K)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber, 14),  BasePlanViewName = "수조부 상부슬래브", SectionName = "L", Scale = 400, Title = "KEY PLAN(L)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber, 15),  BasePlanViewName = "수조부 상부슬래브", SectionName = "M", Scale = 400, Title = "KEY PLAN(M)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber, 16),  BasePlanViewName = "수조부 상부슬래브", SectionName = "N", Scale = 400, Title = "KEY PLAN(N)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber, 17),  BasePlanViewName = "수조부 상부슬래브", SectionName = "O", Scale = 400, Title = "KEY PLAN(O)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber, 18),  BasePlanViewName = "수조부 상부슬래브", SectionName = "P", Scale = 400, Title = "KEY PLAN(P)" },
                 new { SheetNumber = BuildSheetNumber(_reservoirStartSheetNumber, 19),  BasePlanViewName = "수조부 상부슬래브", SectionName = "Q", Scale = 400, Title = "KEY PLAN(Q)" }

            };

            var keyMapLimit = Math.Max(0, Math.Min(_reservoirTotalSheetCount - 4, keyMaps.Length));

            foreach (var placement in placements.Take(placementLimit))
            {
                if (!sheetByNumber.TryGetValue(placement.SheetNumber, out var sheet))
                    continue;

                if (!viewByName.TryGetValue(placement.ViewName, out var view))
                    continue;

                var placedViewId = _sheetUseCase.AddViewToSheet(sheet.Id, view.ViewId, "_시트");

                if (!string.IsNullOrWhiteSpace(placedViewId))
                {
                    if (placement.Scale > 0)
                        _sheetUseCase.UpdateViewScale(placedViewId, placement.Scale);

                    if (!string.IsNullOrWhiteSpace(placement.Form))
                        _sheetUseCase.ApplyViewFormProfile(placedViewId, placement.Form);

                    if (!string.IsNullOrWhiteSpace(placement.VisualStyle))
                        _sheetUseCase.UpdateViewVisualStyle(placedViewId, placement.VisualStyle);
                    if (!string.IsNullOrWhiteSpace(placement.ViewTitleOnSheet))
                        _sheetUseCase.UpdateViewTitleOnSheet(placedViewId, placement.ViewTitleOnSheet);
                    _sheetUseCase.UpdateSheetParameters(
                        sheet.Id, sheet.SheetName, string.Empty, 
                        placement.Scale > 0 ? $"1:{placement.Scale}" : string.Empty, sheet.SheetNumber);

                    _sheetUseCase.RecenterViewportToSheetCenter(sheet.Id, placedViewId);
                    _sheetUseCase.UpdateViewportTitleLayout(sheet.Id, placedViewId, placement.TitleOffsetX, placement.TitleOffsetY, placement.TitleLineLength);
                }
            }
            foreach (var km in keyMaps.Take(keyMapLimit))
            {
                if (!sheetByNumber.TryGetValue(km.SheetNumber, out var sheet))
                    continue;

                if (!viewByName.TryGetValue(km.BasePlanViewName, out var baseView))
                    continue;

                var placedKeyMapViewId = _sheetUseCase.AddViewToSheet(sheet.Id, baseView.ViewId, "_KeyMap", km.Title);

                if (string.IsNullOrWhiteSpace(placedKeyMapViewId))
                    continue;

                _sheetUseCase.UpdateViewScale(placedKeyMapViewId, km.Scale);
                _sheetUseCase.UpdateViewTitleOnSheet(placedKeyMapViewId, km.Title);
                _sheetUseCase.ApplyViewFormProfile(placedKeyMapViewId, "KeyMap");
                _sheetUseCase.FilterKeyMapSections(placedKeyMapViewId, km.SectionName);
                _sheetUseCase.SetViewportType(sheet.Id, placedKeyMapViewId, "제목 없음");
                _sheetUseCase.MoveViewportBySheetRatio(sheet.Id, placedKeyMapViewId, 0.855, 0.82);
            }
            _sheetUseCase.HideCopiedSectionMarkersOnReservoirPlanViews();
        }


        public void DeleteReservoirSheetsAndViews()
        {
            _sheetUseCase.DeleteReservoirSheetsAndViews(_reservoirStartSheetNumber, _reservoirTotalSheetCount);
        }
        public void DeleteReservoirSheets()
        {
            _sheetUseCase.DeleteReservoirSheets(_reservoirStartSheetNumber, _reservoirTotalSheetCount);
        }

        public void DeleteReservoirViews()
        {
            _sheetUseCase.DeleteReservoirViews();
        }


        public IList<DimensionTypeDto> GetDimensionTypes()
        {
            return _sheetUseCase.GetDimensionTypes();
        }

        public void ApplyReservoirDimensions(string dimensionTypeName)
        {
            var sheets = GetReservoirSheets();

            foreach (var sheet in sheets)
            {
                _sheetUseCase.ApplyReservoirDimensions(sheet.Id, dimensionTypeName);
            }
        }

        public void OpenReservoirSheets()
        {
            var sheets = GetReservoirSheets();

            _originalViewId = _sheetUseCase.GetActiveViewId();

            foreach (var sheet in sheets)
            {
                _sheetUseCase.ActivateView(sheet.Id);
            }

            var firstSheet = sheets.FirstOrDefault();
            if (firstSheet != null)
            {
                _sheetUseCase.ActivateView(firstSheet.Id);
            }
        }

        public void OpenFirstReservoirSheet()
        {
            var firstSheet = GetReservoirSheets().FirstOrDefault();
            if (firstSheet == null)
                return;

            _originalViewId = _sheetUseCase.GetActiveViewId();
            _sheetUseCase.ActivateView(firstSheet.Id);
        }

        public void CloseReservoirSheets()
        {
            if (string.IsNullOrWhiteSpace(_originalViewId))
                return;

            _sheetUseCase.ActivateView(_originalViewId);
        }
        public void ApplyReservoirTags()
        {
            var sheets = GetReservoirSheets();

            _originalViewId = _sheetUseCase.GetActiveViewId();

            foreach (var sheet in sheets)
            {
                _sheetUseCase.ApplyReservoirTags(sheet.Id);
            }
        }

        private static HashSet<string> BuildReservoirSheetNumbers(string startSheetNumber, int totalSheetCount)
        {
            var numbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < totalSheetCount; i++)
                numbers.Add(BuildSheetNumber(startSheetNumber, i));

            return numbers;
        }
        private List<SheetInfoDto> GetReservoirSheets()
        {
            return _sheetUseCase.GetSheets()
                .Where(x => !string.IsNullOrWhiteSpace(x.SheetName) &&
                            x.SheetName.StartsWith("배수지 일반도(", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.SheetNumber)
                .ToList();
        }
    }
}
